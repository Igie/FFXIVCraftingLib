using FFXIVCraftingLib.Actions;
using FFXIVCraftingLib.Solving.Solvers.DPTypes;
using FFXIVCraftingLib.Solving.Solvers.Helpers;
using FFXIVCraftingLib.Types;
using FFXIVDataManager.GameData;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using CA = FFXIVCraftingLib.Actions.CraftingAction;
using AP = FFXIVCraftingLib.Solving.Solvers.Helpers.CESPackedArrayPool;
using BAP = FFXIVCraftingLib.Solving.Solvers.Helpers.ByteArrayPool;
using System.Runtime.Serialization.Formatters;
using SaintCoinach.Text;

namespace FFXIVCraftingLib.Solving.Solvers
{
    /* Some explanation for myself on how this works
     * Approaches:
     * 
     * 1. We choose a point in rotation where we think is the middle point. 
     *    To explain in depth, choose parameters that we think are the best and create actions that reach these values
     *    and actions that complete from these values
     * 2. Dynamic programming. It however has multiple restrictions. Multidimensional DP array that holds engine state?
     * 3. (takes too much time) Brute force all actions
     * 4. Same as 3, but we could instead use action combinations and reduce number of possible items in brute force items, requires combinations generation
     * 5. Instead of combinations, we could brute force next x actions, select best and continue from there\
     * 
     * Whatcha think chap? 
     * I think I will stick with DP and engine states for now...
     */


    //work in progress, currently does not work
    public sealed class DPSolver : BaseSolver
    {
        private const bool MULTITHREADING = true;

        // note: we can remove states by comparing them in state collections

        private ConcurrentDictionary<int, Queue<DPStackItem>> Stack;
        private ThreadLocal<CraftingEngine> ThreadLocalEngine;

        private int StackItemCount;
        private int MaxDepth;
        private int MinDepth;
        private int[] Depths;

        private CESPacked[] BestStates;
        private int BestStatesLength;
        private int BestStep;
        private CraftingEngine ClonedEngine
        {
            get => ThreadLocalEngine.Value;
        }

        public bool IsRunning { get; private set; }

