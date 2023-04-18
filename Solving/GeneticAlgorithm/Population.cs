using System;
using System.Diagnostics;

namespace FFXIVCraftingLib.Solving.GeneticAlgorithm
{
    public sealed class Population
    {
        public int Index { get; private set; }
        private CraftingEngine Engine { get; set; }


        public int MaxSize { get; private set; }
        public int DefaultChromosomeSize { get; private set; }
        public int ChromosomeSize { get; private set; }
        public byte[] AvailableValues { get; private set; }

        public Chromosome[] Chromosomes { get; private set; }

        public int CurrentGeneration { get; private set; }

        public int NonProgressiveGenerations { get; private set; }
        public UInt128 NonProgressiveScore { get; private set; }

        public Chromosome Best { get; private set; }

        public Chromosome PendingBest { get; set; }

        public bool LeaveInitialValues { get; set; }
        public byte[] InitialValues { get; set; }

        public event Action<Population> GenerationRan = delegate { };

        public Population(int index, CraftingEngine sim, int maxSize, int chromosomeSize, byte[] availableValues, byte[] initialValues = null)
        {
            Index = index;
            Engine = sim.Clone(false);
            MaxSize = maxSize;
            Chromosomes = new Chromosome[MaxSize];
            DefaultChromosomeSize = chromosomeSize;
            ChromosomeSize = chromosomeSize;
            AvailableValues = availableValues;
            LeaveInitialValues = false;
            for (int i = 0; i < MaxSize; i++)
            {
                Chromosomes[i] = new Chromosome(Engine, availableValues, chromosomeSize);
            }

            if (initialValues != null)
            {
                initialValues.CopyTo(Chromosomes[0].Values, 0);

            }

            Array.Sort(Chromosomes);
            Best = Chromosomes[0];
            for (int i = 1; i < Chromosomes.Length; i++)
            {
                if (Chromosomes[i - 1].Fitness < Chromosomes[i].Fitness)
                    Debugger.Break();
            }

            CurrentGeneration = 0;
        }

        public void ChangeAvailableValues(byte[] newValues)
        {
            AvailableValues = newValues;
            for (int i = 0; i < MaxSize; i++)
                Chromosomes[i].AvailableValues = newValues;
        }

        public void AddChromosomes(byte[][] values, bool sort)
        {
            for (int i = 0; i < values.Length; i++)
                AddChromosome(values[i], sort);
        }

        public void AddChromosome(byte[] values, bool sort)
        {
            byte[] newValues = new byte[ChromosomeSize];

            values.CopyTo(newValues, 0);



            if (LeaveInitialValues)
            {
                InitialValues.CopyTo(newValues, 0);
            }

            AddChromosome(new Chromosome(Engine, AvailableValues, ChromosomeSize, newValues), sort);
        }

        public void AddChromosome(Chromosome c, bool sort)
        {
            if (Chromosomes[Chromosomes.Length - 1].Fitness < c.Fitness && !Contains(c))
            {
                Chromosomes[Chromosomes.Length - 1] = c;
                if (sort)
                    Array.Sort(Chromosomes);
            }
        }

        public void RunOnce()
        {
            int len = Chromosomes.Length;
            int crossoverParent1 = MaxSize / 4;
            int crossoverParent2 = Math.Min(3, len);
            for (int i = 0; i < crossoverParent1; i++)
            {
                int maxParent2 = i + crossoverParent2;
                for (int j = i + 1; j < maxParent2; j++)
                {
                    Crossover(Chromosomes[i], Chromosomes[j], len - i - 1);
                }
            }

            Array.Sort(Chromosomes);
            Best = Chromosomes[0];


            int crossoverFromMax = Math.Max(10, MaxSize / 8);
            for (int i = crossoverFromMax; i < MaxSize; i++)
            {
                Mutation(i);
            }

            Array.Sort(Chromosomes);
            Best = Chromosomes[0];
            try
            {
                if (PendingBest != null)
                {
                    if (LeaveInitialValues)
                    {
                        InitialValues.CopyTo(PendingBest.Values, 0);
                        PendingBest.Fitness = PendingBest.Evaluate();
                    }

                    if (!Contains(PendingBest) && Chromosomes[Chromosomes.Length - 1].Fitness < PendingBest.Fitness)
                    {
                        Chromosomes[Chromosomes.Length - 1] = PendingBest.Clone();
                        Array.Sort(Chromosomes);
                        Best = Chromosomes[0];
                    }
                    PendingBest = null;
                }
            }
            catch (Exception e)
            {
                Debugger.Break();
            }

            CurrentGeneration++;

            if (Best.Fitness <= NonProgressiveScore)
                NonProgressiveGenerations++;
            else
            {
                NonProgressiveScore = Best.Fitness;
                NonProgressiveGenerations = 0;
            }


            if (NonProgressiveGenerations >= int.MaxValue)
            {
                int changeFrom = MaxSize / 10;
                for (int i = changeFrom; i < MaxSize; i++)
                {
                    Chromosomes[i] = Chromosomes[i].Clone();
                    if (LeaveInitialValues)
                    {
                        Array.Copy(InitialValues, Chromosomes[i].Values, InitialValues.Length);
                        Chromosomes[i].Fitness = Chromosomes[i].Evaluate();
                    }
                }
                Array.Sort(Chromosomes);

                Best = Chromosomes[0];

                NonProgressiveGenerations = 0;
                NonProgressiveScore = Best.Fitness;
            }

            GenerationRan(this);
        }

