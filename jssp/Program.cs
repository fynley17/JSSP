using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using jssp.Models;
using Algorithms;
using System.Diagnostics;

namespace jssp
{
    internal class Program
    {
        private static Dictionary<string, string> filePathMap;

        static void Main(string[] args)
        {
            Application.Init();

            var top = Application.Top;
            var mainWindow = new Window("Job Scheduling Problem") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

            // File selection list
            var fileListView = new ListView() { X = 1, Y = 1, Width = Dim.Fill() - 2, Height = Dim.Fill() - 20 };
            var files = GetCsvFiles();
            fileListView.SetSource(files);


            // Buttons
            var processButton = new Button("Process File") { X = 1, Y = Pos.Bottom(fileListView) + 1 };
            var exportButton = new Button("Export to CSV") { X = Pos.Right(processButton) + 2, Y = Pos.Top(processButton) };
            var openButton = new Button("Open CSV") { X = Pos.Right(exportButton) + 2, Y = Pos.Top(processButton) };
            var exitButton = new Button("Exit") { X = Pos.Right(openButton) + 2, Y = Pos.Top(processButton) };

            // Status label
            var statusLabel = new Label("Select a file and click 'Process File'") { X = 1, Y = Pos.Bottom(processButton), Width = Dim.Fill() };



            // Add components to the main window
            mainWindow.Add(statusLabel, fileListView, processButton, exportButton, openButton, exitButton);
            top.Add(mainWindow);

            string selectedFile = null;
            List<Job> jobs = null;
            Schedule bestSchedule = null;
            string outputFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.csv");

            // Process file button click
            processButton.Clicked += () =>
            {
                if (fileListView.SelectedItem < 0 || fileListView.SelectedItem >= files.Count)
                {
                    statusLabel.Text = "Please select a valid file.";
                    return;
                }

                selectedFile = filePathMap[files[fileListView.SelectedItem]];
                statusLabel.Text = $"Processing file: {Path.GetFileName(selectedFile)}";

                try
                {
                    var jobProcessor = new JobProcessor();
                    jobs = jobProcessor.ProcessCsv(selectedFile);

                    if (jobs.Count == 0)
                    {
                        statusLabel.Text = "No jobs found in the selected file.";
                        return;
                    }

                    var ga = new GA(jobs);
                    Stopwatch stopwatch = Stopwatch.StartNew(); // Start the stopwatch
                    bestSchedule = ga.Solve();
                    stopwatch.Stop(); // Stop the stopwatch

                    // Calculate the average processing time per job
                    double totalProcessingTime = jobs.Sum(job => job.Operations.Sum(op => op.ProcessingTime));
                    double averageProcessingTime = totalProcessingTime / jobs.Count;

                    // Calculate the number of operations per subdivision
                    var operationsPerSubdivision = jobs
                        .SelectMany(job => job.Operations)
                        .GroupBy(op => op.Subdivision)
                        .ToDictionary(group => group.Key, group => group.Count());

                    // Format the operations per subdivision as a table
                    string tableHeader = "Subdivision | Operations\n" +
                                         "------------|-----------\n";
                    string tableRows = string.Join("\n", operationsPerSubdivision.Select(kvp => $"{kvp.Key.PadRight(12)} | {kvp.Value}"));
                    string operationsTable = tableHeader + tableRows;

                    statusLabel.Text = $"File processed in {stopwatch.ElapsedMilliseconds}ms.\n" +
                                       $"Best fitness: {GA.Evaluate(bestSchedule, jobs)}.\n" +
                                       $"Total jobs completed: {jobs.Count}.\n" +
                                       $"Average processing time per job: {averageProcessingTime:F2} hrs.\n\n" +
                                       $"Operations per subdivision:\n{operationsTable}";
                    Application.Refresh(); // Refresh the UI to show the updated status
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Error processing file: {ex.Message}";
                }
            };

            // Export to CSV button click
            exportButton.Clicked += () =>
            {
                if (bestSchedule == null || jobs == null)
                {
                    statusLabel.Text = "No schedule to export. Process a file first.";
                    return;
                }

                try
                {
                    SchedulePrinter.PrintScheduleAsCsv(jobs, bestSchedule, outputFilePath);
                    statusLabel.Text = $"Schedule exported to {outputFilePath}";
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Error exporting file: {ex.Message}";
                }
            };

            // Open CSV button click
            openButton.Clicked += () =>
            {
                if (!File.Exists(outputFilePath))
                {
                    statusLabel.Text = "No exported file found. Export a schedule first.";
                    return;
                }

                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", outputFilePath);
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Error opening file: {ex.Message}";
                }
            };

            // Exit button click event
            exitButton.Clicked += () =>
            {
                Application.RequestStop(); // Stops the application
            };

            Application.Run();
        }

        private static List<string> GetCsvFiles()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string relativePath = @"CSVs";
            string directoryPath = Path.Combine(baseDirectory, relativePath);

            Files.directoryExists(directoryPath);

            string[] files = Directory.GetFiles(directoryPath, "*.csv");
            Files.filesExists(files);

            // Create a mapping of filenames to full paths
            filePathMap = files.ToDictionary(Path.GetFileName, fullPath => fullPath);

            // Return only the filenames
            return filePathMap.Keys.ToList();
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