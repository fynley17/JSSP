using System.Collections.Generic;

namespace Models
{
    public class Job
    {
        public int JobId { get; set; }
        public List<Operation> Operations { get; set; } = new();
        public int NextOperationIndex { get; set; } = 0;

        public bool IsNextOperation => NextOperationIndex < Operations.Count;

        public Operation NextOperation() => Operations[NextOperationIndex++];

        public void ResetOpIndex() => NextOperationIndex = 0;
    }
}
