using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using time.Functions.Entities;

namespace time.Functions.Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static async Task Run(
            [TimerTrigger("0 */3 * * * *")] TimerInfo myTimer,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            [Table("consolidatedTimes", Connection = "AzureWebJobsStorage")] CloudTable consolidateTimesTable,
            ILogger log)
        {
            log.LogInformation($"Starting consolidate information at: {DateTime.Now}");

            // Registers not consolidated 
            string timeFilter = TableQuery.GenerateFilterConditionForBool(nameof(TimeEntity.Consolidate), QueryComparisons.Equal, false);
            TableQuery<TimeEntity> timeQuery = new TableQuery<TimeEntity>().Where(timeFilter);
            List<TimeEntity> times = (await timeTable.ExecuteQuerySegmentedAsync(timeQuery, null)).OrderBy((x) => x.Date).ToList();

            // Created registers
            int created = 0;
            // Updated Registers
            int updated = 0;

            // Employees to update
            List<string> employees = times.Select((time) => time.IdEmployee).Distinct().ToList();

            foreach (string employee in employees)
            {
                // Registers by employee
                List<TimeEntity> employeeTimes = times.Where((time) => time.IdEmployee == employee).ToList();
                List<string> result = new List<string>();

                // if employee haven't checkout 
                if (employeeTimes.Count % 2 == 1)
                {
                    employeeTimes.RemoveAt(employeeTimes.Count - 1);
                }

                foreach (TimeEntity time in employeeTimes)
                {
                    DateTime timeDate = time.Date.Date.ToUniversalTime();

                    
                    if (result.FirstOrDefault((date) => date.Equals(timeDate.ToString())) != null)
                    {
                        continue;
                    }

                    // Consolidated by employee and date
                    string consolidatedDateFilter = TableQuery.GenerateFilterConditionForDate(nameof(ConsolidatedEntity.Date), QueryComparisons.GreaterThanOrEqual, timeDate);
                    string consolidatedEmployeeFilter = TableQuery.GenerateFilterCondition(nameof(ConsolidatedEntity.IdEmployee), QueryComparisons.Equal, employee);

                    TableQuery<ConsolidatedEntity> consolidatedQuery = new TableQuery<ConsolidatedEntity>().Where(TableQuery.CombineFilters(consolidatedEmployeeFilter, TableOperators.And, consolidatedDateFilter));
                    TableQuerySegment<ConsolidatedEntity> consolidateds = await consolidateTimesTable.ExecuteQuerySegmentedAsync(consolidatedQuery, null);
                    ConsolidatedEntity consolidated = consolidateds.FirstOrDefault();

                    List<TimeEntity> timeRegisters = times.Where((x) => x.IdEmployee == employee && x.Date.Date == timeDate).ToList();

                    
                    int minutes = timeRegisters.Aggregate(new string[] { "", "0" }, (acum, row) =>
                    {
                        if (row.Type == 0)
                        {
                            return new string[] { row.Date.ToString(), acum[1] };
                        }

                        DateTime date = DateTime.Parse(acum[0]);
                        int counter = int.Parse(acum[1]);

                        TimeSpan time = row.Date - date;
                        counter += (int)time.TotalMinutes;

                        return new string[] { "", counter.ToString() };
                    },
                    (acum) => int.Parse(acum[1]));

                    // Write into consolidated table
                    if (consolidated == null)
                    {
                        TableOperation create = TableOperation.Insert(new ConsolidatedEntity
                        {
                            Date = timeDate,
                            ETag = "*",
                            IdEmployee = employee,
                            TimeWorked = minutes,
                            PartitionKey = "CONSOLIDATED",
                            RowKey = Guid.NewGuid().ToString()
                        });

                        await consolidateTimesTable.ExecuteAsync(create);
                        created++;
                    }
                    else
                    {
                        consolidated.TimeWorked += minutes;
                        TableOperation update = TableOperation.Replace(consolidated);

                        await consolidateTimesTable.ExecuteAsync(update);
                        updated++;
                    }

                    foreach (TimeEntity timeRegister in timeRegisters)
                    {
                        // Update consolidated 
                        timeRegister.Consolidate = true;

                        TableOperation update = TableOperation.Replace(timeRegister);
                        await timeTable.ExecuteAsync(update);
                    }

                    // Add to processed list
                    result.Add(timeTable.ToString());
                }
            }

            log.LogInformation($"{created} new records have been consolidated and {updated} have been updated.");
        }
    }
}
