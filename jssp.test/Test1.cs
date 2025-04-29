namespace jssp.test
{
    using jssp.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;

    [TestClass]
    public class TestOperation
    {
        [TestMethod]
        public void TestOperations()
        {
            // Arrange
            var job = new Job
            {
                JobId = 1,
                Operations = new List<Operation>
                {
                    new Operation { OperationId = 1, ProcessingTime = 5 },
                    new Operation { OperationId = 2, ProcessingTime = 3 }
                }
            };

            // Act
            var nextOperation = job.NextOperation();
            var nextOperationAfterFirst = job.NextOperation();

            // Assert
            Assert.AreEqual(1, nextOperation.OperationId);
            Assert.AreEqual(2, nextOperationAfterFirst.OperationId);
        }

        [TestMethod]
        public void TestOperationProperties()
        {
            // Arrange
            var operation = new Operation
            {
                JobId = 1,
                OperationId = 1,
                Subdivision = "A",
                ProcessingTime = 5,
                StartTime = 10
            };
            // Act & Assert
            Assert.AreEqual(1, operation.JobId);
            Assert.AreEqual(1, operation.OperationId);
            Assert.AreEqual("A", operation.Subdivision);
            Assert.AreEqual(5, operation.ProcessingTime);
            Assert.AreEqual(10, operation.StartTime);
            Assert.AreEqual(15, operation.EndTime); // EndTime is StartTime + ProcessingTime
        }
    }

    [TestClass]
    public class TestSchedule
    {
        [TestMethod]
        public void TestScheduleCreation()
        {
            // Arrange
            var job = new Job
            {
                JobId = 1,
                Operations = new List<Operation>
                {
                    new Operation { OperationId = 1, ProcessingTime = 5 },
                    new Operation { OperationId = 2, ProcessingTime = 3 }
                }
            };
            var schedule = new Schedule
            {
                Jobs = new List<Job> { job },
                Fitness = 0
            };
            // Act & Assert
            Assert.AreEqual(1, schedule.Jobs.Count);
            Assert.AreEqual(0, schedule.Fitness);
        }
    }
}
