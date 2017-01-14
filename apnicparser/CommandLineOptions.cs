using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;

namespace apnicparser
{
    public class CommandLineOptions
    {
        private OptionSet GetOptionSet()
        {
            var options = new OptionSet {
                { "f|filename=", "the name of the filename to read from", n => Filename = n },
                { "l|location=", "a location to limit the details to", n => LimitLocations.Add(n?.ToLower()) },
                { "t|type=", "a type to limit the details to", n => LimitTypes.Add(n?.ToLower()) },
                { "v", "make the output be in a verbose mode", o => Verbose = o != null },
                { "h|help", "show this message and exit", o => ShouldShowHelp = o != null },
                { "s|separator=", "provide a custom separator for the entries", UseCustomSeparator},
                { "n", "use new line as a separator", o => { if (o != null) { UseCustomSeparator("\r\n"); }}},
                //{ "country-only", "show just the country information", o => ShowOnlyCountry = o},
                { "L|no-location-limit", "no limit on the location", o => NoLocationLimit = o != null},
                { "T|no-type-limit", "no limit on the types", o => NoTypeLimit = o != null},
                { "X|no-console-print", "dont print the output to the console", o => NoConsolePrint = o != null},
                { "Z|no-clipboard-set", "dont set the clipboard", o => NoClipboardSet = o != null},
                { "o|outfile=", "write the output to a file", o => OutputFile = o},
                { "d|download", "download the file even if not processing file", o => DownloadFile = o != null},
                { "D|no-download", "dont download the file", o => NoDownloadFile = o != null},
                { "P|no-process", "dont process the file", o => ExcludeProcessingFile = o != null},
                { "c|cache-file=", "set the name of the cache file, the default is delegated-apnic-extended-latest.cached", o => CacheFileName = o},
                //{ "e|cache-file-expiry", "sets the cache file expiry time span, should be in the format Days:", o => CacheFileName = o},

            };
            return options;
        }

        public bool TryParse(IEnumerable<string> args)
        {
            try
            {
                var sb = new StringBuilder();

                UnparsedValues = Options.Parse(args);
                //if (MultipleCustomSeparatorsSpecified)
                //{
                //    ParseMessage = "Multiple custom separators specified";
                //    return false;
                //}

                if (!NoTypeLimit && !LimitTypes.Any())
                {
                    LimitTypes.Add("ipv4");
                }

                if (!NoLocationLimit && !LimitLocations.Any())
                {
                    LimitLocations.Add("au");
                }

                if (Filename?.Equals(":", StringComparison.InvariantCultureIgnoreCase) ?? true)
                {
                    Filename = "ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest";
                }

                if (NoClipboardSet && NoConsolePrint && OutputFile == null)
                {
                    if (NoDownloadFile)
                    {
                        ParseMessage = "Options given means that the program doesn't do anything, and no-download specified";
                        return false;
                    }

                    if ( !DownloadFile)
                    {
                        ParseMessage = "Options given means that the program doesn't do anything, Use -d to download the file if required";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exc)
            {
                ParseException = exc;
                ParseMessage = exc.Message;
                return false;
            }
        }
        
        public bool Verbose { get; set; } = false;
        public bool ShouldShowHelp { get; set; } = false;
        public string Filename { get; set; } = "ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest";
        public string Separator { get; set; } = " ";

        public List<string> LimitLocations { get; set; }

        public List<string> LimitTypes { get; set; }

        public OptionSet Options { get; set; }

        public bool MultipleCustomSeparatorsSpecified { get; set; }

        public bool UsingCustomSeparator { get; set; }

        public List<string> UnparsedValues { get; set; }

        public Exception ParseException { get; set; }

        public string ParseMessage { get; set; }

        public CommandLineOptions()
        {
            LimitLocations = new List<string>();
            LimitTypes = new List<string>();
            Options = GetOptionSet();
        }

        private void UseCustomSeparator(string separator)
        {
            Separator = separator;
            if (UsingCustomSeparator)
            {
                MultipleCustomSeparatorsSpecified = true;
                ShouldShowHelp = true;
            }
            else
            {
                UsingCustomSeparator = true;
            }
        }

        public bool ExcludeProcessingFile { get; set; }

        public bool DownloadFile { get; set; }

        public bool NoDownloadFile { get; set; }

        public string CacheFileName { get; set; } = "delegated-apnic-extended-latest.cached";

        public string OutputFile { get; set; }

        public bool NoClipboardSet { get; set; }

        public bool NoConsolePrint { get; set; }

        public bool NoTypeLimit { get; set; }

        public bool NoLocationLimit { get; set; }

        // Not implemented yet
        //public bool ShowOnlyCountry { get; set; }
    }
}
