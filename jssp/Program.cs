using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace jssp
{
    public class JobShop 
    {
        private  Random rand = new();
        public static bool EnsureOrder()
        {

            return false;
        }
    }
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

    public class JobOperation
    {
        public int OperationId { get; set; }
        public string Subdivision { get; set; }
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
            foreach (var job in jobs)
            {
                Console.WriteLine($"JobId: {job.Key}");
                foreach (var operation in job.Value)
                {
                    Console.WriteLine($"  OperationId: {operation.OperationId}, Subdivision: {operation.Subdivision}, ProcessingTime: {operation.ProcessingTime}");
                }
            }
        }
    }
}