using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;

namespace SimpleCrawler
{
    class Program
    {

        static List<string> ScannedWebsiteResults = new List<string>();

        static async Task Main(string[] args)
        {

            try
            {
                while (true)
                {
                    Console.WriteLine("Session Started : ");

                    var watch = Stopwatch.StartNew();

                    //Reading websites from text file
                    string[] websites = File.ReadAllLines(@"C:\Users\nyada\Desktop\New folder\\Assignment\websites.txt");

                    await RunScanParallelAsync(websites);

                    watch.Stop();

                    var elapsedMs = watch.ElapsedMilliseconds;

                    Console.WriteLine("Total execution time: " + elapsedMs);

                    StorageFinalData();

                    Console.ReadLine();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        static async Task RunScanParallelAsync(string[] websites)
        {
            List<Task<WebsiteDataModel>> tasks = new List<Task<WebsiteDataModel>>();

            //Download & Scan the content of each website 
            foreach (string website in websites)
            {
                tasks.Add(DownloadAndScanWebsiteAsync(website));
            }

            var results = await Task.WhenAll(tasks);

            //Convert each each record to Json
            foreach (var item in results)
            {
                var json = new JavaScriptSerializer().Serialize(new ScannedWebsiteResult() { Website = item.WebsiteUrl, Google = item.Google, ScanStarted = item.ScanStarted, ScanCompleted = item.ScanCompleted });
                ScannedWebsiteResults.Add(json);
            }

        }

        static async Task<WebsiteDataModel> DownloadAndScanWebsiteAsync(string websiteURL)
        {
            WebsiteDataModel output = new WebsiteDataModel();
            output.WebsiteUrl = websiteURL;
            output.ScanStarted = DateTime.Now.ToString("HH:mm:ss");

            try
            {

                using (var client = new WebClient())
                {
                    output.WebsiteData = await client.DownloadStringTaskAsync("http://" + websiteURL + "/");
                    if (output.WebsiteData.Contains("www.google-analytics.com"))
                        output.Google = true; 
                    
                    output.ScanCompleted = DateTime.Now.ToString("HH:mm:ss");
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(websiteURL + " : " + ex.Message);
                output.ScanCompleted = DateTime.Now.ToString("HH:mm:ss");
            }

            return output;
        }

        //Storing final result to Azure Storage
        static void StorageFinalData() 
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference("crawler");

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            foreach (var item in ScannedWebsiteResults)
            {
                CloudQueueMessage message = new CloudQueueMessage(item);
                queue.AddMessage(message);
            }
                
            
        }
    }
}
