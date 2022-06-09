using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetTime
{
    // Derived from this rather neat answer at https://stackoverflow.com/questions/40627941/asynchronous-operations-in-a-console-application/40630963#40630963
    public class Machine
    {
        public bool ConsoleKeyInfo { get; private set; }

        public void Run()
        {
            // get the times and compare
            DateTime remoteNow = GetRemoteTime();
            DateTime localNow = DateTime.Now;
            Console.WriteLine("Remote time is  " + remoteNow.ToLongDateString() + " " + remoteNow.ToLongTimeString());
            Console.WriteLine("Local time is   " + localNow.ToLongDateString() + " " + localNow.ToLongTimeString());
            TimeSpan timeDiff = remoteNow - localNow;
            // Display the difference
            StringBuilder sb = new StringBuilder();
            sb.Append("Local time is");
            if (timeDiff.Days != 0) sb.Append(" " + Grammar(timeDiff.Days, "day"));
            if (timeDiff.Hours != 0) sb.Append(" " + Grammar(timeDiff.Hours, "hour"));
            if (timeDiff.Minutes != 0) sb.Append(" " + Grammar(timeDiff.Minutes, "minute"));
            sb.Append(" " + Grammar(timeDiff.Seconds, "second"));
            sb.Append((timeDiff.TotalDays > 0) ? " behind." : " ahead.");
            if (Math.Abs(timeDiff.Days) > 365)
            {
                double years = ((double)timeDiff.TotalDays) / 365.25;
                sb.Append(string.Format(" That's about {0:F1} years.", Math.Abs(years)));
            }
            Console.WriteLine(sb);

            // If we're more than 15 seconds out, we need to do something
            if (Math.Abs(timeDiff.TotalSeconds) > 15)
            {
                // But not unless we are admin
                if (!IsAdmin())
                {
                    Console.WriteLine("I'd like to help, but you haven't got admin rights");
                    Console.WriteLine("Try a console window with admin rights and run " + Process.GetCurrentProcess().MainModule.FileName);
                }
                else
                {
                    // Does the user want to change it?
                    Console.WriteLine("Press 'R' to set to Remote, any other key to ignore");
                    ConsoleKeyInfo consoleKeyInfo;
                    // Wait for them to choose
                    while (!Console.KeyAvailable)
                    {
                        Console.Write('.');
                        Thread.Sleep(1000);
                    }
                    consoleKeyInfo = Console.ReadKey(true);
                    // If they want to get the remote time ...
                    if (consoleKeyInfo.Key.ToString().ToUpper() == "R")
                    {
                        // Fetch the remote time again - we may have waited a while since the original request
                        remoteNow = GetRemoteTime();

                        TimeZoneInfo timeZoneInfo = TimeZoneInfo.Local;
                        TimeSpan offsetAmount = timeZoneInfo.GetUtcOffset(remoteNow);
                        DateTime timeToUse = (remoteNow - offsetAmount);

                        SYSTEMTIME systime = new SYSTEMTIME(timeToUse);
                        SetSystemTime(ref systime);
                        Console.WriteLine();
                        Console.WriteLine("Local time set to  " + remoteNow.ToLongDateString() + " " + remoteNow.ToLongTimeString());
                    }
                }
            }
            else
            {
                Console.WriteLine("Bye");
            }
            Console.WriteLine("Press any key to finish");
            Console.ReadKey();
        }

        /// <summary> Do we have admin rights?</summary>
        /// <returns>true if admin</returns>
        private static bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary> Takes a count and a unit, removes the sign and creates good grammar from it
        /// e.g. -2 and "year" becomes "2 years"
        /// e.g. 1 and "year" becomes "1 year"</summary>
        /// <param name="count"></param>
        /// <param name="unit"></param>
        /// <returns>good grammar</returns>
        private string Grammar(int count, string unit)
        {
            int absCount = Math.Abs(count);
            return absCount.ToString() + " " + unit + ((absCount) != 1 ? "s" : "");
        }

        private static DateTime GetRemoteTime()
        {
            return GetTime().Result;
        }

        public static async Task<DateTime> GetTime()
        {
            return await Task.WhenAny
            (
                Task.Run(() => GetNetworkTime("ntp.ripe.net")),
                Task.Run(() => GetNetworkTime("2.europe.pool.ntp.org")),
                Task.Run(() => GetNetworkTime("ntp2.net.berkeley.edu")),
                Task.Run(() => GetNetworkTime("time.windows.com"))
            ).Result;
        }


        public static async Task<DateTime> GetNetworkTime(string url)
        {
            IPAddress[] address = Dns.GetHostEntry(url).AddressList;

            if (address == null || address.Length == 0)
            {
                Console.WriteLine($"Couldn't dns resolve {url}");
            }

            IPEndPoint ep = new IPEndPoint(address[0], 123);
            var result = await GetNetworkTime(ep);

            return result;
        }



        public static async Task<DateTime> GetNetworkTime(IPEndPoint ep)
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, ep, null);

                byte[] ntpData = new byte[48]; // RFC 2030 
                ntpData[0] = 0x1B;

                await Task.Factory.FromAsync(
                    socket.BeginSend(ntpData, 0, ntpData.Length, SocketFlags.None, null, null),
                    socket.EndSend);

                await Task.Factory.FromAsync(
                    socket.BeginReceive(ntpData, 0, ntpData.Length, SocketFlags.None, null, null),
                    socket.EndReceive);

                return asDateTime(ntpData);
            }
        }

        static DateTime asDateTime(byte[] ntpData)
        {
            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;

            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);

            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            TimeZoneInfo timeZoneInfo = TimeZoneInfo.Local;
            TimeSpan offsetAmount = timeZoneInfo.GetUtcOffset(dateTime);
            DateTime networkDateTime = (dateTime + offsetAmount);

            return networkDateTime;
        }


        [DllImport("kernel32.dll")]
        static extern bool SetSystemTime(ref SYSTEMTIME time);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                Year = (ushort)dt.Year;
                Month = (ushort)dt.Month;
                DayOfWeek = (ushort)dt.DayOfWeek;
                Day = (ushort)dt.Day;
                Hour = (ushort)dt.Hour;
                Minute = (ushort)dt.Minute;
                Second = (ushort)dt.Second;
                Milliseconds = (ushort)dt.Millisecond;
            }
        }
    }
}