        private int UniqueTasks;
        public event Action Finished = delegate { };
        public DPSolver(CraftingEngine engine)
            : base(engine)
        {
            ThreadLocalEngine = new ThreadLocal<CraftingEngine>(() => engine.Clone(false));
            IsRunning = false;
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            if (BestStates != null)
            {
                AP.DisposeStates(BestStates, BestStatesLength);
                AP.Return(BestStates);
                BestStates = null;
            }

            BestStep = 40;
            MaxDepth = 0;
            MinDepth = 30;
            Thread thread = new Thread(AwaitedStart);
            thread.Start();
        }
        CESCollection initialCollection;
        private void AwaitedStart()
        {
            UniqueTasks = 0;
            Stack = new();
            byte[] initial = new byte[] { CraftingActions.MuscleMemory, CraftingActions.Manipulation, CraftingActions.Veneration, CraftingActions.WasteNot, CraftingActions.Groundwork };
            //initial = new byte[0];
            Depths = new int[40];

            CESPacked[] initialStates = new CESPacked[1];
            ClonedEngine.AddActions(true, initial);
            initialStates[0] = new CESPacked(ClonedEngine);


            initialCollection = new CESCollection(initialStates[0].ActionsLength);


            initialCollection.Initialize(initialStates.AsSpan(0, 1), null, 0);
            AP.DisposeStates(initialStates, initialStates.Length);
            AP.Return(initialStates);
            var (completed, completedLength) = GenerateBestStates(initialCollection, true);
            //initialCollection.Dispose();
            if (completedLength == 0) return;

            var completedStates = new CESPacked[completedLength];
            CESPacked.Copy(completed, completedStates, completedLength);
            AP.DisposeStates(completed, completedLength);
            AP.Return(completed);
            if (BestStatesLength > 0)
            {
                AP.DisposeStates(BestStates, BestStatesLength);
                AP.Return(BestStates);
                BestStates = null;
            }
            Array.Sort(completedStates, CESPackedComparer.ByScore);

            IsRunning = false;

            Engine.RemoveActions();
            Engine.AddActions(true, completedStates[0].Actions.Take(completedStates[0].ActionsLength).ToArray());

            Finished();
        }

       
        private (CESPacked[], int) GenerateBestStates(CESCollection currentStates, bool removeWorse, int currentLevel = 0)
        {
            if (currentStates == null)
            {
                throw new Exception();
            }

            CESPacked[] completedStates = null;
            int completedStatesLength = 0;
            do
            {
                (completedStates, completedStatesLength) = currentStates.FindCompletedStates();

                if (currentStates.Length == 0 || (BestStates != null && BestStep < currentStates.Depth))
                {
                    currentStates.Dispose();
                    return (completedStates, completedStatesLength);
                }

                if (completedStatesLength > 0)
                {
                    currentStates.Dispose();

                    if (BestStates == null)
                    {
                        BestStates = AP.Rent(completedStatesLength);
                        CESPacked.Copy(completedStates, BestStates, completedStatesLength);
                        BestStatesLength = completedStatesLength;
                        BestStep = BestStates[0].Step;
                    } else
                    {
                        bool better = false;
                        if (completedStates[0].Step < BestStep)
                        {
                            better = true;
                            BestStep = completedStates[0].Step;
                        } else
                        for (int i = 0; i < BestStatesLength; i++)
                        {
                            
                            for (int j= 0; j < completedStatesLength; j++)
                                if (BestStates[i].Score < completedStates[j].Score)
                                {
                                    better = true;
                                    break;
                                }
                            if (better)
                                break;

                        }

                        if (better)
                        {
                            lock (BestStates)
                            {
                                AP.DisposeStates(BestStates, BestStatesLength);
                                AP.Return(BestStates);
                                BestStates = null;
                                BestStates = AP.Rent(completedStatesLength);
                                BestStatesLength = completedStatesLength;
                                CESPacked.Copy(completedStates, BestStates, completedStatesLength);
                            }
                        }
                    }



                    return (completedStates, completedStatesLength);
                }
                if (currentStates.Depth >= BestStep)
                {
                    currentStates.Dispose();
                    return (completedStates, completedStatesLength);
                }
                //removeWorse = false;
                int removedNow = 0;
                if (removeWorse /*&& currentStates.Length * CESCollection.MAX_ACTIONS > CESCollection.MAX_STATES_LENGTH*/)
                {
                    removedNow = 0;
                    if (BestStates != null)
                    {
                        lock (BestStates)
                        {
                            removedNow = currentStates.RemoveWorseStates(BestStates, BestStatesLength);
                        }
                    }
                    if (removedNow > 0)
                    {
                        //Debugger.Break();
                    }
                    removedNow += currentStates.RemoveWorseStates();
                }

                Debugger.Log(0, "", $"{"",-10} depth:{currentStates.Depth,-17} removed:{removedNow,-7} left:{currentStates.Length,-6} level:{currentLevel,-6} parent:{currentStates.GetParentString()}\r\n");

                if (currentStates.CanGenerateStates())
                {
                    currentStates.GenerateStates(ClonedEngine, false);
                    removeWorse = true;
                }
                else
                {
                    //Debugger.Log(0, "", $" reducing and splitting states\r\n");
                    bool shouldGenerateWithNoLimit = false;
                    if (shouldGenerateWithNoLimit)
                    {
                        currentStates.GenerateStatesNoLimit(ClonedEngine, true);
                    }

                    int oldStatesLength = currentStates.Length;
                    (var slices, int numberInnerStates, int removed) = currentStates.SplitStates(ClonedEngine, !shouldGenerateWithNoLimit);

                    if (numberInnerStates == 1)
                    {
                        removeWorse = false;
                        Debugger.Log(0, "", $"{"",-10} depth:{currentStates.Depth,-17} removed:{removed,-7} left:{currentStates.Length,-6} level:{currentLevel,-6} parent:{currentStates.GetParentString()}\r\n");
                        continue;
                    }

                    Debugger.Log(0, "", $"{"",-10} depth:{currentStates.Depth,-5} inner:{numberInnerStates, -5} removed:{removed,-19} level:{currentLevel,-6} parent:{currentStates.GetParentString()}\r\n");

                    (CESPacked[], int)[] completedInnerStates = ArrayPool<(CESPacked[], int)>.Shared.Rent(numberInnerStates);
                    //ArrayPool<CESPacked[]>.Shared.Rent(numberInnerStates);

                    currentStates.Dispose();

                    int completedCount = 0;


                        DPStackItem[] tasks = new DPStackItem[numberInnerStates];
                        //Debugger.Log(0, "", $"creating parallel call\r\n");
                        lock(Depths)
                        for (int i = 0; i < numberInnerStates; i++)
                        {
                            //if (slices[i].Length == 0)
                            //    continue;
                            DPStackItem item = new DPStackItem(slices[i], false, currentLevel + 1);
                            tasks[i] = item;
                        }

                        AddLevel(tasks, numberInnerStates);

                        GenerateBestStatesLoop(numberInnerStates, tasks[0].States.Depth);
                        bool shouldWait;
                        do
                        {
                            Thread.Sleep(10);
                            shouldWait = false;
                            foreach(var t in tasks)
                            {
                                if (!t.Completed)
                                {
                                    shouldWait = true;
                                    break;
                                }
                            }

                        } while (shouldWait);
                        lock (Depths)
 
                        for (int i = 0; i < numberInnerStates; i++)
                        {
                            while(tasks[i].Result.Item1 == null)
                                Thread.Sleep(10);
                            completedInnerStates[i] = tasks[i].Result;
                            completedCount += completedInnerStates[i].Item2;
                            tasks[i].Dispose();
                        }
                    
                    //lock (ArrayPool<CESCollection>.Shared)
                        ArrayPool<CESCollection>.Shared.Return(slices, true);

                    
                    if (completedCount == 0)
                    {
                        for (int i  = 0; i < numberInnerStates; i++)
                            AP.Return(completedInnerStates[i].Item1, true);
                        ArrayPool<(CESPacked[], int)>.Shared.Return(completedInnerStates, true);
                        return (AP.Rent(0), 0);
                    }
                    completedStates = AP.Rent(completedCount);
                    int currentLength = 0;
                    for (int i = 0; i < numberInnerStates; i++)
                    {
                        CESPacked.Copy(completedInnerStates[i].Item1, 0, completedStates, currentLength, completedInnerStates[i].Item2);
                        currentLength += completedInnerStates[i].Item2;
                        AP.DisposeStates(completedInnerStates[i]);
                        AP.Return(completedInnerStates[i].Item1);
                    }
                    ArrayPool<(CESPacked[], int)>.Shared.Return(completedInnerStates, true);
                    return (completedStates, currentLength);
                }

            } while (IsRunning);

            return (completedStates, completedStatesLength);
        }

        

