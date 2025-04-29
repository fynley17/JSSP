using System;
using System.Collections.Generic;
using System.IO;
using jssp.Models;
using Algorithms;

namespace jssp
{
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
            var Jobs = jobProcessor.ProcessCsv(schedule);

            // Solve the scheduling problem using the Genetic Algorithm
            var ga = new GA(Jobs);
            var bestSchedule = ga.Solve();

            // Output the fitness of the best schedule
            Console.WriteLine($"Fitness of the best schedule: {GA.Evaluate(bestSchedule, Jobs)}");

            // Output the best schedule as a table
            string outputFilePath = Path.Combine(base_directory, "output.csv");
            SchedulePrinter.PrintScheduleAsCsv(Jobs, bestSchedule, outputFilePath);

            Console.WriteLine($"Best schedule saved to {outputFilePath}");
        }

        public static class SchedulePrinter
        {
            public static void PrintScheduleAsCsv(List<Job> jobs, Schedule schedule, string filePath)
            {
                // Determine the maximum end time
                int maxEndTime = 0;
                foreach (var job in jobs)
                {
                    foreach (var operation in job.Operations)
                    {
                        if (operation.EndTime > maxEndTime)
                        {
                            maxEndTime = operation.EndTime;
                        }
                    }
                }

                // Create a dictionary to map subdivision names to column indices
                var subdivisionNames = new Dictionary<string, int>();
                int nextColumnIndex = 0;
                foreach (var job in jobs)
                {
                    foreach (var operation in job.Operations)
                    {
                        if (!subdivisionNames.ContainsKey(operation.Subdivision))
                        {
                            subdivisionNames[operation.Subdivision] = nextColumnIndex++;
                        }
                    }
                }

                // Initialize a 2D array to represent the table
                var table = new string[maxEndTime + 1, subdivisionNames.Count];

                // Populate the table with JobId and OperationId
                foreach (var job in jobs)
                {
                    foreach (var operation in job.Operations)
                    {
                        int columnIndex = subdivisionNames[operation.Subdivision];
                        for (int time = operation.StartTime; time < operation.EndTime; time++)
                        {
                            table[time, columnIndex] = $"J{operation.JobId}O{operation.OperationId}";
                        }
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

    class Files
    {
        public static void directoryExists(string directory_path)
        {
            if (!Directory.Exists(directory_path))
            {
                Console.WriteLine("Directory doesn't exist");
                Environment.Exit(1);
            }
        }

        public static void filesExists(string[] files)
        {
            if (files.Length == 0)
            {
                Console.WriteLine("No .csv files found in the directory.");
                Environment.Exit(1);
            }
        }
    }

    public class JobProcessor
    {
        public List<Job> ProcessCsv(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            List<Operation> operations = lines.Skip(1) // Skip header
                .Select(static line => line.Split(','))
                .Select(static values => new Operation
                {
                    JobId = int.Parse(values[0]),
                    OperationId = int.Parse(values[1]),
                    Subdivision = values[2],
                    ProcessingTime = int.Parse(values[3])
                })
                .ToList();

            // Group operations by JobId
            IEnumerable<Job> jobGroups = operations
                .GroupBy(op => op.JobId)
                .Select(static group => new Job
                {
                    JobId = group.Key,
                    Operations = group
                        .OrderBy(static op => op.OperationId) // Ensure operations are in order
                        .ToList()
                });

            return jobGroups.ToList();
        }
    }
}