﻿using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using ZedGraph;

namespace ABC
{
    public delegate double FitnessFunction(double[] coords);
    class Swarm
    {
        private static Swarm instance;
        private static object syncRoot = new object();
        private Swarm()
        {
            Agents = new List<Agent>();
            Rnd = new Random();
            BestPatches = new List<Point>();
            ElitePatches = new List<Point>();
            Trail = new Dictionary<double[], double>();
        }
        public static Swarm GetInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new Swarm();
                    }
                }
            }
            return instance;
        }
        
        public void Initialize(FitnessFunction func, int dim = 2, int iterations = 100000, 
                        int scoutsCount = 10, int bestAgentsCount = 5, int eliteAgentsCount = 2,
                        int bestPatchesCount = 3, int elitePatchesCount = 2, double patchSize = 1)
        {
            PatchSize = patchSize;
            Size = scoutsCount + bestAgentsCount * bestPatchesCount + eliteAgentsCount * elitePatchesCount;
            Iterations = iterations;
            BestAgentsCount = bestAgentsCount;
            EliteAgentsCount = eliteAgentsCount;
            BestPatchesCount = bestPatchesCount;
            ElitePatchesCount = elitePatchesCount;
            FitFunction = FitnessFunctions.RosenbrocsSaddle;
            Dimension = dim;
            CurrentIteration = 0;
            Fitness = Double.MaxValue;
            Position = new Point(dim);
            PrevAverageFitness = double.MaxValue;

            // first agents initialization
            for (int i = 0; i < scoutsCount; i++) { Agents.Add(new Agent(Agent.RoleTypes.Scout)); }
            for (int i = 0; i < bestAgentsCount * bestPatchesCount; i++) { Agents.Add(new Agent(Agent.RoleTypes.Employed)); }
            for (int i = 0; i < eliteAgentsCount * elitePatchesCount; i++) { Agents.Add(new Agent(Agent.RoleTypes.Onlooker)); }


            AverageFitness = Agents.Where(a => a.Role != Agent.RoleTypes.Scout).Sum(a => a.Fitness) / (Size - BestAgentsCount);
        }
        public void Run()
        {
            PrevAverageFitness = AverageFitness;
            PrevFitness = Fitness;

            var BestPatches = Trail
                            .OrderBy(a => a.Value)
                            .Take(BestPatchesCount)
                            .Select(s => s.Key);



            var ElitePatches = Trail
                            .OrderBy(a => a.Value)
                            .Skip(BestPatchesCount)
                            .Take(ElitePatchesCount)
                            .Select(s => s.Key);


            int c1 = 0;
            int c2 = 0;
            foreach (var p in BestPatches)
            {
                var ag = Agents.Where(x => x.Role == Agent.RoleTypes.Employed)
                    .Skip(c1 * BestAgentsCount)
                    .Take(BestAgentsCount);
                foreach (var a in ag)
                {
                    a.Search(p);
                }
                c1++;
            }
            foreach (var p in ElitePatches)
            {
                var ag = Agents.Where(x => x.Role == Agent.RoleTypes.Onlooker)
                    .Skip(c2 * EliteAgentsCount)
                    .Take(EliteAgentsCount);
                foreach (var a in ag)
                {
                    a.Search(p);
                }
                c2++;
            }

            // Scouts search step
            foreach (var scout in Agents.Where(a => a.Role == Agent.RoleTypes.Scout))
            {
                scout.GlobalSearch();
            }

            AverageFitness = Agents.Where(a => a.Role != Agent.RoleTypes.Scout ) .Sum(a => a.Fitness) / (Size - BestAgentsCount);
            CurrentIteration++;
            
            GenerationsCounter = (Math.Abs(PrevAverageFitness - AverageFitness) < 0.0001) ? GenerationsCounter + 1 : 0;
            
            PatchChangeCounter = (PrevFitness == Fitness) ? PatchChangeCounter + 1 : 0;

            if (PatchChangeCounter>50)
            {
                PatchSize = 0.95 * PatchSize;
                PatchChangeCounter = 0;
            }
        }

        public double EuclidDistance()
        {
            double sum = 0;
            foreach (var coord in Position.Coords)
            {
                sum += (coord-1) * (coord - 1);
            }
            return Math.Sqrt(sum);
        }

        public int Dimension;
        public Dictionary<double[], double> Trail { get; set; }
        public FitnessFunction FitFunction { get; set; }
        public int Iterations { get; set; }
        public int CurrentIteration { get; set; }
        public int PatchChangeCounter { get; set; }
        public int GenerationsCounter { get; set; }
        public int FittnessCallsCounter { get; set; }
        public Point Position { get; set; }
        public double Fitness { get; set; }
        public double PrevAverageFitness { get; set; }
        public double PrevFitness { get; set; }
        public double AverageFitness { get; set; }
        public int Size { get; set; }
        public double PatchSize { get; set; }
        public List<Point> BestPatches { get; set; }
        public int BestPatchesCount { get; set; }
        public List<Point> ElitePatches { get; set; }
        public int ElitePatchesCount { get; set; }
        public List<Agent> Agents { get; }
        public int BestAgentsCount { get; set; }
        public int EliteAgentsCount { get; set; }
        public Random Rnd { get; }
    }
}
