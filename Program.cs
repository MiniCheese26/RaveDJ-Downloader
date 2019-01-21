using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using NAudio.Wave;

namespace RaveDJ_Downloader
{
    class Program
    {
        public static string downloadFolder;
        public static string title;
        public static string videoURL;
        public static WebClient webC = new WebClient();

        static void Main()
        {
            Console.WriteLine("Enter URL");
            string URL = Console.ReadLine();

            while (CheckIfRaveDJLink(URL) == false)
            {
                Console.WriteLine("\nEnter a Rave.dj link");

                URL = Console.ReadLine();
            }

            var urlID = URL.Split('/');

            string jsonURL = "https://api.red.wemesh.ca/mashups/" + urlID[3];

            while (WebExceptionCatch(jsonURL) == false)
            {
                Console.WriteLine("\nURL is not valid");

                URL = Console.ReadLine();

                urlID = URL.Split('/');

                jsonURL = "https://api.red.wemesh.ca/mashups/" + urlID[3];
            }

            webC.Encoding = Encoding.UTF8;

            string jsonContent = webC.DownloadString(jsonURL);

            dynamic jsonObject = JObject.Parse(jsonContent);

            if (jsonObject.data.maxUrl == "")
            {
                Console.WriteLine("\nVideo isn't finished");
                Console.ReadKey();
                Console.Clear();
                Main();
            }

            videoURL = jsonObject.data.maxUrl;
            title = jsonObject.data.title;
            title = title + ".mp4";

            DownloadFolderProcess();
        }

        static void DownloadFolderProcess()
        {
            string folderTitle = Regex.Replace(title, ".mp4", "");
            string downloadPath = Path.GetFullPath(Directory.GetCurrentDirectory()) + @"\Downloads";

            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
                Directory.CreateDirectory(downloadPath + @"\" + folderTitle);
                downloadFolder = downloadPath + @"\" + folderTitle;
                DownloadFunction();
            }
            else
            {
                Directory.CreateDirectory(downloadPath + @"\" + folderTitle);
                downloadFolder = downloadPath + @"\" + folderTitle;
                DownloadFunction();
            }
        }

        static void DownloadFunction()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; }; //Site for hosting uses an expired certificate at the time of coding

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

            DownloadDone();
        }

        static void DownloadDone()
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

        static bool WebExceptionCatch(string URL)
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

        static bool CheckIfRaveDJLink(string URL)
        {
            if (!URL.Contains("https://rave.dj/"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        static void ConvertAudio(string fileLocation)
        {
            string fileMP3Extension = Regex.Replace(fileLocation, ".mp4", ".mp3");

            var inputFile = fileLocation;
            var outputFile = fileMP3Extension;

            using (var videoFile = new MediaFoundationReader(inputFile))
            {
                MediaFoundationEncoder.EncodeToMp3(videoFile, fileMP3Extension);
            }

            Console.WriteLine("\nDone");

            Console.WriteLine("Would you like to delete the mp4? y/n");
            string deleteMP4 = Console.ReadLine().ToLower();

            if (deleteMP4 == "y")
            {
                File.Delete(fileLocation);
                Console.WriteLine("\nDeleted!");
                Console.ReadKey();
            }

            Console.Clear();
            Main();
        }
    }
}
