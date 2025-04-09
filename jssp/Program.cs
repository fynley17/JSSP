using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace jssp
{
    public class JobShop
    {
        public List<JobOperation> GenerateRandomSchedule(List<List<JobOperation>> jobs)
        {
            var schedule = new List<JobOperation>();
            var jobOperationIndices = new Dictionary<int, int>(); // Tracks the next operation index for each job
            var machineAvailability = new Dictionary<int, int>(); // Tracks the next available time for each machine
            var rand = new Random(Guid.NewGuid().GetHashCode()); // Use a unique seed for randomness

            // Initialize job operation indices
            for (int i = 0; i < jobs.Count; i++)
            {
                jobOperationIndices[i] = 0;
            }

            // While there are still operations to schedule
            while (schedule.Count < jobs.Sum(job => job.Count))
            {
                // Select a random job that still has unscheduled operations
                var availableJobs = jobOperationIndices
                    .Where(kvp => kvp.Value < jobs[kvp.Key].Count)
                    .Select(kvp => kvp.Key)
                    .OrderBy(_ => rand.Next()) // Shuffle the available jobs to introduce randomness
                    .ToList();

                if (availableJobs.Count == 0)
                {
                    throw new InvalidOperationException("No available jobs to schedule. Check job data integrity.");
                }

                // Randomly select a job from the shuffled available jobs
                int randomJobIndex = availableJobs.First();
                var operation = jobs[randomJobIndex][jobOperationIndices[randomJobIndex]];

                // Determine the earliest start time for the operation
                int previousOperationEndTime = 0;
                if (jobOperationIndices[randomJobIndex] > 0)
                {
                    previousOperationEndTime = schedule
                        .First(op => op.JobId == randomJobIndex && op.OperationId == operation.OperationId - 1)
                        .EndTime;
                }

                int machineAvailableTime = machineAvailability.ContainsKey(operation.SubdivisionId)
                    ? machineAvailability[operation.SubdivisionId]
                    : 0;

                operation.StartTime = Math.Max(previousOperationEndTime, machineAvailableTime);
                operation.EndTime = operation.StartTime + operation.ProcessingTime;

                // Update machine availability and job operation index
                machineAvailability[operation.SubdivisionId] = operation.EndTime;
                jobOperationIndices[randomJobIndex]++;

                // Add the operation to the schedule
                schedule.Add(operation);
            }

            // Verify that all operations are included
            int totalOperations = jobs.Sum(job => job.Count);
            if (schedule.Count != totalOperations)
            {
                throw new InvalidOperationException($"Schedule generation failed. Expected {totalOperations} operations, but got {schedule.Count}.");
            }

            return schedule;
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
            var makespan = new List<double>();
            foreach (var schedule in population)
            {
                makespan.Add(FitnessFunction(schedule));
            }

            // Genetic Algorithm main loop
            for (int generation = 0; generation < maxGenerations; generation++)
            {
                // Selection: Select parents based on fitness (lower makespan is better)
                var selectedParents = SelectParents(population, makespan);

                // Crossover: Create offspring by combining parents
                var offspring = Crossover(selectedParents);

                // Mutation: Introduce random changes to offspring
                Mutate(offspring);

                // Evaluate fitness of new population
                population = offspring;
                makespan.Clear();
                foreach (var schedule in population)
                {
                    makespan.Add(FitnessFunction(schedule));
                }

                Console.WriteLine($"Generation {generation + 1}: Best Makespan = {makespan.Min()}");
            }

            // Return the best schedule
            int bestIndex = makespan.IndexOf(makespan.Min());
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

            for (int i = 0; i < population.Count; i++)
            {
                Console.WriteLine($"Schedule {i + 1}:");
                foreach (var operation in population[i])
                {
                    Console.WriteLine($"  JobId: {operation.JobId}, OperationId: {operation.OperationId}, Subdivision: {operation.Subdivision}, StartTime: {operation.StartTime}, EndTime: {operation.EndTime}");
                }
            }

            return population;
        }

        public double FitnessFunction(List<JobOperation> schedule)
        {
            int makespan = 0;
            foreach (var operation in schedule)
            {
                if (operation.EndTime > makespan)
                {
                    makespan = operation.EndTime;
                }
            }
            Console.WriteLine($"makespan {makespan}");
            return makespan;
        }

        private List<List<JobOperation>> SelectParents(List<List<JobOperation>> population, List<double> makespan)
        {
            var selectedParents = new List<List<JobOperation>>();
            var rand = new Random();

            for (int i = 0; i < populationSize; i++)
            {
                // Perform a tournament selection
                int parent1Index = rand.Next(populationSize);
                int parent2Index = rand.Next(populationSize);

                // Select the parent with the better fitness (lower makespan)
                if (makespan[parent1Index] < makespan[parent2Index])
                {
                    selectedParents.Add(population[parent1Index]);
                }
                else
                {
                    selectedParents.Add(population[parent2Index]);
                }
            }

            return selectedParents;
        }

        private List<List<JobOperation>> Crossover(List<List<JobOperation>> parents)
        {
            var offspring = new List<List<JobOperation>>();
            var rand = new Random();

            for (int i = 0; i < parents.Count; i += 2)
            {
                if (i + 1 >= parents.Count)
                {
                    offspring.Add(parents[i]);
                    continue;
                }

                var parent1 = parents[i];
                var parent2 = parents[i + 1];

                // Perform one-point crossover
                int crossoverPoint = rand.Next(1, parent1.Count - 1);
                var child1 = parent1.Take(crossoverPoint).Concat(parent2.Skip(crossoverPoint)).ToList();
                var child2 = parent2.Take(crossoverPoint).Concat(parent1.Skip(crossoverPoint)).ToList();

                offspring.Add(child1);
                offspring.Add(child2);
            }

            return offspring;
        }

        private void Mutate(List<List<JobOperation>> population)
        {
            var rand = new Random();

            foreach (var schedule in population)
            {
                if (rand.NextDouble() < mutationRate)
                {
                    // Swap two random operations in the schedule
                    int index1 = rand.Next(schedule.Count);
                    int index2 = rand.Next(schedule.Count);

                    var temp = schedule[index1];
                    schedule[index1] = schedule[index2];
                    schedule[index2] = temp;
                }
            }
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