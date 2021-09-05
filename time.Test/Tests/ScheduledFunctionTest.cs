using System;
using time.Functions.Functions;
using time.Test.Helpers;
using Xunit;

namespace time.Test.Tests
{
    public class ScheduledFunctionTest
    {

        [Fact]
        public void SchudeledFunction_Should_Log_Message()
        {
            //Arrenge
            MockCloudTableTimes mockTimes = new MockCloudTableTimes(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            MockCloudTableTimes mockConsolidated = new MockCloudTableTimes(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));

            ListLogger logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);


            //Act
            ScheduledFunction.Run(null, mockTimes, mockConsolidated, logger);
            string message = logger.Logs[0];

            //Assert
            Assert.Contains("Starting consolidate", message);

        }
    }
}
