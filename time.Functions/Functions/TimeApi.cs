using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using time.Common.Models;
using time.Common.Responses;
using time.Functions.Entities;

namespace time.Functions.Functions
{
    public static class TimeApi
    {
        [FunctionName(nameof(CreateRegister))]
        public static async Task<IActionResult> CreateRegister(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "time")] HttpRequest req,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            ILogger log)
        {
            log.LogInformation("Recieved a new Register.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Time time = JsonConvert.DeserializeObject<Time>(requestBody);

            if (string.IsNullOrEmpty(time?.IdEmployee))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a IdEmployee."

                });
            }

            TimeEntity timeEntity = new TimeEntity
            {
                IdEmployee = time.IdEmployee,
                Date = time.Date,
                Type = time.Type,
                ETag = "*",
                Consolidate = false,
                PartitionKey = "TIME",
                RowKey = Guid.NewGuid().ToString(),
            };

            TableOperation addOperation = TableOperation.Insert(timeEntity);
            await timeTable.ExecuteAsync(addOperation);

            string message = "New register stored in Table";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }

        [FunctionName(nameof(UpdateRegister))]
        public static async Task<IActionResult> UpdateRegister(
           [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "time/{id}")] HttpRequest req,
           [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
           string id,
           ILogger log)
        {
            log.LogInformation($"Update Register: {id} received");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Time time = JsonConvert.DeserializeObject<Time>(requestBody);

            //search id in table 
            TableOperation findOperation = TableOperation.Retrieve<TimeEntity>("TIME", id);
            TableResult findResult = await timeTable.ExecuteAsync(findOperation);

            //validate id 
            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Register not found ."

                });
            }

            //Update register
            TimeEntity timeEntity = (TimeEntity)findResult.Result;
            timeEntity.Consolidate = time.Consolidate;
            if (!string.IsNullOrEmpty(time.IdEmployee) && time.Type >= 0)
            {
                timeEntity.Date = time.Date;
                timeEntity.IdEmployee = time.IdEmployee;
                timeEntity.Type = time.Type;

            }
            //execute operation in table
            TableOperation addOperation = TableOperation.Replace(timeEntity);
            await timeTable.ExecuteAsync(addOperation);

            string message = $"Register: {id} Update in Table";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }

        [FunctionName(nameof(GetAllRegister))]
        public static async Task<IActionResult> GetAllRegister(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "time")] HttpRequest req,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            ILogger log)
        {
            log.LogInformation("Get all register Recieved .");

            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>();
            TableQuerySegment<TimeEntity> registers = await timeTable.ExecuteQuerySegmentedAsync(query, null);


            string message = "Retrieved all Registers";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = registers
            });
        }

        [FunctionName(nameof(GetRegisterById))]
        public static IActionResult GetRegisterById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "time/{id}")] HttpRequest req,
            [Table("time","TIME", "{id}", Connection = "AzureWebJobsStorage")] TimeEntity timeEntity,
            string id,
            ILogger log)
        {
            log.LogInformation($"Get Register by id: {id} Received.");

            //validate id 
            if (timeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Register not found ."

                });
            }

            string message = $"Register: {timeEntity.IdEmployee} Retrieved";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }


        [FunctionName(nameof(DeleteRegister))]
        public static async Task<IActionResult> DeleteRegister(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "time/{id}")] HttpRequest req,
           [Table("time", "TIME", "{id}", Connection = "AzureWebJobsStorage")] TimeEntity timeEntity,
           [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
           string id,
           ILogger log)
        {
            log.LogInformation($"Delete Register id: {id}.received");

            //validate id 
            if (timeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Register not found ."

                });
            }

            await timeTable.ExecuteAsync(TableOperation.Delete(timeEntity));
            string message = $"Register: {timeEntity.RowKey} Deleted";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }

    }
}
