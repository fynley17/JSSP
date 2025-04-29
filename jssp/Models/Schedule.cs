using System.Collections.Generic;

namespace jssp.Models
{
    public class Schedule
    {
        public List<int> JobOrder { get; set; } = new();

        public int Fitness { get; set; } = int.MaxValue;
        public List<Job> Jobs { get; set; }

        public Schedule Clone()
        {
            return new Schedule
            {
                JobOrder = new List<int>(JobOrder),
                Fitness = Fitness
            };
        }
    }
}