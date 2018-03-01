using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.Extensions.Primitives;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace MyApp.Functions
{
    /// <summary>
    /// The output of this function should be an InsertOrMerge/InserOrReplace, but it seems
    /// that this is not yet supported by the WebJobs SDK and is not available in Azure
    /// Functions: https://github.com/Azure/azure-webjobs-sdk/issues/919
    /// </summary>
    public class QueueTrigger
    {
        public static TableEntity Run(TableEntity invite, 
            DateTimeOffset expirationTime, 
            DateTimeOffset insertionTime, 
            DateTimeOffset nextVisibleTime,
            string queueTrigger,
            string id,
            string popReceipt,
            int dequeueCount,
            TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {invite.ToString()}\n" +
                $"queueTrigger={queueTrigger}\n" +
                $"expirationTime={expirationTime}\n" +
                $"insertionTime={insertionTime}\n" +
                $"nextVisibleTime={nextVisibleTime}\n" +
                $"id={id}\n" +
                $"popReceipt={popReceipt}\n" + 
                $"dequeueCount={dequeueCount}");

            return invite;
        }
    }
}
