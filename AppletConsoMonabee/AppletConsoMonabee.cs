using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Web;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Win32;
using RestSharp;

namespace AppletConsoMonabee
{
    public partial class AppletConsoMonabee : Form
    {

        static System.Windows.Forms.Timer s_myTimer = new System.Windows.Forms.Timer();
        static int s_myCounter;
        static int intervaleRapports;
        static string addresseAPI;
        static string macBeeNet;
        static string hostName;

        public AppletConsoMonabee()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            
            #region debut logs
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("Log.txt", FileMode.Append, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                MessageBox.Show("Impossible d'ouvrir Log.txt" + e.Message);
                return;
            }
            Console.SetOut(writer);
            Console.WriteLine("###################################");
            Console.WriteLine("# {0} {1} #", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            Console.WriteLine("###################################");
            #endregion

            //Fichier de configuration
            getConfig();

            //Intervalle
		    s_myTimer.Tick += new EventHandler(s_myTimer_Tick);

		    // 1 minute = 60*1000 millisecondes
            s_myTimer.Interval = 60000 * intervaleRapports;
		    s_myTimer.Start();
            Console.WriteLine("Timer lancé à {0}:{1}:{2}, intervalle : {3}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, s_myTimer.Interval.ToString());

            //Recuperartion des Evenements d'extinction

            #region Fin logs
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
            #endregion logs
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //if (this.WindowState == FormWindowState.Minimized){
            //    notifyIcon1.BalloonTipText = "Minimisé";
            //    notifyIcon1.ShowBalloonTip(10);
            //}
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void afficherLeFormulaireToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            eteindre();
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        #region actions
        public void eteindre()
        {
            ManagementBaseObject shutdown = null;
            ManagementClass win32 = new ManagementClass("Win32_OperatingSystem");
            win32.Get();
            win32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject shutdownParams = win32.GetMethodParameters("Win32Shutdown");
            shutdownParams["Reserved"] = "0";
            shutdownParams["Flags"] = "1";
            foreach (ManagementObject mObj in win32.GetInstances())
            {
                shutdown = mObj.InvokeMethod("Win32Shutdown", shutdownParams, null);
            }
        }
        #endregion

        #region evenements
        public void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            MessageBox.Show(e.Reason.ToString());
            e.Cancel = true;
            //envoyer au serveur la dateTime et la raison
            Mesure mesure = new Mesure(hostName, macBeeNet, DateTime.UtcNow, 0, 0, e.Reason.ToString(), false);
            mesure.ajouterMesure();
            List<Mesure> lesMesures = Mesure.getlistMesures();
            for (int i = 0; i < lesMesures.Count; i++)
            {
                if (!lesMesures[i].envoyé)
                {
                    envoyerMesure(lesMesures[i]);
                    lesMesures[i].envoyé = true;
                    Console.WriteLine(envoyerMesure(mesure));
                }
                Console.WriteLine(i);
            }
        }
        public void s_myTimer_Tick(object sender, EventArgs e)
        {
            //envoyer les mesures
            Mesure mesure = new Mesure(hostName, macBeeNet, DateTime.UtcNow, getConso(), getTypeChassis(), null, false);
            mesure.ajouterMesure();
            List<Mesure> lesMesures = Mesure.getlistMesures();
            for (int i = 0; i < lesMesures.Count; i++)
            {
                if (!lesMesures[i].envoyé)
                {
                    lesMesures[i].envoyé = true;
                    Console.WriteLine(envoyerMesure(lesMesures[i]));
                }
                Console.WriteLine(i);
            }
        }
        #endregion

        #region recherche BeeNet

        public string[] getPlageIP(){
            string[] plageIP = new string[255];
            string[] sousPartiesIP;
            //par default 192.268.1.1/24
            string localIP = "192.168.42.1";
            
            //adresse du poste
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }

            sousPartiesIP = localIP.Split('.');
            for (int i = 1; i < 255; i++)
            {
                plageIP[i] = sousPartiesIP[0]+'.'+sousPartiesIP[1]+'.'+sousPartiesIP[2]+'.'+Convert.ToString(i);
            }
            return plageIP;
        }
        public void trouverBeeNet(string[] plageIP)
        {
            if (plageIP.Length == 0)
                throw new ArgumentException ("Pas d'IP");
            for (int i = 1; i < plageIP.Count(); i++)
            {
                string who = plageIP[i];
                AutoResetEvent waiter = new AutoResetEvent (false);

                Ping pingSender = new Ping ();

                // When the PingCompleted event is raised, 
                // the PingCompletedCallback method is called.
                pingSender.PingCompleted += new PingCompletedEventHandler (PingCompletedCallback);

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes (data);
                int timeout = 100;

                //Ttl, DontFragment
                PingOptions options = new PingOptions (64, true);

                // Send the ping asynchronously. 
                // Use the waiter as the user token. 
                // When the callback completes, it can wake up this thread.
                pingSender.SendAsync(who, timeout, buffer, options, waiter);

                waiter.WaitOne(100, true);

                    Console.WriteLine("Ping " + plageIP[i] + " Lancé.");
            }
        }

