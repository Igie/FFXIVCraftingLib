using FFXIVCraftingLib.Actions;
using FFXIVCraftingLib.Solving.GeneticAlgorithm;
using System.Diagnostics;
using CA = FFXIVCraftingLib.Actions.CraftingAction;
namespace FFXIVCraftingLib.Solving.Solvers
{
    public sealed class GASolver : BaseSolver
    {
        public byte[] AvailableActions { get; private set; }

        public int TaskCount { get; set; }
        public Population[] Populations { get; private set; }
        private int BestIndex { get; set; }
        private Chromosome BestChromosome { get; set; }
        private Task[] Tasks { get; set; }

        public int Iterations { get; private set; }

        private bool Continue;
        public bool IsRunning { get; private set; }
        private bool NeedsUpdate { get; set; }

        public bool LeaveStartingActions { get; set; }
        public bool UseDictionary { get; set; }
        public bool CopyBestRotationToPopulations { get; set; }

        public int TimeLimit { get; set; }
        public int IterationLimit { get; set; }

        public bool WriteToDatabase { get; set; }

        public bool IsStartInitialized { get; private set; }

        public event Action<Population> GenerationRan = delegate { };
        public event Action<CraftingEngine> FoundBetterRotation = delegate { };
        public event Action Update = delegate { };
        public event Action Stopped = delegate { };

        public GASolver(CraftingEngine engine)
        : base(engine)
        {
            TaskCount = 10;
            CopyBestRotationToPopulations = false;
            LeaveStartingActions = false;
            WriteToDatabase = true;
            IsStartInitialized = false;
        }

        public async void AwaitedStart(int taskCount = 10, int chromosomeCount = 190, bool leaveStartingActions = false, int timeLimit = 0, int iterationLimit = 0, bool useDictionary = false, byte[] actions = null)
        {
            await Start(taskCount, chromosomeCount, leaveStartingActions, timeLimit, iterationLimit, useDictionary);
        }

        public async Task Start(int taskCount = 10, int chromosomeCount = 190, bool leaveStartingActions = false, int timeLimit = 0, int iterationLimit = 0, bool useDictionary = false, byte[] actions = null)
        {
            if (IsRunning) return;
            if (actions == null)
                actions = CA.Ids.Where(x => CA.GetLevel(x) <= Engine.Level && CA.GetSuccess(x) == 1).ToArray();
            if (Engine.Level >= CA.GetLevel((byte)CraftingActionID.TrainedEye) && Engine.CurrentRecipe != null && Engine.Level >= Engine.CurrentRecipe.Level + 10)
                actions = actions.Append((byte)CraftingActionID.TrainedEye).ToArray();
            AvailableActions = actions.ToArray();
            if (Populations == null)
            {
                Populations = new Population[taskCount];
                for (int i = 0; i < taskCount; i++)
                    Populations[i] = new Population(i, Engine.Clone(false), chromosomeCount, 40, AvailableActions);
            }

            Continue = true;
            NeedsUpdate = false;
            IsRunning = true;
            Iterations = 0;

            UseDictionary = useDictionary;

            TaskCount = taskCount;

            Tasks = new Task[TaskCount];

            if (Populations == null)
            {
                Populations = new Population[TaskCount];
                for (int i = 0; i < TaskCount; i++)
                    Populations[i] = new Population(i, Engine.Clone(false), chromosomeCount, 40, AvailableActions);
            }


            if (Populations.Length != TaskCount)
            {
                Population[] newPopulations = new Population[TaskCount];
                Array.Copy(Populations, newPopulations, Math.Min(TaskCount, Populations.Length));
                Populations = newPopulations;
            }

            for (int i = 0; i < TaskCount; i++)
            {
                if (Populations[i] == null)
                {
                    Populations[i] = new Population(i, Engine.Clone(false), chromosomeCount, 40, AvailableActions);
                }
                else if (Populations[i].Chromosomes.Length != chromosomeCount)
                {
                    Populations[i].ChangeSize(chromosomeCount);
                }
            }

            for (int i = 0; i < TaskCount; i++)
                Populations[i].ChangeAvailableValues(AvailableActions);

            LeaveStartingActions = leaveStartingActions;
            BestChromosome = new Chromosome(Engine.Clone(false), AvailableActions, 40, Engine.GetCraftingActions());

            for (int i = 0; i < Populations.Length; i++)
                Populations[i].PendingBest = BestChromosome.Clone();

            TimeLimit = timeLimit;
            IterationLimit = iterationLimit;

            IsStartInitialized = true;

            Task.Run(async () =>
            {
                Task t = UpdateLoop();
                for (int i = 0; i < TaskCount; i++)
                {
                    Tasks[i] = new Task(InnerStart, i);
                    Tasks[i].Start();
                }
                await t;
                Task.WaitAll(Tasks);
            });

            IsStartInitialized = false;
        }

        public async Task WaitForStartInitialization()
        {
            while (!IsStartInitialized)
            {
                await Task.Delay(25);
            }
        }

        private void InnerStart(object index)
        {
            int i = (int)index;
            Populations[i].Reevaluate(Engine, LeaveStartingActions);

            while (Continue)
            {
                Populations[i].RunOnce();
                Iterations++;
                GenerationRan(Populations[i]);

                var best = Populations[i].Best;
                if (BestChromosome.Fitness < best.Fitness && !NeedsUpdate)
                {
                    BestChromosome = best.Clone();
                    BestIndex = i;
                    NeedsUpdate = true;
                }
            }
        }

        private async Task UpdateLoop()
        {
            bool useTimeLimit = TimeLimit > 0;
            bool useIterationLimit = IterationLimit > 0;

            Stopwatch sw = Stopwatch.StartNew();

            while (Continue)
            {
                if (NeedsUpdate)
                {
                    Engine.RemoveActions();
                    Engine.AddActions(true, BestChromosome.Values.Where(y => y > 0).ToArray());
                    NeedsUpdate = false;

                    if (CopyBestRotationToPopulations)
                        for (int i = 0; i < Populations.Length; i++)
                            Populations[i].PendingBest = BestChromosome.Clone();
                    CraftingEngine engine = Engine.Clone(true);
                    //if (WriteToDatabase)
                        //Utils.AddRotationFromSim(sim);
                    FoundBetterRotation(engine);
                }

                if (useTimeLimit && sw.ElapsedMilliseconds >= TimeLimit || useIterationLimit && Iterations >= IterationLimit)
                    Continue = false;
                else
                    await Task.Delay(100);
                Update();
            }
            await Task.WhenAll(Tasks);
            IsRunning = false;
            Stopped();
        }

        public async Task Stop()
        {
            if (!IsRunning) return;
            Continue = false;
            await Task.WhenAll(Tasks);
        }
    }
}
