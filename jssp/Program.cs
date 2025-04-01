using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace jssp
{
    public class JobShop
    {
        private Random rand = new();

        public List<JobOperation> GenerateRandomSchedule(List<List<JobOperation>> jobs)
        {
            var schedule = new List<JobOperation>();
            var allOperations = jobs.SelectMany(job => job).ToList();

            while (allOperations.Count > 0)
            {
                var operation = allOperations[rand.Next(allOperations.Count)];
                allOperations.Remove(operation);
                schedule.Add(operation);
            }

            return schedule;
        }

        public bool EnsureOrder(List<JobOperation> schedule)
        {
            var subdivisionEndTimes = new Dictionary<int, int>();

            foreach (var operation in schedule)
            {
                int subdivisionId = operation.SubdivisionId;
                int startTime = operation.StartTime;

                if (subdivisionEndTimes.ContainsKey(subdivisionId))
                {
                    if (startTime < subdivisionEndTimes[subdivisionId])
                    {
                        return false; // Subdivision is used in parallel
                    }
                }

                subdivisionEndTimes[subdivisionId] = operation.EndTime;
            }

            var jobs = schedule.GroupBy(op => op.JobId);
            foreach (var job in jobs)
            {
                var operations = job.OrderBy(op => op.OperationId).ToList();
                for (int i = 1; i < operations.Count; i++)
                {
                    if (operations[i].StartTime < operations[i - 1].EndTime)
                    {
                        return false; // Operations within a job are not in the correct order
                    }
                }
            }

            return true;
        }
    }
    public class JobProcessor
    {
        public List<List<JobOperation>> ProcessCsv(string filePath)
        {
            var jobs = new List<List<JobOperation>>();
            var subdivisionIds = new Dictionary<string, int>();
            int nextSubdivisionId = 1;

            using (var reader = new StreamReader(filePath))
            {
                // Skip the header line
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    int jobId = int.Parse(values[0]);
                    string subdivision = values[2];
                    int processingTime = int.Parse(values[3]);

                    // Assign a unique ID to the subdivision if it doesn't already have one
                    if (!subdivisionIds.ContainsKey(subdivision))
                    {
                        subdivisionIds[subdivision] = nextSubdivisionId++;
                    }
                    int subdivisionId = subdivisionIds[subdivision];

                    var jobOperation = new JobOperation
                    {
                        JobId = jobId,
                        Subdivision = subdivision,
                        SubdivisionId = subdivisionId,
                        ProcessingTime = processingTime
                    };

                    // Ensure the list is large enough to hold the jobId index
                    while (jobs.Count <= jobId)
                    {
                        jobs.Add(new List<JobOperation>());
                    }

                    // Assign a sequential OperationId within the job
                    jobOperation.OperationId = jobs[jobId].Count;

                    jobs[jobId].Add(jobOperation);
                }
            }

            return jobs;
        }
    }

    public class JobOperation
    {
        public int JobId { get; set; }
        public int OperationId { get; set; }
        public string Subdivision { get; set; }
        public int SubdivisionId { get; set; }
        public int ProcessingTime { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }

    class Files
    {
        public static void directoryExists(string directory_path)
        {
            if (!Directory.Exists(directory_path))
            {
                Console.WriteLine("Directory doesn't exist");
                return;
            }
        }

        public static void filesExists(string[] files)
        {
            if (files.Length == 0)
            {
                Console.WriteLine("No .csv files found in the directory.");
                return;
            }
        }
    }

    public class GA
    {
        private int populationSize;
        private double mutationRate;
        private int maxGenerations;
        private Random rand = new();

        public GA(int populationSize, double mutationRate, int maxGenerations)
        {
            this.populationSize = populationSize;
            this.mutationRate = mutationRate;
            this.maxGenerations = maxGenerations;
        }

        public List<JobOperation> Run(List<List<JobOperation>> jobs)
        {
            // Initialize population
            var population = InitialisePopulation(jobs);

            // Evaluate fitness of initial population
            var fitnessScores = new List<double>();

            // Genetic Algorithm main loop (simplified)
            for (int generation = 0; generation < maxGenerations; generation++)
            {
                // Selection, Crossover, and Mutation steps would go here

                // Evaluate fitness of new population
                fitnessScores.Clear();
                foreach (var schedule in population)
                {
                    fitnessScores.Add(FitnessFunction(schedule));
                }
            }

            // Return the best schedule
            int bestIndex = fitnessScores.IndexOf(fitnessScores.Min());
            return population[bestIndex];
        }

        private List<List<JobOperation>> InitialisePopulation(List<List<JobOperation>> jobs)
        {
            // Initialize the population with random schedules
            var population = new List<List<JobOperation>>();
            var jobShop = new JobShop();

            for (int i = 0; i < populationSize; i++)
            {
                var schedule = jobShop.GenerateRandomSchedule(jobs);
                population.Add(schedule);
            }

            return population;
        }

        public int CalculateMakespan(List<JobOperation> schedule)
        {
            int makespan = 0;
            foreach (var operation in schedule)
            {
                if (operation.EndTime > makespan)
                {
                    makespan = operation.EndTime;
                }
            }
            return makespan;
        }

        public double FitnessFunction(List<JobOperation> schedule)
        {
            int makespan = CalculateMakespan(schedule);
            // The fitness value is the inverse of the makespan to ensure that shorter makespans are better
            return 1.0 / makespan;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            string base_directory = AppDomain.CurrentDomain.BaseDirectory;
            string relative_path = @"CSVs";
            string directory_path = Path.Combine(base_directory, relative_path);
            Files.directoryExists(directory_path);

            string[] files = Directory.GetFiles(directory_path, "*.csv");
            Files.filesExists(files);

            // Display the list of files
            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine(i + 1 + ": " + Path.GetFileName(files[i]));
            }

            // Ask the user to pick a file
            Console.WriteLine("Enter the number of the file you want to select:");
            int choice;

            // Validate input and ensure the choice is a valid number
            bool validChoice = int.TryParse(Console.ReadLine(), out choice);

            if (!validChoice || choice < 1 || choice > files.Length)
            {
                Console.WriteLine("Invalid choice. Please select a valid number.");
                return;
            }

            // Store the full path of the selected file in the 'schedule' variable
            string schedule = files[choice - 1];

            // Output the selected file's path
            Console.WriteLine("You selected: " + Path.GetFileName(schedule));

            // Process the selected CSV file
            var jobProcessor = new JobProcessor();
            var jobs = jobProcessor.ProcessCsv(schedule);

            // Display the jobs and their operations
            for (int jobId = 0; jobId < jobs.Count; jobId++)
            {
                var jobOperations = jobs[jobId];
                if (jobOperations.Count > 0)
                {
                    Console.WriteLine($"JobId: {jobId}");
                    foreach (var operation in jobOperations)
                    {
                        Console.WriteLine($"  OperationId: {operation.OperationId}, Subdivision: {operation.Subdivision}, SubdivisionId: {operation.SubdivisionId}, ProcessingTime: {operation.ProcessingTime}");
                    }
                }
            }

            // Run the Genetic Algorithm
            var ga = new GA(populationSize: 100, mutationRate: 0.01, maxGenerations: 100);
            var bestSchedule = ga.Run(jobs);

            string filePath = Path.Combine(base_directory, "output.csv");
            // Output the best schedule as a table
            SchedulePrinter.PrintScheduleAsCsv(bestSchedule, filePath);
        }

        public static class SchedulePrinter
        {
            public static void PrintScheduleAsCsv(List<JobOperation> schedule, string filePath)
            {
                // Determine the maximum end time
                int maxEndTime = 0;
                foreach (var operation in schedule)
                {
                    if (operation.EndTime > maxEndTime)
                    {
                        maxEndTime = operation.EndTime;
                    }
                }

                // Create a dictionary to map subdivision names to column indices
                var subdivisionNames = new Dictionary<string, int>();
                int nextColumnIndex = 0;
                foreach (var operation in schedule)
                {
                    if (!subdivisionNames.ContainsKey(operation.Subdivision))
                    {
                        subdivisionNames[operation.Subdivision] = nextColumnIndex++;
                    }
                }

                // Initialize a 2D array to represent the table
                var table = new string[maxEndTime + 1, subdivisionNames.Count];

                // Populate the table with JobId and OperationId
                foreach (var operation in schedule)
                {
                    int columnIndex = subdivisionNames[operation.Subdivision];
                    for (int time = operation.StartTime; time < operation.EndTime; time++)
                    {
                        table[time, columnIndex] = $"J{operation.JobId}O{operation.OperationId}";
                    }
                }

                // Open a StreamWriter to write to the CSV file
                using (var writer = new StreamWriter(filePath))
                {
                    // Write the table header
                    writer.Write("Time,");
                    foreach (var subdivisionName in subdivisionNames.Keys)
                    {
                        writer.Write($"{subdivisionName},");
                    }
                    writer.WriteLine();

                    // Write the table rows
                    for (int time = 0; time <= maxEndTime; time++)
                    {
                        writer.Write($"{time},");
                        for (int columnIndex = 0; columnIndex < subdivisionNames.Count; columnIndex++)
                        {
                            writer.Write($"{table[time, columnIndex] ?? string.Empty},");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }
    }
}