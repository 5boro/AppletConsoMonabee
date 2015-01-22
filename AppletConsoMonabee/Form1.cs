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


namespace AppletConsoMonabee
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized){
                notifyIcon1.BalloonTipText = "Minimizé";
                notifyIcon1.ShowBalloonTip(1000);
            }
            else
            {
                notifyIcon1.BalloonTipText = "normal";
                notifyIcon1.ShowBalloonTip(1000);
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
            string[] plageIP = new string[255];
            for (int i = 1; i < 254; i++)
            {
                plageIP[i] = "192.168.42." + Convert.ToString(i);
            }
            //args[0] = textBox1.Text;
            trouverBeeNet(plageIP);
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
            for (int i = 1; i < plageIP.Count() - 1; i++)
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

                // Wait 1 seconds for a reply. 
                int timeout = 100;

                // Set options for transmission: 
                // The data can go through 64 gateways or routers 
                // before it is destroyed, and the data packet 
                // cannot be fragmented.
                PingOptions options = new PingOptions (64, true);

                //Console.WriteLine ("Ttl: {0}", options.Ttl);
                //Console.WriteLine ("Ne pas fragmenter: {0}", options.DontFragment);

                // Send the ping asynchronously. 
                // Use the waiter as the user token. 
                // When the callback completes, it can wake up this thread.
                pingSender.SendAsync(who, timeout, buffer, options, waiter);

                // Prevent this example application from ending. 
                // A real application should do something useful 
                // when possible.
                waiter.WaitOne(100, false);
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

            //Console.WriteLine ("ping status: {0}", reply.Status);
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine ("Addresse: {0}", reply.Address.ToString ());
                Console.WriteLine ("Temps : {0}", reply.RoundtripTime);
                Console.WriteLine(Dns.GetHostEntry(reply.Address));
                //Console.WriteLine ("Time to live: {0}", reply.Options.Ttl);
                //Console.WriteLine ("Don't fragment: {0}", reply.Options.DontFragment);
                //Console.WriteLine ("Buffer size: {0}", reply.Buffer.Length);
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
