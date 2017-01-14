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
        static void Main(string[] args)
        {
            var p = new Program();
            try
            {
                p.RunProgram(args);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.Message);
                try
                {
                    p._cleanupWriter?.Dispose();
                }
                catch
                {
                    // ignore
                }
            }
        }

        private Program()
        {
        }

        public WriterHelper WriterHelper
        {
            get { return _writerHelper; }
        }

        public void RunProgram(string[] args)
        {
            CommandLineOptions options;
            if (!TryOptions(args, out options))
            {
                return;
            }

            SetupWriteHelper(options);

            // The readme file for the original apnic source file is ftp://ftp.apnic.net/pub/apnic/stats/apnic/README.TXT
            // data source file downloaded from ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest by default

            if (options.Filename.Contains("://"))
            {
                var tempFile = "delegated-apnic-extended-latest.temp";
                var tempCachedFile = options.CacheFileName;
                var tempCacheInfo = new FileInfo(tempCachedFile);
                if (!(options.NoDownloadFile) && (!tempCacheInfo.Exists || tempCacheInfo.LastWriteTime < DateTime.Now.AddDays(-1)))
                {
                    using (var client = new WebClient())
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }
                        client.DownloadFile(options.Filename, tempFile);
                        if (tempCacheInfo.Exists)
                        {
                            tempCacheInfo.Delete();
                        }

                        File.Move(tempFile, tempCachedFile);
                    }
                }
                options.Filename = tempCachedFile;
            }

            if (!File.Exists(options.Filename))
            {
                Console.Error.WriteLine("File to process does not exist, try using --help to get usage");
                return;
            }

            if (options.ExcludeProcessingFile)
            {
                return;
            }

            var lines = File.ReadAllLines(options.Filename);
            lines = lines.Where(l => !l.StartsWith("#") && !l.Contains("*") && !l.Contains("-") && !l.Contains("+")).ToArray();

#if DEBUG
            RangeGaps ranges = new RangeGaps();
#endif
            Dictionary<string, int> locationList = new Dictionary<string, int>();

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


                if (options.LimitTypes.Any() && !options.LimitTypes.Contains(type.ToLower()))
                {
                    continue;
                }

                if (options.LocationOnly)
                {
                    if (!locationList.ContainsKey(place))
                    {
                        locationList.Add(place, 1);
                    }
                    else
                    {
                        locationList[place] = locationList[place] + 1;
                    }
                    continue;
                }

                if (options.LimitLocations.Any() && !options.LimitLocations.Contains(place.ToLower()))
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

                    if (options.Verbose)
                    {
                        WriterHelper.Write(line + "|");
                        WriterHelper.Write(startIp + "|");
                        WriterHelper.Write(endIp + "|");
                        WriterHelper.Write(startIp + "/" + mask + "|");
                    }
                    WriterHelper.Write(rangeStartStr + "/" + significantBits);

                    if (rangeStartStr != startIp.ToString())
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (type.Equals("ipv6", StringComparison.OrdinalIgnoreCase))
                {
                    if (options.Verbose)
                    {
                        WriterHelper.Write(line + "|");
                    }
                    WriterHelper.Write(rangeStartStr + "/" + numberAssigned);
                }
                else if (type.Equals("asn", StringComparison.OrdinalIgnoreCase))
                {
                    if (options.Verbose)
                    {
                        WriterHelper.Write(line + "|");
                    }
                    WriterHelper.Write(rangeStartStr + "+" + numberAssignedMinus1);
                }

                WriterHelper.WriteSeparator();
            }

            if (options.LocationOnly)
            {
                foreach (var location in locationList.OrderBy(i => i.Key))
                {
                    if (options.Verbose)
                    {
                        WriterHelper.WriteWithSeparator(location.Key + "(" + location.Value + ")");
                    }
                    else
                    {
                        WriterHelper.WriteWithSeparator(location.Key);
                    }
                }
            }

#if DEBUG
            WriterHelper.WriteLine();
            WriterHelper.WriteLine("Filled Ranges");
            foreach (var range in ranges.FilledRanges)
            {
                var total = range.Total;
                var startIp = new IPAddress(ReverseBytes(range.Start));
                var endIp = new IPAddress(ReverseBytes(range.End));

                WriterHelper.WriteWithSeparator(startIp + "-" + endIp + "-" + total);
            }

            WriterHelper.WriteLine();
            WriterHelper.WriteLine("Missing Ranges");
            foreach (var range in ranges.MissingRanges)
            {
                var total = range.Total;
                var startIp = new IPAddress(ReverseBytes(range.Start));
                var endIp = new IPAddress(ReverseBytes(range.End));

                WriterHelper.WriteWithSeparator(startIp + "-" + endIp + "-" + total);
            }
#endif

            if (!options.NoClipboardSet && WriterHelper?.OutputBuilder != null)
            {
                ClipboardHelper.SetClipboard(WriterHelper?.OutputBuilder?.ToString());
            }

            _cleanupWriter?.Dispose();
        }

        private TextWriter _cleanupWriter = null;

        private bool SetupWriteHelper(CommandLineOptions options)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(options.OutputFile))
                {
                    _cleanupWriter = File.CreateText(options.OutputFile);
                }

                _writerHelper = new WriterHelper(!options.NoConsolePrint, !options.NoClipboardSet, _cleanupWriter);
                _writerHelper.Separator = options.Separator;
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.Message);
                return false;
            }
            return true;
        }

        private bool TryOptions(string[] args, out CommandLineOptions options)
        {
            options = new CommandLineOptions();
            var success = options.TryParse(args);

            if (!success || options.ShouldShowHelp)
            {
                if (!success)
                {
                    Console.WriteLine(options.ParseMessage);
                }
                options.Options.WriteOptionDescriptions(Console.Out);
                return false;
            }
            return true;
        }

        private WriterHelper _writerHelper;

        private static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
    }
}
