using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Web;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AppletConsoMonabee
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //this.WindowState = FormWindowState.Minimized;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized){
                notifyIcon1.BalloonTipText = "Minimisé";
                notifyIcon1.ShowBalloonTip(100);
            }
            else
            {
                notifyIcon1.BalloonTipText = "normal";
                notifyIcon1.ShowBalloonTip(100);
            }
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
            string[] plageIP = new string[254];
            string[] sousPartiesIP;
            //par default 192.268.1.1/24
            string localIP = "192.168.42.1";
            
            ////adresse du poste
            //IPHostEntry host;
            //host = Dns.GetHostEntry(Dns.GetHostName());
            //foreach (IPAddress ip in host.AddressList)
            //{
            //    if (ip.AddressFamily.ToString() == "InterNetwork")
            //    {
            //        localIP = ip.ToString();
            //    }
            //}

            sousPartiesIP = localIP.Split('.');
            for (int i = 1; i < 254; i++)
            {
                plageIP[i] = sousPartiesIP[0]+'.'+sousPartiesIP[1]+'.'+sousPartiesIP[2]+'.'+Convert.ToString(i);
            }

            trouverBeeNet(plageIP);

            List<BeeNet> lesBeeNet = BeeNet.getlistBeeNets();
            for (int i = 0; i < lesBeeNet.Count(); i++)
            {
                Console.WriteLine(lesBeeNet[i].getIp());
                Console.WriteLine(lesBeeNet[i].getNom());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        #region actions
        public void Shutdown()
        {
            ManagementBaseObject shutdown = null;
            ManagementClass win32 = new ManagementClass("Win32_OperatingSystem");
            win32.Get();
            win32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject shutdownParam = win32.GetMethodParameters("Win32Shutdown");
            shutdownParam["Reserved"] = "0";
            foreach (ManagementObject mObj in win32.GetInstances())
            {
                //shutdown = mObj.InvokeMethod();
            }
        }
        #endregion


        #region ping
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
                        BeeNet beeNet = new BeeNet(reply.Address.ToString(), hostInfo.HostName);
                        Console.WriteLine("BeeNet : " + reply.Address.ToString() + ", " + hostInfo.HostName);
                        Console.WriteLine("Temps : {0}", reply.RoundtripTime + "ms");
                    }
                }
                catch(SocketException e) 
               {
                   //Console.WriteLine("SocketException caught!!!");
                   //Console.WriteLine("Source : " + e.Source);
                   //Console.WriteLine("Message : " + e.Message);
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
        #endregion

        #region API
        static string HttpPost(string url, string[] paramName, string[] paramVal)
        {
            HttpWebRequest req = WebRequest.Create(new Uri(url))
                                 as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            // Build a string with all the params, properly encoded.
            // We assume that the arrays paramName and paramVal are
            // of equal length:
            StringBuilder paramz = new StringBuilder();
            for (int i = 0; i < paramName.Length; i++)
            {
                paramz.Append(paramName[i]);
                paramz.Append("=");
                paramz.Append(HttpUtility.UrlEncode(paramVal[i]));
                paramz.Append("&");
            }

            // Encode the parameters as form data:
            byte[] formData =
                UTF8Encoding.UTF8.GetBytes(paramz.ToString());
            req.ContentLength = formData.Length;

            // Send the request:
            using (Stream post = req.GetRequestStream())
            {
                post.Write(formData, 0, formData.Length);
            }

            // Pick up the response:
            string result = null;
            using (HttpWebResponse resp = req.GetResponse()
                                          as HttpWebResponse)
            {
                StreamReader reader =
                    new StreamReader(resp.GetResponseStream());
                result = reader.ReadToEnd();
            }

            return result;
        }
        #endregion
    }
}
