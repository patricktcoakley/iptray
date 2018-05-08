using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using IPTrayApplet.Properties;

namespace IPTrayApplet
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }
    }


    public class App : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public App()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.ipcopy,
                ContextMenu = new ContextMenu(),
                Visible = true
            };

            foreach (String s in GetIPAddresses()) // Populate menu with each IP address
            {
                trayIcon.ContextMenu.MenuItems.Add(s, CopyToClipboard);
            }

            trayIcon.ContextMenu.MenuItems.Add("Exit", Exit); // Add the Exit menu item last
        }

        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        private void CopyToClipboard(object sender, EventArgs e)
        {
            var menuItem = (MenuItem) sender; // Cast the sender as menu item type in order to get text property
            var ip = menuItem.Text.Split(' ');
            Clipboard.SetText(ip[0]);
        }

        private ArrayList GetIPAddresses()
        {
            var listOfIPs = new ArrayList();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                var ipProperties = network.GetIPProperties();

                foreach (var address in ipProperties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork
                        && !(IPAddress.IsLoopback(address.Address))
                        && !IsPrivate(address.Address.GetAddressBytes()[0])) // Only add non-private IPv4 addresses
                    {
                        listOfIPs.Add(address.Address.ToString() + " (" + network.Name + ")");
                    }
                    
                }
            }
            // Add public IP via HTTP GET request
            String[] getRequest = new WebClient().DownloadString("http://checkip.dyndns.org/").Split(':');
            getRequest = getRequest[1].Split('<');
            String externalIP = getRequest[0].Trim(' ') + " (Public IP)";
            listOfIPs.Add(externalIP);
            return listOfIPs;
        }

        private bool IsPrivate(byte b)
        {
            var privateIPList = new byte[] { 127, 169, 172 };
            foreach(var prefix in privateIPList)
            {
                if (prefix == b) return true;
            }
            return false;
        }
    }
}