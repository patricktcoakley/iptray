using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using IPTrayCore.Properties;
using Newtonsoft.Json.Linq;

namespace IPTrayCore {
    public static class Program {
        [STAThread]
        public static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }
    }

    public class App : ApplicationContext {
        private static readonly NotifyIcon TrayIcon = new NotifyIcon {
            Icon = Resources.iptray,
            ContextMenu = new ContextMenu(),
            Visible = true
        };

        private static readonly HttpClient HttpClient = new HttpClient();

        public App() {
            NetworkChange.NetworkAddressChanged += RefreshAddresses;
            SetMenuItems();
        }

        private static async void SetMenuItems() {
            var addresses = await GetAddresses();
            TrayIcon.ContextMenu.MenuItems.Clear();

            if (addresses?.Count > 0) {
                foreach (var address in addresses) {
                    TrayIcon.ContextMenu.MenuItems.Add(address, CopyToClipboard);
                }
            }

            TrayIcon.ContextMenu.MenuItems.Add("Exit", Exit);
        }


        private static void CopyToClipboard(object sender, EventArgs e) {
            var menuItem = sender as MenuItem;
            var ip = menuItem?.Text.Split(' ');
            if (ip != null) Clipboard.SetText(ip[0]);
        }

        private static async Task<List<string>> GetAddresses() {
            var listOfIPs = new List<string>();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces) {
                var addresses = networkInterface.GetIPProperties().UnicastAddresses;

                foreach (var address in addresses) {
                    if (IsValidAddress(address)) listOfIPs.Add(address.Address + " (" + networkInterface.Name + ")");
                }
            }

            try {
                var response = await HttpClient.GetAsync("https://api.ipify.org/?format=json");
                var stringResult = await response.Content.ReadAsStringAsync();
                listOfIPs.Add($"{JObject.Parse(stringResult).SelectToken("ip")} (Public)");
            } catch (Exception e) {
                Console.WriteLine(e); // TODO replace with logger
            }

            return listOfIPs;
        }

        private static void RefreshAddresses(object sender, EventArgs e) => SetMenuItems();
        private static void Exit(object sender, EventArgs e) => Application.Exit();
        private static bool IsPrivateAddress(byte b) => new byte[] {127, 169, 172}.Any(prefix => prefix == b);

        private static bool IsValidAddress(UnicastIPAddressInformation address) =>
            address.Address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(address.Address) // Ignore loopback
            && !IsPrivateAddress(address.Address.GetAddressBytes()[0]); // Only add non-private IPv4 addresses;
    }
}