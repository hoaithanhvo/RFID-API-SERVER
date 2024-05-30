//#define LOCAL
namespace ApiServer
{
    using System;
    using System.Threading;
    using System.Reflection;
    using System.Net.Sockets;
    using System.Net.NetworkInformation;
    using System.Runtime.InteropServices;
    using Grapevine;
    using log4net;

    static class DisableConsoleQuickEdit
    {
        const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        internal static bool Go()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            if (!GetConsoleMode(consoleHandle, out uint consoleMode))
            {
                // ERROR: Unable to get console mode.
                return false;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT;

            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                // ERROR: Unable to set console mode
                return false;
            }

            return true;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            DisableConsoleQuickEdit.Go();

            string IP = string.Empty;
#if LOCAL
            IP = GetLocalIPv4(NetworkInterfaceType.Wireless80211);
            Logging.WriteLog($"Run server on {IP}:12345");
            Logging.WriteLog("Press CTRL+C to terminate server.\n");
            Logging.WriteLog($"Host: {IP}:12345");
#else
            IP = GetLocalIPv4(NetworkInterfaceType.Ethernet);
            Logging.WriteLog($"Run server on {IP}:1234");
            Logging.WriteLog("Press CTRL+C to terminate server.\n");
            Logging.WriteLog($"Host: {IP}:1234");
#endif
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            try
            {
                var server = RestServerBuilder.UseDefaults().Build();

                server.Prefixes.Clear();
#if LOCAL
                server.Prefixes.Add("http://*:12345/");
#else
                //server.Prefixes.Add("http://*:1234/");
#endif
                server.Start();
                exitEvent.WaitOne();
                Logging.WriteLog("Server Stopped!");
                server.Stop();
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
            }
        }
        
        static string GetLocalIPv4(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }
    }

    class Logging
    {
        public static ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void WriteLog(string msg)
        {
            Console.WriteLine(msg);
            logger.Info(msg);
        }
    }
}
