namespace jssp.test
{
    using jssp.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;

    [TestClass]
    public class Test1
    {
        [TestMethod]
        public void TestJobOperations()
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
    }
}
