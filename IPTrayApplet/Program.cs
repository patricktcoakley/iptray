using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using IPTrayApplet.Properties;
using Newtonsoft.Json.Linq;

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
        private readonly NotifyIcon _trayIcon;
        private static readonly HttpClient HttpClient = new HttpClient();

        public App()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.ipcopy,
                ContextMenu = new ContextMenu(),
                Visible = true
            };

            NetworkChange.NetworkAddressChanged += RefreshAddresses;
            SetMenuItems();
        }

        private async void SetMenuItems()
        {
            var addresses = await GetAddresses();
            _trayIcon.ContextMenu.MenuItems.Clear();

            if (addresses != null && addresses.Count > 0)
            {
                foreach (var address in addresses)
                {
                    _trayIcon.ContextMenu.MenuItems.Add(address, CopyToClipboard);
                }
            }

            _trayIcon.ContextMenu.MenuItems.Add("Exit", Exit);
        }


        private static void CopyToClipboard(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            var ip = menuItem?.Text.Split(' ');
            if (ip != null) Clipboard.SetText(ip[0]);
        }

        private static async Task<List<string>> GetAddresses()
        {
            var listOfIPs = new List<string>();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();


            foreach (var networkInterface in networkInterfaces)
            {
                foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(address.Address) // Ignore loopback
                        && !IsPrivateAddress(address.Address.GetAddressBytes()[0])
                    ) // Only add non-private IPv4 addresses
                    {
                        listOfIPs.Add(address.Address + " (" + networkInterface.Name + ")");
                    }
                }
            }

            try
            {
                {
                    var response = await HttpClient.GetAsync("https://api.ipify.org/?format=json");
                    var stringResult = await response.Content.ReadAsStringAsync();
                    listOfIPs.Add($"{JObject.Parse(stringResult).SelectToken("ip")} (Public)");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return listOfIPs;
        }

        private void RefreshAddresses(object sender, EventArgs e) => SetMenuItems();
        private static void Exit(object sender, EventArgs e) => Application.Exit();
        private static bool IsPrivateAddress(byte b) => new byte[] {127, 169, 172}.Any(prefix => prefix == b);
    }
}