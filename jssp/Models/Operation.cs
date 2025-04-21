namespace Models
{
    public class Operation
    {
        public int JobId { get; set; }
        public int OperationId { get; set; }
        public string Subdivision { get; set; } = string.Empty;
        public int ProcessingTime { get; set; }

        // Computed during evaluation
        public int StartTime { get; set; }
        public int EndTime => StartTime + ProcessingTime;
    }
}
