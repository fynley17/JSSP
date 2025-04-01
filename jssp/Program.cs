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
        public static bool EnsureOrder()
        {
            // Ensure the order of the operations


            return false;
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
                    int operationId = int.Parse(values[1]);
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
                        OperationId = operationId,
                        Subdivision = subdivision,
                        SubdivisionId = subdivisionId,
                        ProcessingTime = processingTime
                    };

                    // Ensure the list is large enough to hold the jobId index
                    while (jobs.Count <= jobId)
                    {
                        jobs.Add(new List<JobOperation>());
                    }

                    jobs[jobId].Add(jobOperation);
                }
            }

            return jobs;
        }
    }

    public class JobOperation
    {
        public int OperationId { get; set; }
        public string Subdivision { get; set; }
        public int SubdivisionId { get; set; }
        public int ProcessingTime { get; set; }
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
            var population = InitializePopulation(jobs);

            for (int generation = 0; generation < maxGenerations; generation++)
            {
                // Evaluate fitness
                var fitnessScores = EvaluateFitness(population);

                // Selection
                var matingPool = Selection(population, fitnessScores);

                // Crossover
                var newPopulation = Crossover(matingPool);

                // Mutation
                Mutate(newPopulation);

                // Replacement
                population = newPopulation;
            }

            // Return the best schedule
            return GetBestSchedule(population);
        }

        private List<List<JobOperation>> InitializePopulation(List<List<JobOperation>> jobs)
        {
            // Initialize the population with random schedules
            // ...
        }

        private List<double> EvaluateFitness(List<List<JobOperation>> population)
        {
            // Evaluate the fitness of each schedule in the population
            // ...
        }

        private List<List<JobOperation>> Selection(List<List<JobOperation>> population, List<double> fitnessScores)
        {
            // Select schedules based on their fitness scores
            // ...
        }

        private List<List<JobOperation>> Crossover(List<List<JobOperation>> matingPool)
        {
            // Perform crossover to create new schedules
            // ...
        }

        private void Mutate(List<List<JobOperation>> population)
        {
            // Perform mutation on the population
            // ...
        }

        private List<JobOperation> GetBestSchedule(List<List<JobOperation>> population)
        {
            // Return the best schedule from the population
            // ...
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
        }
    }
}