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
        public Task Scan(IPAddress[] ipAddresses, int[] ports) =>
            Task.WhenAll(ipAddresses
                .Select(ipAddress => PingAddress(ipAddress)
                    .ContinueWith(task =>
                    {
                        if (task.Result != IPStatus.Success) return;
                        Task.WhenAll(ports.Select(port => CheckPort(ipAddress, port)))
                            .ContinueWith(_ => { }, TaskContinuationOptions.AttachedToParent);
                    }))
            );

        private static Task<IPStatus> PingAddress(IPAddress ipAddress, int timeout = 3000)
        {
            var ping = new Ping();
            Console.WriteLine($"Pinging {ipAddress}");
            return ping
                .SendPingAsync(ipAddress, timeout)
                .ContinueWith(task =>
                {
                    ping.Dispose();
                    Console.WriteLine($"Pinged {ipAddress}: {task.Result.Status}");
                    return task.Result.Status;
                });
        }

        private static Task CheckPort(IPAddress ipAddress, int port, int timeout = 3000)
        {
            var tcpClient = new TcpClient();
            Console.WriteLine($"Checking {ipAddress}:{port}");
            return tcpClient
                .ConnectAsync(ipAddress, port, timeout)
                .ContinueWith(task =>
                {
                    Console.WriteLine($@"Checked {ipAddress}:{port} - {task.Result}");
                    tcpClient.Dispose();
                });
        }
    }
}