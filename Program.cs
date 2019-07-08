using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine;

namespace OAITester {
    class Program {
        static async Task Main(string[] args) {
            var options = CommandLine.Parser.Default.ParseArguments<Options>(args);
            await options.MapResult(async x => {
                await DoWork(x);
            }, error => Task.CompletedTask);
        }

        public static async Task DoWork(Options options) {
            var baseUri = options.Url;
            var metadataPrefix = options.MetadataPrefix;

            var httpClient = new HttpClient();
            httpClient.BaseAddress = baseUri;

            var resumptionToken = string.Empty;

            do {
                var query = "?verb=ListRecords&resumptionToken=" + resumptionToken;
                if(string.IsNullOrWhiteSpace(resumptionToken))
                    query = "?verb=ListRecords&metadataPrefix=" + metadataPrefix;

                Write(Console.ForegroundColor, "Fetching data from ");
                Write(ConsoleColor.DarkCyan, baseUri + query);
                WriteLine(Console.ForegroundColor, ".");

                var xmlString = await httpClient.GetStringAsync(query);
                var xml = XDocument.Parse(xmlString);
              
                XNamespace ns = "http://www.openarchives.org/OAI/2.0/";

                var resumptionTokenNode = xml.Descendants(ns + "resumptionToken").FirstOrDefault();
                resumptionToken = resumptionTokenNode?.Value;

                if(!string.IsNullOrWhiteSpace(resumptionToken)) {
                    try {
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(resumptionToken));
                        Write(Console.ForegroundColor, "Got resumption token ");
                        Write(ConsoleColor.DarkCyan, resumptionToken);
                        Write(Console.ForegroundColor, ". It was decoded to ");
                        Write(ConsoleColor.DarkCyan, decoded);
                        WriteLine(Console.ForegroundColor, ".");
                    } catch(FormatException) {
                        Write(Console.ForegroundColor, "Got resumption token ");
                        Write(ConsoleColor.DarkCyan, resumptionToken);
                        WriteLine(Console.ForegroundColor, ".");
                    }
                } else {
                    Console.WriteLine("No resumption token, we're done!");
                }
            } while(resumptionToken != null);
        }

        static void Write(ConsoleColor color, string text) {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }

        static void WriteLine(ConsoleColor color, string text) {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }
    }

    public class Options {
        [Option('u', "url",
            Required = true,
            HelpText = "The url to the OAI-PMH service without any query parameters.")]
        public Uri Url { get; set; }

        [Option('m', "metadata-prefix",
            Default = "marcxchange",
            HelpText = "Sets the metadataPrefix parameter. Defaults to marcxchange.")]
        public string MetadataPrefix { get; set; }
    }
}
