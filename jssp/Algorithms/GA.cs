using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics; // Add this at the top of the file
using Models;

namespace Algorithms
{
    public class GA
    {
        private const int PopulationSize = 50;
        private const int Generations = 50;
        private const int TournamentSize = 5;

        private readonly List<Job> _jobs;
        private readonly Random rand = new Random();

        public GA(List<Job> jobs)
        {
            _jobs = jobs;
        }

        public Schedule Solve()
        {
            Stopwatch stopwatch = Stopwatch.StartNew(); // Start the stopwatch

            List<Schedule> population = InitialisePopulation();

            Schedule best = population[0];

            for (int g = 0; g < Generations; g++)
            {
                // Parallelize the fitness evaluation
                Parallel.ForEach(population, schedule =>
                {
                    schedule.Fitness = Evaluate(schedule, _jobs.Select(j => new Job
                    {
                        JobId = j.JobId,
                        Operations = j.Operations.Select(o => new Operation
                        {
                            JobId = o.JobId,
                            OperationId = o.OperationId,
                            Subdivision = o.Subdivision,
                            ProcessingTime = o.ProcessingTime
                        }).ToList()
                    }).ToList());
                });

                population = population.OrderBy(c => c.Fitness).ToList();
                if (population[0].Fitness < best.Fitness)
                    best = population[0].Clone();

                List<Schedule> newPopulation = new List<Schedule> { best.Clone() }; // elitism

                Parallel.For(0, PopulationSize - 1, _ =>
                {

                    Schedule parent1 = Selection(population);
                    Schedule parent2 = Selection(population);

                    Schedule child = Crossover(parent1, parent2);
                    Mutate(child);

                    lock (newPopulation) // Ensure thread safety when adding to the shared list
                    {
                        newPopulation.Add(child);
                    }
                });

                population = newPopulation;
            }

            stopwatch.Stop(); // Stop the stopwatch
            Console.WriteLine($"Time taken to find the optimal solution: {stopwatch.ElapsedMilliseconds} ms"); // Output the elapsed time in milliseconds

            return best;
        }

        private List<Schedule> InitialisePopulation()
        {
            List<int> Pool = _jobs.SelectMany(j => Enumerable.Repeat(j.JobId, j.Operations.Count)).ToList();
            List<Schedule> population = new List<Schedule>();

            Parallel.For(0, PopulationSize, i =>
            {
                List<int> genes = Pool.OrderBy(x => rand.Next()).ToList();
                lock (population) // Ensure thread safety when adding to the shared list
                {
                    population.Add(new Schedule { JobOrder = genes });
                }
            });

            return population;
        }

        public static int Evaluate(Schedule schedule, List<Job> jobs)
        {
            Dictionary<int, Job> jobDict = jobs.ToDictionary(j => j.JobId);
            Dictionary<string, int> mAvailable = new();
            Dictionary<int, int> jobAvailable = new();

            foreach (Job job in jobs)
            {
                job.ResetOpIndex();
                jobAvailable[job.JobId] = 0;
            }

            int time = 0;

            foreach (int jobId in schedule.JobOrder)
            {
                Job job = jobDict[jobId];
                Operation operation = job.NextOperation();

                string machine = operation.Subdivision;

                int ready = Math.Max(
                    jobAvailable[jobId],
                    mAvailable.TryGetValue(machine, out int mTime) ? mTime : 0
                );

                operation.StartTime = ready;
                mAvailable[machine] = operation.EndTime;
                jobAvailable[jobId] = operation.EndTime;

                time = Math.Max(time, operation.EndTime);
            }

            return time;
        }

        private Schedule Selection(List<Schedule> population) => population.OrderBy(x => rand.Next()).Take(TournamentSize).OrderBy(c => c.Fitness).First();

        private Schedule Crossover(Schedule parent1, Schedule parent2)
        {
            int size = parent1.JobOrder.Count;
            List<int> child = new(new int[size]);
            Dictionary<int, int> used = new();

            int start = rand.Next(size / 2);
            int end = rand.Next(start + 1, size);

            for (int i = start; i < end; i++)
            {
                child[i] = parent1.JobOrder[i];
                used.TryAdd(child[i], 0);
                used[child[i]]++;
            }

            int parent2Index = 0;
            for (int i = 0; i < size; i++)
            {
                if (i >= start && i < end)
                    continue;
                while (used.TryGetValue(parent2.JobOrder[parent2Index], out int count) &&
                        count >= _jobs.First(j => j.JobId == parent2.JobOrder[parent2Index]).Operations.Count)
                    parent2Index++;

                int gene = parent2.JobOrder[parent2Index++];
                child[i] = gene;
                used.TryAdd(gene, 0);
                used[gene]++;
            }

            return new Schedule { JobOrder = child };
        }

        private void Mutate(Schedule schedule)
        {
            int start = rand.Next(schedule.JobOrder.Count);
            int end = rand.Next(start, schedule.JobOrder.Count);
            schedule.JobOrder.Reverse(start, end - start + 1);
        }

        private Schedule RouletteWheelSelection(List<Schedule> population)
        {
            // Calculate the total fitness of the population
            double totalFitness = population.Sum(individual => 1.0 / individual.Fitness); // Inverse fitness for minimization

            // Generate a random value between 0 and the total fitness
            double randomValue = rand.NextDouble() * totalFitness;

            // Iterate through the population to find the selected individual
            double cumulativeFitness = 0.0;
            foreach (var individual in population)
            {
                cumulativeFitness += 1.0 / individual.Fitness; // Inverse fitness for minimization
                if (cumulativeFitness >= randomValue)
                {
                    return individual;
                }
            }

            // Fallback (shouldn't happen if the logic is correct)
            return population.Last();
        }
    }
}