using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using Microsoft.Azure.WebJobs.Extensions.Storage;

namespace JoakimSoftware.Functions
{
    public static class GitCheckCS
    {
        [FunctionName("GitCheckCS")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
