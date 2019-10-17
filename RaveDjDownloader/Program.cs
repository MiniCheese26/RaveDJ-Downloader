using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RaveDjDownloader
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                Environment.Exit(1);
            }
            
            string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Failed to find config.json");
                Environment.Exit(1);
            }

            Configuration? configuration = DeserializeConfiguration(configPath);

            if (configuration == null)
            {
                Console.WriteLine("Failed to parse configuration");
                Environment.Exit(1);
            }

            var downloadManager = new DownloadManager();
            await downloadManager.StartDownloads(configuration, args);
        }

        private static Configuration? DeserializeConfiguration(string configPath)
        {
            try
            {
                string configFile = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<Configuration>(configFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            return null;
        }

        private static void Help()
        {
            Console.WriteLine("Usage: dotnet raveDjDownloader.dll <URL1> <URL2> ...");
        }
    }
}