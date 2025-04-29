namespace jssp.test
{
    using Algorithms;
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

    [TestClass]
    public class TestSolve
    {
        [TestMethod]
        public void TestSolveProducesValidSchedule()
        {
            // Arrange
            var jobs = new List<Job>
            {
                new Job
                {
                    JobId = 1,
                    Operations = new List<Operation>
                    {
                        new Operation { OperationId = 1, Subdivision = "A", ProcessingTime = 5 },
                        new Operation { OperationId = 2, Subdivision = "B", ProcessingTime = 3 }
                    }
                },
                new Job
                {
                    JobId = 2,
                    Operations = new List<Operation>
                    {
                        new Operation { OperationId = 1, Subdivision = "A", ProcessingTime = 4 },
                        new Operation { OperationId = 2, Subdivision = "C", ProcessingTime = 6 }
                    }
                }
            };

            var ga = new GA(jobs);

            // Act
            var bestSchedule = ga.Solve();

            // Assert
            Assert.IsNotNull(bestSchedule, "The Solve method returned a null schedule.");
            Assert.IsTrue(bestSchedule.Fitness >= 0, "The fitness of the best schedule should be greater than or equal to zero.");

            // Verify that all unique job IDs are present in the JobOrder
            Assert.AreEqual(jobs.Count, bestSchedule.JobOrder.Distinct().Count(), "The number of unique job IDs in the job order should match the number of jobs.");
        }
    }
}
