using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace apnicparser
{
    class Program
    {
        static StringBuilder sb = new StringBuilder();

#if DEBUG
        public class Range
        {
            public uint Start;
            public uint End;

            public uint Total { get { return End - Start + 1; } }

            public Range()
            {
            }

            public Range(uint start, uint end)
            {
                Start = start;
                End = end;
            }

            public static Range ExtendRangeOrNew(Range existing, uint start, uint end)
            {
                if(existing != null && existing.End == start - 1)
                {
                    existing.End = end;
                }
                else
                {
                    return new Range(start, end);
                }
                return null;
            }
        }

        public class RangeGaps
        {
            public readonly List<Range> FilledRanges = new List<Range>();
            public readonly List<Range> MissingRanges = new List<Range>();

            public Range CurrentRange = null;

            public void AddNewRange(uint start, uint end)
            {
                var newRange = Range.ExtendRangeOrNew(CurrentRange, start, end);

                if (CurrentRange == null)
                {
                    FilledRanges.Add(newRange);
                    CurrentRange = newRange;
                }
                else if( newRange != null)
                {
                    var missingRange = new Range(CurrentRange.End + 1, newRange.Start - 1);
                    MissingRanges.Add(missingRange);
                    FilledRanges.Add(newRange);
                    CurrentRange = newRange;
                }
            }
        }
#endif

        static void Main(string[] args)
        {
            // The readme file for the original release is ftp://ftp.apnic.net/pub/apnic/stats/apnic/README.TXT

            //Downloaded from ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest
            string file = @":";
            string[] limitLocaions = { "au", };
            string[] limitTypes = { "ipv4", };

            if (args.Length > 0)
            {
                file = args[0];
            }

            if (file == ":")
            {
                file = "ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest";
            }

            if (file.Contains("://"))
            {
                var tempFile = "delegated-apnic-extended-latest.temp";
                var tempCachedFile = "delegated-apnic-extended-latest.cached";
                var tempCacheInfo = new FileInfo(tempCachedFile);
                if (!tempCacheInfo.Exists || tempCacheInfo.LastWriteTime < DateTime.Now.AddDays(-1))
                {
                    using (var client = new WebClient())
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }
                        client.DownloadFile(file, tempFile);
                        if (tempCacheInfo.Exists)
                        {
                            tempCacheInfo.Delete();
                        }

                        File.Move(tempFile, tempCachedFile);
                    }
                }
                file = tempCachedFile;
            }

            if (args.Length > 1)
            {
                var limitLocation = args[1].ToLower();
                limitLocaions = limitLocation.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (args.Length > 2)
            {
                var limitType = args[2].ToLower();
                limitTypes = limitType.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            }

            bool shortOutput = true;

            if (args.Length > 3)
            {
                var outputType = args[3].ToLower();

                shortOutput = outputType != "long" && outputType != "l";
            }

            if (args.Length > 4)
            {
                Separator = args[4];
            }

            if (!File.Exists(file))
            {
                Console.Error.WriteLine("{0} doesn't exist, usage is\r\n [filename [location,location,location [type,type,type]]]", file);
                Console.ReadKey();
                return;
            }

            var lines = File.ReadAllLines(file);
            lines = lines.Where(l => !l.StartsWith("#") && !l.Contains("*") && !l.Contains("-") && !l.Contains("+")).ToArray();

#if DEBUG
            RangeGaps ranges = new RangeGaps();
#endif
            foreach (var line in lines)
            {
                var sections = line.Split('|');
                var offset = -1;
                var registry = sections[++offset];
                var place = sections[++offset];
                var type = sections[++offset];
                var rangeStartStr = sections[++offset];
                var numberAssignedStr = sections[++offset];
                var dateAssignedStr = sections[++offset];
                var status = sections[++offset];
                var instances = sections[++offset];

                if (limitLocaions.Length > 0 && !limitLocaions.Contains(place.ToLower()))
                {
                    continue;
                }

                if (limitTypes.Length > 0 && !limitTypes.Contains(type.ToLower()))
                {
                    continue;
                }

                var numberAssigned = uint.Parse(numberAssignedStr);
                var numberAssignedMinus1 = numberAssigned - 1;

                if (type.Equals("ipv4", StringComparison.OrdinalIgnoreCase))
                {
                    var significantBitsReverse = (int)Math.Log(numberAssigned, 2);

                    var originalBits = (int)Math.Pow(2, significantBitsReverse);
                    var significantBits = 32 - significantBitsReverse;

                    if (originalBits != numberAssigned)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                    var ipAddressParts = rangeStartStr.Split('.');
                    byte[] parts = new byte[4];
                    for (int index = 0; index < ipAddressParts.Length; index++)
                    {
                        var ipAddressPart = ipAddressParts[index];
                        parts[index] = byte.Parse(ipAddressPart);
                    }

                    var ipAddress = new IPAddress(parts);

                    var ipAddressAsInt = ((uint)parts[3] << 24 | (uint)parts[2] << 16 | (uint)parts[1] << 8 | (uint)parts[0]);

                    var endPower = ReverseBytes(numberAssignedMinus1);
                    var startPower = ~endPower;

                    var start = startPower & ipAddressAsInt;
                    var end = start | endPower;

                    var mask = new IPAddress(startPower);

                    var startReversed = ReverseBytes(start);
                    var endReversed = ReverseBytes(end);

#if DEBUG
                    ranges.AddNewRange(startReversed, endReversed);
#endif

                    var startIp = new IPAddress(start);
                    var endIp = new IPAddress(end);

                    if (!shortOutput)
                    {
                        Write(line + "|");
                        Write(startIp + "|");
                        Write(endIp + "|");
                        Write(startIp + "/" + mask + "|");
                    }
                    Write(rangeStartStr + "/" + significantBits);

                    if (rangeStartStr != startIp.ToString())
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (type.Equals("ipv6", StringComparison.OrdinalIgnoreCase))
                {
                    if (!shortOutput)
                    {
                        Write(line + "|");
                    }
                    Write(rangeStartStr + "/" + numberAssigned);
                }
                else if (type.Equals("asn", StringComparison.OrdinalIgnoreCase))
                {
                    if (!shortOutput)
                    {
                        Write(line + "|");
                    }
                    Write(rangeStartStr + "+" + numberAssignedMinus1);
                }

                WriteSeparator();
            }

#if DEBUG
            WriteLine("Filled Ranges");
            foreach (var range in ranges.FilledRanges)
            {
                var total = range.Total;
                var startIp = new IPAddress(ReverseBytes(range.Start));
                var endIp = new IPAddress(ReverseBytes(range.End));

                WriteWithSeparator(startIp + "-" + endIp  + "-" + total);
            }

            WriteLine("Missing Ranges");
            foreach (var range in ranges.MissingRanges)
            {
                var total = range.Total;
                var startIp = new IPAddress(ReverseBytes(range.Start));
                var endIp = new IPAddress(ReverseBytes(range.End));

                WriteWithSeparator(startIp + "-" + endIp + "-" + total);
            }
#endif

            SetClipboard(sb.ToString());

            Console.ReadKey();
        }

        private static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        private static string Separator = "\r\n";

        private static void WriteSeparator()
        {
            sb.Append(Separator);
            Console.Write(Separator);
        }

        private static void WriteLine(string value = null)
        {
            value = value ?? string.Empty;
            sb.AppendLine(value);
            Console.WriteLine(value);
        }

        private static void WriteWithSeparator(string value = null)
        {
            Write((value ?? string.Empty) + Separator);
        }

        private static void Write(string value)
        {
            sb.Append(value);
            Console.Write(value);
        }

        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        static void SetClipboard(string value)
        {
            if( OpenClipboard(IntPtr.Zero))
            {
                if( EmptyClipboard())
                {
                    var ptr = Marshal.StringToHGlobalUni(value);
                    if(!SetClipboardData(13, ptr))
                    {
                        // Only free the data if setting the clipboard wasn't successful, the memory is managed by the system once it is set
                        Marshal.FreeHGlobal(ptr);
                    }
                    if (!CloseClipboard())
                    {
#if DEBUG
                        Console.Error.WriteLine("Could not close the clipboard");
#endif
                    }
                }
            }
        }
    }
}
