using System.Collections.Generic;

namespace Models
{
    public class Job
    {
        public int JobId { get; set; }
        public List<Operation> Operations { get; set; } = new();
        public int NextOpIndex { get; set; } = 0;

        public bool HasNextOperation => NextOpIndex < Operations.Count;

        public Operation NextOperation() => Operations[NextOpIndex++];

        public void Reset() => NextOpIndex = 0;
    }
}
