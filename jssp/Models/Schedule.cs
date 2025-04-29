using System.Collections.Generic;

namespace jssp.Models
{
    public class Schedule
    {
        public List<int> JobOrder { get; set; } = new();

        public int Fitness { get; set; } = int.MaxValue;

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