        private static void PingCompletedCallback (object sender, PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user. 
            if (e.Cancelled)
            {
                Console.WriteLine ("Ping canceled.");

                // Let the main thread resume.  
                // UserToken is the AutoResetEvent object that the main thread  
                // is waiting for.
                ((AutoResetEvent)e.UserState).Set ();
            }

            // If an error occurred, display the exception to the user. 
            if (e.Error != null)
            {
                Console.WriteLine ("Ping failed:");
                Console.WriteLine (e.Error.ToString ());

                // Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
            }

            PingReply reply = e.Reply;

            DisplayReply (reply);

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }

        public static void DisplayReply (PingReply reply)
        {
            if (reply == null)
                return;

            if (reply.Status == IPStatus.Success)
            {
                try{
                    IPHostEntry hostInfo = Dns.GetHostEntry(reply.Address);
                    if (hostInfo.HostName.Contains("BeeNet"))
                    {
                        String MAC = GetMacFromIP(reply.Address);
                        BeeNet beeNet = new BeeNet(reply.Address.ToString(), hostInfo.HostName, MAC);
                        beeNet.ajouterBeeNet();

                        Console.WriteLine("BeeNet : " + reply.Address.ToString() + ", " + hostInfo.HostName + ", " + MAC);
                        Console.WriteLine("Temps :" + reply.RoundtripTime + "ms");
                    }
                }
                catch(SocketException e) 
               {
                    Console.WriteLine("SocketException caught!!!");
                    Console.WriteLine("Source : " + e.Source);
                    Console.WriteLine("Message : " + e.Message);
               }
               catch(ArgumentNullException e)
               {
                    Console.WriteLine("ArgumentNullException caught!!!");
                    Console.WriteLine("Source : " + e.Source);
                    Console.WriteLine("Message : " + e.Message);
               }
               catch(Exception e)
               {
                    Console.WriteLine("Exception caught!!!");
                    Console.WriteLine("Source : " + e.Source);
                    Console.WriteLine("Message : " + e.Message);
               }
            }
        }
        
        [System.Runtime.InteropServices.DllImport("Iphlpapi.dll", EntryPoint = "SendARP")]
        internal extern static Int32 SendArp(Int32 destIpAddress, Int32 srcIpAddress, byte[] macAddress, ref Int32 macAddressLength);

        public static String GetMacFromIP(System.Net.IPAddress IP)
        {
            if (IP.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("IPv4 seulement");

            Int32 addrInt = IpToInt(IP);
            Int32 srcAddrInt = IpToInt(IP);

            byte[] mac = new byte[6]; // 48 bit
            int length = mac.Length;
            int reply = SendArp(addrInt, srcAddrInt, mac, ref length);

            String rawMac = new System.Net.NetworkInformation.PhysicalAddress(mac).ToString();
            String newMac = Regex.Replace(rawMac, "(..)(..)(..)(..)(..)(..)", "$1:$2:$3:$4:$5:$6");

            return newMac;
        }
        
        private static Int32 IpToInt(System.Net.IPAddress IP)
        {
            byte[] bytes = IP.GetAddressBytes();
            return BitConverter.ToInt32(bytes, 0);
        }
        #endregion

        #region Communications Monabee
        public string envoyerMesure(Mesure uneMesure)
        {
            var client = new RestClient(addresseAPI);
            var request = new RestRequest("ajoutMesure.php", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("netBios", uneMesure.netBIOS);
            request.AddParameter("macBeeNet", uneMesure.macBeeNet);
            request.AddParameter("dateheure", uneMesure.heure.ToString("yyyy-MM-dd H:mm:ss"));
            request.AddParameter("conso", uneMesure.conso);
            request.AddParameter("chassis", uneMesure.typechassis);
            request.AddParameter("evenement", uneMesure.evenement);
            var response = client.Execute(request);
            return response.Content;
        }
        #endregion

        #region Communications BeeNet

        public string getAddrServeur()
        {
            return "http://apiserveur.com/";
        }

        #endregion

        #region WMI

        public static int getConso()
        {
            int conso = 0;
            conso = getPowerMeter();
            if (conso == 0)
	        {
                conso = getVoltage()*getCourant();
	        }
            return conso;
        }
        public static int getPowerMeter()
        {
            int resultat = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2\\power",
                    "SELECT * FROM Win32_PowerMeter");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("Win32_PowerMeter instance");
                    Console.WriteLine("CurrentReading: {0}", queryObj["CurrentReading"]);
                    Console.WriteLine("-----------------------------------");
                    if (Convert.ToInt32(queryObj["CurrentReading"]) != 0)
                    {
                        resultat = Convert.ToInt32(queryObj["CurrentReading"]);
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("Mesure PowerMeter : " + e.Message);
            }
            return resultat;
        }

        public static int getTypeChassis()
        {
            int resultat = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_SystemEnclosure");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("Win32_SystemEnclosure instance");
                    if (queryObj["ChassisTypes"] == null)
                        Console.WriteLine("ChassisTypes: {0}", queryObj["ChassisTypes"]);
                    else
                    {
                        UInt16[] arrChassisTypes = (UInt16[])(queryObj["ChassisTypes"]);
                        foreach (UInt16 arrValue in arrChassisTypes)
                        {
                            Console.WriteLine("ChassisTypes: {0}", arrValue);
                            if (arrValue != 0)
                            {
                                resultat = arrValue;
                            }
                        }
                    }
                    Console.WriteLine("-----------------------------------");

                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("Mesure chassisType" + e.Message);
            }
            return resultat;
        }

