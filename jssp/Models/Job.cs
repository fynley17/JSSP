using System.Collections.Generic;

namespace Models
{
    public class Job
    {
        public int JobId { get; set; }
        public List<Operation> Operations { get; set; } = new();
        public int NextOpperationIndex { get; set; } = 0;

        public bool IsNextOperation => NextOpperationIndex < Operations.Count;

        public Operation NextOperation() => Operations[NextOpperationIndex++];

        public void ResetOpIndex() => NextOpperationIndex = 0;
    }
}
