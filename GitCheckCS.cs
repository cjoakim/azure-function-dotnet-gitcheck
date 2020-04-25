using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

using Azure.Storage.Blobs;

// Timer-triggered C# Azure Function which detects changes in a GitHub repositories list.
// Chris Joakim, Microsoft, 2020/04/25
//
// Set these two environment variables in the Azure Function App:
// GITHUB_REST_API_READ_TOKEN        <-- your GitHub token for invoking the REST API
// AZURE_STORAGE_CONNECTION_STRING   <-- the connection string to your Azure Storage Account
//
// Useful Links:
// https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/console-webapiclient
// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
// https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
//
// Implementation Notes:
// 1) Using the System.Text.Json library, rather than Newtonsoft, for JSON serialization.
// 2) Using the Azure.Storage.Blobs library, as this code was first developed in a Console app.
// 3) The only Function bindings are for the trigger itself, not for Storage.
// 4) Class Repo is used for Serialization, not all attributes of the GitHub JSON response are used.


namespace JoakimSoftware.Functions
{
    public static class GitCheckCS
    {
        // Change these three variables per your GitHub account and Azure Storage contents.
        private static string githubUser    = "cjoakim";
        private static string containerName = "github";
        private static string blobName      = "github_repos.json";
        private static List<Repo> repos = new List<Repo>();

        private static ILogger logger = null;

        //public static void 
        [FunctionName("GitCheckCS")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                logger = log;  // assign the injected ILogger to the static variable, for use in other methods

                // First, invoke the GitHub REST API and get the current list of my repos.
                string apiResponseJSON = await GetGitHubRepos();
                //log.LogInformation(apiResponseJSON);
                List<Repo> currentRepos = DeserializeReposJson(apiResponseJSON, "GitHub API");

                // Next, read Azure Blob Storage for the previous GitHub REST API response JSON.
                string blobJSON = ReadBlob();
                List<Repo> previousRepos = DeserializeReposJson(blobJSON, "Blob Storage");

                // Compare the current (HTTP API) vs previous (Blob) GitHub repo contents.
                if (apiResponseJSON != blobJSON)
                {
                    log.LogInformation("Sound the alarm!  Something has changed!");
                    log.LogInformation("currentRepos from GitHub API; count: " + currentRepos.Count);
                    foreach (Repo repo in currentRepos)
                    {
                        log.LogInformation(repo.ToString());
                    }
                    log.LogInformation("previousRepos from Blob; count: " + previousRepos.Count);
                    foreach (Repo repo in previousRepos)
                    {
                        log.LogInformation(repo.ToString());
                    }
                    WriteBlob(apiResponseJSON);
                }
                else
                {
                    log.LogInformation("No differences this run");
                }
            }
            catch (Exception e)
            {
                log.LogInformation("Exception: {0}", e.Message);
            }
        }

        static async Task<string> GetGitHubRepos()
        {
            string reposUrl = "http://api.github.com/users/" + githubUser + "/repos";
            string token = Environment.GetEnvironmentVariable("GITHUB_REST_API_READ_TOKEN");
            Console.WriteLine("GetGitHubRepos; url: {0} token: {1}",reposUrl, token);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(
                new System.Net.Http.Headers.ProductInfoHeaderValue("AppName", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);

            HttpResponseMessage response = await client.GetAsync(reposUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        static List<Repo> DeserializeReposJson(string responseBody, string source)
        {
            List<Repo> repos = JsonSerializer.Deserialize<List<Repo>>(responseBody);
            logger.LogInformation("DeserializeGitHubRepos count: {0} in source: {1}", repos.Count, source);
            return repos;
        }

        static string ReadBlob()
        {
            string connString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            if (blobClient.Exists())
            {
                logger.LogInformation("ReadUpdateBlobStorage; it exists");
                string text;
                using (var memoryStream = new MemoryStream())
                {
                    blobClient.DownloadTo(memoryStream);
                    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                }
                return text;
            }
            else
            {
                // return an empty JSON array if the blob doesn't exist
                return "[]";
            }
        }

        static void WriteBlob(string apiResponseJSON)
        {
            string connString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            blobClient.Upload(StringToStream(apiResponseJSON), true);
            logger.LogInformation("Blob written; " + blobName);
        }

        public static Stream StringToStream(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }

    public class Repo
    {
        public int id { get; set; }
        public string name { get; set; }
        public string full_name { get; set; }
        public string description { get; set; }

        public override string ToString()
        {
            return "Repo; id: " + id + ", name: " + name + ", full_name:" + full_name;
        }
    }
}
