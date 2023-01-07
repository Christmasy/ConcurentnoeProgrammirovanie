using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ContKor
{
    public class TplScanner : IPScanner
    {
        private static async Task ProcessIpAddressAsync(IPAddress ipAddress, int[] ports)
        {
            var status = await PingAddressAsync(ipAddress);
            if (status != IPStatus.Success) return; 
            await Task.WhenAll(ports.Select(port => CheckPortAsync(ipAddress, port))); 
        }
        
        public Task Scan(IPAddress[] ipAddresses, int[] ports) =>
            Task.WhenAll(ipAddresses.Select(ipAddress => ProcessIpAddressAsync(ipAddress, ports)));
        
        private static async Task<IPStatus> PingAddressAsync(IPAddress ipAddress, int timeout = 3000)
        {
            using var ping = new Ping(); // dispose ping
            Console.WriteLine($"Pinging {ipAddress}");
            var result = await ping.SendPingAsync(ipAddress, timeout);
            Console.WriteLine($"Pinged {ipAddress}: {result.Status}");
            return result.Status;
        }

        private static async Task CheckPortAsync(IPAddress ipAddress, int port, int timeout = 3000)
        {
            using var tcpClient = new TcpClient();
            Console.WriteLine($"Checking {ipAddress}:{port}");
            var result = await tcpClient.ConnectAsync(ipAddress, port, timeout);
            Console.WriteLine($"Checked {ipAddress}:{port} - {result}");
        }
    }
}