        public void Crossover(Chromosome first, Chromosome second, int pos)
        {
            if (first == null || second == null) return;
            int index = SRandom.Next(ChromosomeSize);
            int size = SRandom.Next(ChromosomeSize - index);
            if (size == ChromosomeSize) size = ChromosomeSize / 2;

            byte[] firstArray = new byte[ChromosomeSize];
            byte[] secondArray = new byte[ChromosomeSize];

            Array.Copy(first.Values, firstArray, ChromosomeSize);
            Array.Copy(second.Values, secondArray, ChromosomeSize);

            Array.Copy(first.Values, index, secondArray, index, size);
            Array.Copy(second.Values, index, firstArray, index, size);


            Chromosome firstNew = new Chromosome(Engine, AvailableValues, firstArray.Length, firstArray);
            Chromosome secondNew = new Chromosome(Engine, AvailableValues, secondArray.Length, secondArray);

            if (LeaveInitialValues)
            {
                Array.Copy(InitialValues, firstNew.Values, InitialValues.Length);
                Array.Copy(InitialValues, secondNew.Values, InitialValues.Length);
                firstNew.Fitness = firstNew.Evaluate();
                secondNew.Fitness = secondNew.Evaluate();
            }

            if (firstNew.Fitness > Chromosomes[pos - 1].Fitness && !Contains(firstNew))
                Chromosomes[pos - 1] = firstNew;

            if (secondNew.Fitness > Chromosomes[pos].Fitness && !Contains(secondNew))
                Chromosomes[pos] = secondNew;
        }

        public void Mutation(int cIndex)
        {
            Chromosome chromosome = Chromosomes[cIndex];
            if (chromosome == null) return;

            int type = SRandom.Next(3);

            byte[] newArray = new byte[chromosome.Values.Length];
            chromosome.Values.CopyTo(newArray, 0);
            switch (type)
            {
                case 0:
                    int index = SRandom.Next(ChromosomeSize);
                    int end = SRandom.Next(ChromosomeSize - index) + index;

                    for (int i = index; i < end; i++)
                    {
                        newArray[i] = AvailableValues.GetRandom();
                    }

                    if (LeaveInitialValues)
                    {
                        Array.Copy(InitialValues, newArray, InitialValues.Length);
                    }
                    break;

                case 1:
                    newArray[SRandom.Next(ChromosomeSize)] = 0;
                    break;

                case 2:
                    int randIndex1 = SRandom.Next(chromosome.Engine.ActionCount);
                    int randIndex2 = SRandom.Next(chromosome.Engine.ActionCount);
                    byte temp = newArray[randIndex1];
                    newArray[randIndex1] = newArray[randIndex2];
                    newArray[randIndex2] = temp;
                    break;
            }

            if (LeaveInitialValues)
            {
                Array.Copy(InitialValues, newArray, InitialValues.Length);
            }

            Chromosome newChromosome = new Chromosome(Engine, AvailableValues, ChromosomeSize, newArray);
            if (newChromosome.Fitness > chromosome.Fitness && !Contains(newChromosome))
                Chromosomes[cIndex] = newChromosome;
        }

        public void ChangeSize(int newSize)
        {
            MaxSize = newSize;

            Chromosome[] newArray = new Chromosome[MaxSize];
            Array.Copy(Chromosomes, newArray, Math.Min(MaxSize, Chromosomes.Length));
            Chromosomes = newArray;

            for (int i = 0; i < MaxSize; i++)
                if (Chromosomes[i] is null)
                    Chromosomes[i] = new Chromosome(Engine, AvailableValues, ChromosomeSize);
        }

        public bool Contains(Chromosome chromosome)
        {
            int length = Chromosomes.Length;
            for (int i = 0; i < length; i++)
                if (Chromosomes[i] == chromosome)
                    return true;
            return false;
        }

        public void Reevaluate(CraftingEngine sim, bool leaveInitialValues)
        {
            LeaveInitialValues = leaveInitialValues;
            Engine.SetRecipe(sim.CurrentRecipe);
            sim.CopyTo(Engine);

            InitialValues = sim.GetCraftingActions();
            ChromosomeSize = DefaultChromosomeSize;


            for (int i = 0; i < MaxSize; i++)
            {
                if (LeaveInitialValues)
                    Array.Copy(InitialValues, Chromosomes[i].Values, InitialValues.Length);
                Chromosomes[i].Engine.SetRecipe(sim.CurrentRecipe);
                sim.CopyTo(Chromosomes[i].Engine);
                Chromosomes[i].Fitness = Chromosomes[i].Evaluate();
            }
        }
    }
}