        public static int getVoltage()
        {
            int resultat = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_VoltageProbe");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("Win32_VoltageProbe instance");
                    Console.WriteLine("CurrentReading: {0}", queryObj["CurrentReading"]);
                    Console.WriteLine("-----------------------------------");
                    if (Convert.ToInt32(queryObj["CurrentReading"]) != 0)
                    {
                        resultat = Convert.ToInt32(queryObj["CurrentReading"]);
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("Mesure voltageProbe : " + e.Message);
            }
            return resultat;
        }

        public static int getCourant()
        {
            int resultat = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_CurrentProbe");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("Win32_CurrentProbe instance");
                    Console.WriteLine("CurrentReading: {0}", queryObj["CurrentReading"]);
                    Console.WriteLine("-----------------------------------");
                    if (Convert.ToInt32(queryObj["CurrentReading"]) != 0)
                    {
                        resultat = Convert.ToInt32(queryObj["CurrentReading"]);
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("Mesure currentProbe: " + e.Message);
            }
            return resultat;
        }

        public static string getHostName()
        {
            string resultat = "";
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_ComputerSystem");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("-----------------------------------");
                    Console.WriteLine("Win32_ComputerSystem instance");
                    Console.WriteLine("Name: {0}", queryObj["Name"]);
                    Console.WriteLine("-----------------------------------");
                    resultat = queryObj["Name"].ToString();
                }
            }
            catch (ManagementException e)
            {
                resultat = e.Message;
            }
            return resultat;
        }

        #endregion

        #region configuration

        public void getConfig()
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings["ipBeeNet"].Value == null || settings["ipBeeNet"].Value == "" || settings["ipBeeNet"].Value == null || settings["macBeeNet"].Value == "")
                {
                    Console.WriteLine("Fichier de configuration vide ou non présent.");
                    Task t = Task.Run(() => { trouverBeeNet(getPlageIP()); });
                    t.Wait();

                    List<BeeNet> lesBeeNet = BeeNet.getlistBeeNets();
                    for (int i = 0; i < lesBeeNet.Count(); i++)
                    {
                        if (settings["ipBeeNet"] == null)
                        {
                            settings.Add("ipBeeNet", lesBeeNet[i].ip);
                        }
                        else
                        {
                            settings["ipBeeNet"].Value = (lesBeeNet[i].ip);
                        }

                        if (settings["nomBeeNet"] == null)
                        {
                            settings.Add("nomBeeNet", lesBeeNet[i].nom);
                        }
                        else
                        {
                            settings["nomBeeNet"].Value = (lesBeeNet[i].nom);
                        }

                        if (settings["macBeeNet"] == null)
                        {
                            settings.Add("macBeeNet", lesBeeNet[i].mac);
                        }
                        else
                        {
                            settings["macBeeNet"].Value = (lesBeeNet[i].mac);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("fichier de configuration présent.");
                    BeeNet beeNet = new BeeNet(settings["ipBeeNet"].Value, settings["nomBeeNet"].Value, settings["macBeeNet"].Value);
                    beeNet.ajouterBeeNet();
                }

                if (settings["hostName"].Value == null || settings["hostName"].Value == "")
                {
                    settings["hostName"].Value = getHostName();
                }

                if (settings["adresseAPI"].Value == null || settings["adresseAPI"].Value == "")
                {
                    settings["adresseAPI"].Value = getAddrServeur();
                }

                hostName = settings["hostName"].Value;
                intervaleRapports = Convert.ToInt32(settings["intervaleRapports"].Value);
                addresseAPI = settings["adresseAPI"].Value;
                macBeeNet = settings["macBeeNet"].Value;

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Impossible d'accéder au fichier de configuration");
            }
        }
        #endregion
    }
}
