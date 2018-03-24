using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using MediaToolkit.Model;
using MediaToolkit;
using System.Text.RegularExpressions;

namespace RaveDJ_Downloader
{
    class Program
    {
        public static string downloadFolder;

        static void Main()
        {
            Console.WriteLine("Enter URL");
            string URL = Console.ReadLine();

            while (CheckURL(URL) == false)
            {
                Console.WriteLine("\nURL is not valid");

                URL = Console.ReadLine();
            }

            var urlID = URL.Split('/');

            string jsonURL = "https://api.red.wemesh.ca/mashups/" + urlID[3];

            WebClient webC = new WebClient();

            string jsonContent = webC.DownloadString(jsonURL);

            dynamic jsonObject = JObject.Parse(jsonContent);

            if (jsonObject.data.maxUrl == "")
            {
                Console.WriteLine("\nVideo isn't finished");
                Console.ReadKey();
                Console.Clear();
                Main();
            }

            string videoURL = jsonObject.data.maxUrl;
            string title = jsonObject.data.title;
            title = title + ".mp4";

            string localJsonText = File.ReadAllText(Path.GetFullPath(Directory.GetCurrentDirectory()) + @"\settings.json");

            dynamic localJson = JObject.Parse(localJsonText);

            if (localJson.location == "")
            {
                Console.WriteLine("\nSetup a default downloader folder? y/n");
                string defaultFolderPrompt = Console.ReadLine().ToLower();

                if (defaultFolderPrompt == "y")
                {
                    bool firstTimeCheck = true;
                    downloadFolder = DefaultDownloadLocation(firstTimeCheck);
                }
                else
                {
                    Console.WriteLine("\nEnter download folder");
                    downloadFolder = Console.ReadLine();

                    while (!Directory.Exists(downloadFolder))
                    {
                        Console.WriteLine("\nDirectory does not exist, try again");

                        downloadFolder = Console.ReadLine();
                    }
                }
            }
            else if (localJson.location != "") //Using else caused a weird '}' missing error
            {
                bool firstTimeCheck = false;
                downloadFolder = DefaultDownloadLocation(firstTimeCheck);
            }

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; //Site for hosting uses an expired certificate at the time of coding

            if (File.Exists(downloadFolder + @"\" + title))
            {
                Console.WriteLine("\nFile already exists");
                Console.ReadKey();
                Console.Clear();
                Main();
            }
            else
            {
                Console.WriteLine("\nDownloading...");
                webC.DownloadFile(videoURL, downloadFolder + @"\" + title);
            }

            DownloadDone(title);
        }
        
        static void DownloadDone(string title)
        {
            Console.WriteLine("\nDone");
            Console.WriteLine("1. to enter a new link or 2. to convert to mp3");
            string endChoiceStr = Console.ReadLine();

            while (!Int32.TryParse(endChoiceStr, out int n))
            {
                Console.WriteLine("\n1 or 2");

                endChoiceStr = Console.ReadLine();
            }

            while (Int32.Parse(endChoiceStr) > 2)
            {
                Console.WriteLine("\n1 or 2");

                endChoiceStr = Console.ReadLine();
            }

            int endChoice = Int32.Parse(endChoiceStr);

            switch (endChoice)
            {
                case 1:
                    Console.Clear();
                    Main();
                    break;
                case 2:
                    ConvertAudio(downloadFolder + @"\" + title);
                    break;
            }
        }

        static bool CheckURL(string URL)
        {
            if (!URL.Contains("https://rave.dj/") && URL.Length > 16)
            {
                return false;
            }
            else
            {
                try
                {
                    HttpWebRequest req = WebRequest.Create(URL) as HttpWebRequest;
                    req.Method = "HEAD";
                    HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                    response.Close();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        static void ConvertAudio(string fileLocation)
        {
            string fileMP3Extension = Regex.Replace(fileLocation, ".mp4", ".mp3");

            var inputFile = new MediaFile { Filename = fileLocation };
            var outputFile = new MediaFile { Filename = fileMP3Extension };

            using (var engine = new Engine())
            {
                Console.WriteLine("\nConverting...");
                engine.Convert(inputFile, outputFile);
            }

            Console.WriteLine("\nDone");
            Console.ReadKey();
            Console.Clear();
            Main();
        }

        static string DefaultDownloadLocation(bool firstTimeCheck)
        {
            if (firstTimeCheck == true)
            {
                Console.WriteLine("\nEnter your default directory");
                string defaultDir = Console.ReadLine();

                while (!Directory.Exists(defaultDir))
                {
                    Console.WriteLine("\nDirectory doesn't exist");

                    defaultDir = Console.ReadLine();
                }

                string jsonText = File.ReadAllText(Directory.GetCurrentDirectory() + @"\settings.json");
                dynamic jsonObj = JsonConvert.DeserializeObject(jsonText);
                jsonObj["location"] = defaultDir;
                string jsonWrite = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(Path.GetFullPath(Directory.GetCurrentDirectory()) + @"\settings.json", jsonWrite);
                return defaultDir;
            }
            else
            {
                string defaultDirectoryText = File.ReadAllText(Path.GetFullPath(Directory.GetCurrentDirectory()) + @"\settings.json");

                dynamic defaultDirectoryParse = JObject.Parse(defaultDirectoryText);

                string defaultDirectory = defaultDirectoryParse.location;

                return defaultDirectory;
            }
        }
    }
}
