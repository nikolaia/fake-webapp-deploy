using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace MyApp.Functions
{
    public class HttpTrigger
    {
        private class Payload {
            public string CustomerNumber { get; set; }
            public string Status { get; set; }
        }
        public static IActionResult Run(HttpRequest req, TraceWriter log, out TableEntity invite)
        {
            log.Info("C# HTTP trigger function processed a request.");
    
            using (var reader = new StreamReader(req.Body))
            {
                var body = reader.ReadToEnd();
                var payload = JsonConvert.DeserializeObject<Payload>(body);
                

                if (payload != null) {
                    invite = new TableEntity() {
                        PartitionKey = payload.CustomerNumber,
                        RowKey = payload.Status
                    };
                    return new OkObjectResult("Message recieved");
                }

                invite = null;
                return new BadRequestObjectResult("Please pass a valid message");
            }
        }
    }
}