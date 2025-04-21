using System;
using System.Collections.Generic;
using System.Linq;
using Models;

namespace Algorithms
{
    public class GA
    {
        private const int PopulationSize = 100;
        private const int Generations = 1000;
        private const int TournamentSize = 5;

        private readonly List<Job> _jobs;
        private readonly Random _random = new Random();

        public GA(List<Job> jobs)
        {
            _jobs = jobs;
        }

        public Schedule Solve()
        {
            List<Schedule> population = InitialisePopulation();

            Schedule best = population[0];

            for (int g = 0; g < Generations; g++)
            {
                foreach (Schedule schedule in population)
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

                population = population.OrderBy(c => c.Fitness).ToList();
                if (population[0].Fitness < best.Fitness)
                    best = population[0].Clone();

                List<Schedule> newPopulation = new List<Schedule> { best.Clone() }; // elitism

                while (newPopulation.Count < PopulationSize)
                {
                    Schedule parent1 = Selection(population);
                    Schedule parent2 = Selection(population);

                    Schedule child = Crossover(parent1, parent2);
                    Mutate(child);

                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

            return best;
        }

        private List<Schedule> InitialisePopulation()
        {
            List<int> Pool = _jobs.SelectMany(j => Enumerable.Repeat(j.JobId, j.Operations.Count)).ToList();
            List<Schedule> population = new List<Schedule>();

            for (int i = 0; i < PopulationSize; i++)
            {
                List<int> genes = Pool.OrderBy(x => _random.Next()).ToList();
                population.Add(new Schedule { JobOrder = genes });
            }

            return population;
        }

        public static int Evaluate(Schedule schedule, List<Job> jobs)
        {
            Dictionary<string, int> mAvalible = new();
            Dictionary<int, int> JopAvalible = new();
            foreach (Job job in jobs)
            {
                job.Reset();
                JopAvalible[job.JobId] = 0;
            }

            int time = 0;

            foreach (int JobId in schedule.JobOrder)
            {
                Job job = jobs.First(j => j.JobId == JobId);
                Operation operation = job.NextOperation();

                string machine = operation.Subdivision;

                int ready = Math.Max(
                    JopAvalible[JobId],
                    mAvalible.TryGetValue(machine, out int mTime) ? mTime : 0
                );

                operation.StartTime = ready;
                mAvalible[machine] = operation.EndTime;
                JopAvalible[JobId] = operation.EndTime;

                time = Math.Max(time, operation.EndTime);
            }

            return time;
        }

        private Schedule Selection(List<Schedule> population) => population.OrderBy(x => _random.Next()).Take(TournamentSize).OrderBy(c => c.Fitness).First();

        private Schedule Crossover(Schedule parent1, Schedule parent2)
        {
            int size = parent1.JobOrder.Count;
            List<int> child = new(new int[size]);
            Dictionary<int, int> used = new();

            int start = _random.Next(size / 2);
            int end = _random.Next(start + 1, size);

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
            int a = _random.Next(schedule.JobOrder.Count);
            int b = _random.Next(schedule.JobOrder.Count);
            (schedule.JobOrder[a], schedule.JobOrder[b]) = (schedule.JobOrder[b], schedule.JobOrder[a]);
        }
    }
}