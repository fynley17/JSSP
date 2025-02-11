using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace jssp
{
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

    public class CsvConverter
    {
        public void ConvertToCsv(Dictionary<int, List<JobOperation>> jobs, string outputFilePath)
        {
            var subdivisions = jobs.SelectMany(j => j.Value.Select(o => o.Subdivision)).Distinct().ToList();
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

                foreach (var job in jobs)
                {
                    foreach (var operation in job.Value)
                    {
                        writer.Write($"{FormatTime(currentTime)}");

                        foreach (var subdivision in subdivisions)
                        {
                            if (operation.Subdivision == subdivision)
                            {
                                writer.Write($",job{job.Key}- op{operation.OperationId}");
                                currentTime += operation.ProcessingTime * 60; // Increment time by processing time in minutes
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
        }

        private string FormatTime(int minutes)
        {
            int hours = minutes / 60;
            int mins = minutes % 60;
            return $"{hours:D2}:{mins:D2}";
        }
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

            // Convert the processed jobs to a new CSV file
            var csvConverter = new CsvConverter();
            string outputFilePath = Path.Combine(directory_path, "output.csv");
            csvConverter.ConvertToCsv(jobs, outputFilePath);

            Console.WriteLine("CSV file has been created at: " + outputFilePath);
        }
    }
}