        private (CESPacked[], int) GenerateBestStatesStack(DPStackItem item)
        {
            return GenerateBestStates(item.States, item.RemoveWorse, item.CurrentLevel);
        }

        private void GenerateBestStatesLoop(int count = 1, int depth = 1)
        {
            //lock (Stack)
            for (int i = 0; i < count; i++)
            {
                if (StackItemCount == 0) return;

                if (MULTITHREADING && UniqueTasks < 8 && depth > 12)
                {
                    Thread thread = new Thread(StateLoop);
                    thread.Start();
                }
                else StateLoop();
            }

        }

        private void StateLoop()
        {
            UniqueTasks++;
            while (StackItemCount > 0 || IsRunning)
            {
                var item = GetLevel();
                if (item == null)
                    return;
                var result = GenerateBestStatesStack(item);
                item.Completed = true;
                item.Result = result;

            }
            UniqueTasks--;
        }


        private void AddLevel(DPStackItem[] items, int length)
        {
            lock(Depths)
            {
                for (int i = 0; i < length; i++)
                {
                    int depth = items[i].States.Depth;
                    Depths[depth]++;
                    if (!Stack.ContainsKey(depth))
                        Stack[depth] = new();
                    Stack[depth].Enqueue(items[i]);

                    if (MaxDepth < depth) MaxDepth = depth;
                    if (MinDepth > depth || MinDepth == -1) MinDepth = depth;
                    StackItemCount++;
                }
            }
        }

        private DPStackItem GetLevel()
        {
            try
            {
                lock (Depths)
                {
                    if (MaxDepth == -1) return null;
                    //if (MinDepth == -1) return null;
                    Depths[MaxDepth]--;

                    var item = Stack[MaxDepth].Dequeue();
                    if (Depths[MaxDepth] == 0)
                    {
                        MaxDepth = -1;
                        for (int i = 0; i < Depths.Length; i++)
                            if (MaxDepth < i && Depths[i] > 0)
                                MaxDepth = i;
                    }

                    //if (Depths[MinDepth] == 0)
                    //{
                    //    MinDepth = -1;
                    //    for (int i = 0; i < Depths.Length; i++)
                    //        if (Depths[i] > 0)
                    //        {
                    //            MinDepth = i;
                    //            break;
                    //        }
                    //}

                    StackItemCount--;
                    return item;
                }
            }
            catch { };

            return null;
        }





        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
        }
    }
}
