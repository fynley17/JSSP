using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace jssp
{
    // JobOperation.cs
    public class JobOperation
    {
        public int JobId { get; set; }  // Add JobId property here
        public int OperationId { get; set; }
        public string Subdivision { get; set; }
        public int ProcessingTime { get; set; }
    }

    // Genetic.cs
    public class Genetic
    {
        private Random rand = new Random();
        private int populationSize = 100;
        private int generations = 500;
        private double mutationRate = 0.5;
        private double crossoverRate = 1;

        public class Chromosome
        {
            public List<JobOperation> Operations { get; set; } // This would be a list of job operations in order
            public int Makespan { get; set; }

            public Chromosome(List<JobOperation> operations)
            {
                Operations = operations;
                Makespan = CalculateMakespan(operations);
            }

            public int CalculateMakespan(List<JobOperation> operations)
            {
                // Dictionary to track the last time each machine becomes available.
                // Key: Subdivision (machine type), Value: Last available time in hours.
                Dictionary<string, double> machineAvailability = new Dictionary<string, double>();

                // Sort operations by job ID and then by operation ID within each job
                var sortedOperations = operations.OrderBy(o => o.JobId).ThenBy(o => o.OperationId).ToList();

                double currentTime = 9; // Start at 9 AM in hours (since we are assuming workday starts at 9 AM)
                double makespan = 0;

                // Process each operation in the sorted list
                foreach (var operation in sortedOperations)
                {
                    string machineType = operation.Subdivision;
                    double processingTime = operation.ProcessingTime; // In hours

                    // If the machine is not in the dictionary, initialize it with an available time of 9 AM (start of the day)
                    if (!machineAvailability.ContainsKey(machineType))
                    {
                        machineAvailability[machineType] = 9; // Starting time (9 AM in hours)
                    }

                    // Get the earliest available time for the machine
                    double machineAvailableTime = machineAvailability[machineType];

                    // The operation can start when both the machine is available and the job operations respect the order
                    double startTime = Math.Max(currentTime, machineAvailableTime);

                    // Calculate the end time for this operation
                    double endTime = startTime + processingTime;

                    // Update the machine's availability time
                    machineAvailability[machineType] = endTime;

                    // Update the makespan if this operation finishes later than the current makespan
                    makespan = Math.Max(makespan, endTime);

                    // Update the current time for the next operation (this will be when the job is processed next)
                    currentTime = startTime;
                }

                return (int)makespan; // The makespan is the time when the last operation finishes, rounded to the nearest hour
            }
        }

        // Initialize population
        public List<Chromosome> InitializePopulation(Dictionary<int, List<JobOperation>> jobs)
        {
            List<Chromosome> population = new List<Chromosome>();

            for (int i = 0; i < populationSize; i++)
            {
                var operations = GenerateRandomSchedule(jobs);
                population.Add(new Chromosome(operations));
            }

            return population;
        }

        private List<JobOperation> GenerateRandomSchedule(Dictionary<int, List<JobOperation>> jobs)
        {
            var allOperations = jobs.SelectMany(j => j.Value).ToList();
            return allOperations.OrderBy(x => rand.Next()).ToList(); // Shuffle the operations randomly
        }

        public Chromosome SelectParent(List<Chromosome> population)
        {
            int tournamentSize = 5;
            var tournament = population.OrderBy(x => rand.Next()).Take(tournamentSize).ToList();
            return tournament.OrderBy(x => x.Makespan).First();
        }

        public Chromosome Crossover(Chromosome parent1, Chromosome parent2)
        {
            if (rand.NextDouble() > crossoverRate)
            {
                return parent1;
            }

            var childOperations = new List<JobOperation>(parent1.Operations);
            var crossoverPoint = rand.Next(0, parent1.Operations.Count);
            childOperations.RemoveRange(crossoverPoint, childOperations.Count - crossoverPoint);
            childOperations.AddRange(parent2.Operations.Skip(crossoverPoint));

            return new Chromosome(childOperations);
        }

        public void Mutate(Chromosome chromosome)
        {
            if (rand.NextDouble() < mutationRate)
            {
                int index1 = rand.Next(0, chromosome.Operations.Count);
                int index2 = rand.Next(0, chromosome.Operations.Count);

                var temp = chromosome.Operations[index1];
                chromosome.Operations[index1] = chromosome.Operations[index2];
                chromosome.Operations[index2] = temp;

                chromosome.Makespan = chromosome.CalculateMakespan(chromosome.Operations);
            }
        }

        public Chromosome Run(Dictionary<int, List<JobOperation>> jobs)
        {
            List<Chromosome> population = InitializePopulation(jobs);

            for (int generation = 0; generation < generations; generation++)
            {
                List<Chromosome> newPopulation = new List<Chromosome>();

                newPopulation.Add(population.OrderBy(x => x.Makespan).First());

                while (newPopulation.Count < populationSize)
                {
                    var parent1 = SelectParent(population);
                    var parent2 = SelectParent(population);

                    var child = Crossover(parent1, parent2);
                    Mutate(child);

                    newPopulation.Add(child);
                }

                population = newPopulation;

                Console.WriteLine($"Generation {generation}, Best Makespan: {population.OrderBy(x => x.Makespan).First().Makespan}");
            }

            return population.OrderBy(x => x.Makespan).First();
        }
    }

    // JobProcessor.cs
    public class JobProcessor
    {
        public Dictionary<int, List<JobOperation>> ProcessCsv(string filePath)
        {
            var jobs = new Dictionary<int, List<JobOperation>>();

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

                    var jobOperation = new JobOperation
                    {
                        JobId = jobId,  // Set JobId here
                        OperationId = operationId,
                        Subdivision = subdivision,
                        ProcessingTime = processingTime
                    };

                    if (!jobs.ContainsKey(jobId))
                    {
                        jobs[jobId] = new List<JobOperation>();
                    }

                    jobs[jobId].Add(jobOperation);
                }
            }

            return jobs;
        }
    }

    // CsvConverter.cs
    public class CsvConverter
    {
        public void ConvertToCsv(List<JobOperation> operations, string outputFilePath)
        {
            var subdivisions = operations.Select(o => o.Subdivision).Distinct().ToList();
            subdivisions.Sort();

            using (var writer = new StreamWriter(outputFilePath))
            {
                // Write the header
                writer.Write("Time");
                foreach (var subdivision in subdivisions)
                {
                    writer.Write($",{subdivision}");
                }
                writer.WriteLine();

                // Write the job operations
                int currentTime = 9 * 60; // Start at 9 AM in minutes

                foreach (var operation in operations.OrderBy(o => o.JobId).ThenBy(o => o.OperationId))
                {
                    writer.Write($"{FormatTime(currentTime)}");

                    foreach (var subdivision in subdivisions)
                    {
                        if (operation.Subdivision == subdivision)
                        {
                            writer.Write($",job{operation.JobId}-op{operation.OperationId}");
                            currentTime += operation.ProcessingTime * 60; // Increment time by processing time in minutes

                            // Reset time to 9 AM if it reaches 5 PM
                            if (currentTime >= 17 * 60)
                            {
                                currentTime = 9 * 60;
                            }
                        }
                        else
                        {
                            writer.Write(",");
                        }
                    }
                    writer.WriteLine();
                }
            }
        }

        private string FormatTime(int minutes)
        {
            int hours = minutes / 60;
            int mins = minutes % 60;
            return $"{hours:D2}:{mins:D2}";
        }
    }

    // Files.cs
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

    // Program.cs
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

            Console.WriteLine("Running Genetic Algorithm to optimize makespan...");

            var geneticAlgorithm = new Genetic();
            var bestSolution = geneticAlgorithm.Run(jobs);

            Console.WriteLine("Best Optimized Schedule (Best Makespan):");
            foreach (var operation in bestSolution.Operations)
            {
                Console.WriteLine($"Job ID: {operation.JobId}, Operation ID: {operation.OperationId}, Subdivision: {operation.Subdivision}, Processing Time: {operation.ProcessingTime} hours");
            }

            Console.WriteLine("Optimized Makespan: " + bestSolution.Makespan);

            // Convert the processed jobs to a new CSV file
            var csvConverter = new CsvConverter();

            // Here, you should pass the bestSolution's operations to the CSV converter
            string outputFilePath = Path.Combine(directory_path, "output.csv");
            csvConverter.ConvertToCsv(bestSolution.Operations, outputFilePath);

            Console.WriteLine("CSV file has been created at: " + outputFilePath);
        }
    }
}
