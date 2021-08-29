using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using time.Common.Models;
using time.Functions.Entities;

namespace time.Test.Helpers
{
   public class TestFactory
    {
        public static TimeEntity GetTimeEntity()
        {
            return new TimeEntity
            {
                ETag = "*",
                PartitionKey = "TODO",
                RowKey = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                Consolidate = false,
                IdEmployee = "100.",
                Type = 1
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid timeId, Time timeRequest)
        {
            string request = JsonConvert.SerializeObject(timeRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{timeId}"
            };
        }
        public static DefaultHttpRequest CreateHttpRequest(Guid timeId)
        {
            
            return new DefaultHttpRequest(new DefaultHttpContext())
            {  
                Path = $"/{timeId}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Time timeRequest)
        {
            string request = JsonConvert.SerializeObject(timeRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request)
            };
        }

        public static DefaultHttpRequest CreateHttpRequest()
        {
            return new DefaultHttpRequest(new DefaultHttpContext());
        }

        public static Time GetTimeRequest()
        {
            return new Time
            {
                Date = DateTime.UtcNow,
                Consolidate = false,
                IdEmployee = "1000.",
                Type = 1

            };

        }

        public static Stream GenerateStreamFromString(string stringToConvert)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter write = new StreamWriter(stream);
            write.Write(stringToConvert);
            write.Flush();
            stream.Position = 0;
            return stream;
        }

        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger,
                if(type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }

            return logger;
        }
    }
}
