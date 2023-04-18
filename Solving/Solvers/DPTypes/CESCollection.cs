using FFXIVCraftingLib.Solving.Solvers.Helpers;
using FFXIVCraftingLib.Types;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using AP = FFXIVCraftingLib.Solving.Solvers.Helpers.CESPackedArrayPool;

namespace FFXIVCraftingLib.Solving.Solvers.DPTypes
{
    public sealed class CESCollection : IDisposable
    {
        public static HashSet<int> DefaultUniqueStates = new HashSet<int>();
        public const int MAX_STATES_LENGTH = 524288 << 0;
        public const int INITIAL_STATES_LENGTH = 8;
        public const int KEEP_BEST_STATES_LENGTH = MAX_STATES_LENGTH >> 0;
        public const int MAX_ACTIONS = 18;

        public const int REMOVE_SORTING_TRESHOLD = 10000;

        public static readonly CESPacked EmptyState = new CESPacked();

        public HashSet<int> UniqueStates;

        public int Depth { get; private set; }
        private CESPacked[] States;
        private int _Length;
        public int Length => _Length;

        //private int ThreadCount;
        //private string LastCaller;
        //private string LastCallerPath;
        //private int LastCallerLine;

        private static object IDCountLock = new object();
        private static int IDCount = 0;
        private int ID;

        private CESCollection Parent;
        private int ChildrenCount;
        private int CurrentIndex;
        //private List<string> CallMessages = new List<string>(64);
        public CESCollection(int depth, int maxStatesLength = MAX_STATES_LENGTH)
            : this(depth, DefaultUniqueStates, maxStatesLength)
        {
        }

        public CESCollection(int depth, HashSet<int> uniqueStates, int maxStatesLength = MAX_STATES_LENGTH)
        {
            Depth = depth;
            UniqueStates = uniqueStates;
            States = AP.Rent(INITIAL_STATES_LENGTH);
            _Length = 0;

            //ThreadCount = 0;
            lock (IDCountLock)
            {
                ID = IDCount++;
            }
        }

        public string GetParentString()
        {
            string s = string.Empty;

            CESCollection c = this;
            while(c != null)
            {
                s = ' ' + s;
                if (c.Parent != null)
                    s  = c.Parent.ChildrenCount + s;
                else
                    s = '1' + s;
                s = '|' + s;

                s = c.CurrentIndex + s;
                
                c = c.Parent;
            }
            return s;
        }


        public void Initialize(ReadOnlySpan<CESPacked> initialStates, CESCollection parent, int index)
        {
            
            if (MAX_STATES_LENGTH < initialStates.Length)
                throw new ArgumentException("length is too big");

            if (States.Length < initialStates.Length)
            {
                int min = States.Length;
                while (min < initialStates.Length)
                    min <<= 1;

                Array.Resize(ref States, min);
            }

            for (int i = 0; i < initialStates.Length; i++)
            {
                if (initialStates[i].IsDisposed)
                    throw new Exception();
                States[i] = new CESPacked(initialStates[i], true);
            }
            _Length = initialStates.Length;

            Parent = parent;
            CurrentIndex = index;
        }

        public bool CanGenerateStates()
        {
            return _Length * MAX_ACTIONS < MAX_STATES_LENGTH;
        }

        private static (CESPacked[], int) GenerateStates(CESPacked[] states, int length, CraftingEngine e)
        {
            CESPacked[] resultStates = new CESPacked[length * MAX_ACTIONS];

            CESPacked[] newStatesBuffer = AP.Rent(MAX_ACTIONS);
        byte[] actionsBuffer = ByteArrayPool.Rent(MAX_ACTIONS, -1);

        int count;
            //lock (states)
            {
                count = 0;
                for (int i = 0; i < length; i++)
                {
                    if (count > resultStates.Length)
                        throw new Exception("States size is too low");
                    int l = states[i].FillStates(e, newStatesBuffer, actionsBuffer);
                    if (l > MAX_ACTIONS)
                        throw new Exception("Need to increase MAX_ACTIONS count");
                    CESPacked.Copy(newStatesBuffer, 0, resultStates, count, l);

                    count += l;
                }
            }

            return (resultStates, count);
        }

        public void GenerateStates(CraftingEngine e, bool removeWorse = true)
        {
            CESPacked[] newStatesBuffer = AP.Rent(MAX_ACTIONS);
            byte[] actionsBuffer = ByteArrayPool.Rent(MAX_ACTIONS, -1);

            Array.Sort(States, 0, _Length, CESPackedComparer.ByDurability);
            int rent = 1;
            while (rent < _Length * MAX_ACTIONS)
                rent <<= 1;
            if (rent > MAX_STATES_LENGTH)
                throw new Exception();
            CESPacked[] result = AP.Rent(rent);

            //lock (States)
            {
                int count = 0;
                for (int i = 0; i < _Length; i++)
                {
                    if (count > result.Length)
                        throw new Exception("States size is too low");
                    int l = States[i].FillStates(e, newStatesBuffer, actionsBuffer);
                    if (l > MAX_ACTIONS)
                        throw new Exception("Need to increase MAX_ACTIONS count");
                    CESPacked.Copy(newStatesBuffer, 0, result, count, l);

                    count += l;
                }

                AP.DisposeStates(States, _Length);
                AP.Return(States);
                Array.Sort(result, 0, count, CESPackedComparer.ByDurability);

                if (count > MAX_STATES_LENGTH)
                {
                    throw new Exception();
                }
                else
                {
                    rent = 1;
                    while (rent < count)
                        rent <<= 1;
                    States = AP.Rent(rent);
                }
                CESPacked.Copy(result, States, count);
                _Length = count;
                Depth++;
                AP.DisposeStates(result, count);
                AP.Return(result);
               
            }
            //RemoveEqualStates();
            if (removeWorse)
            {
                RemoveWorseStates();
                if (_Length == 0)
                    Debugger.Break();
            }
        }

        public void GenerateStatesNoLimit(CraftingEngine e, bool removeWorse = true)
        {
            //CallMessages.Add("GenerateStatesNoLimit enter");
            //ThreadCheckAndIncrease();
            CESPacked[] fullStates = AP.Rent(_Length * MAX_ACTIONS);
            CESPacked.Copy(States, fullStates, _Length);
            AP.DisposeStates(States, _Length);
            AP.Return(States);
            States = fullStates;
            //ThreadCount--;
            GenerateStates(e, removeWorse);
            Depth++;
            //ThreadCheckAndIncrease();
            //ThreadCount--;
            //CallMessages.Add("GenerateStatesNoLimit exit");
        }

        public (CESCollection[], int, int) SplitStates(CraftingEngine e, bool shouldGenerate)
        {
            Array.Sort(States, 0, _Length, CESPackedComparer.ByCP);

            (CESPacked[] cesPackedGenerated, int length) = GenerateStates(States, _Length, e);
            shouldGenerate = false;

            int removed = 0;//RemoveEqualStates(UniqueStates, cesPackedGenerated, ref length);
            
            if (length >= 10000)
                removed += RemoveWorseStates_Sorting(cesPackedGenerated, ref length);
            else
                removed += RemoveWorseStates_TwoPointers(cesPackedGenerated, ref length);

            Depth++;
            int numberInnerStates = (int)Math.Ceiling(length / (double)MAX_STATES_LENGTH);
            if (numberInnerStates <= 1)
            {
                AP.DisposeStates(States, _Length);
                AP.Return(States);
                States = Array.Empty<CESPacked>();

                int rent = 1;

                if (rent < length)
                {
                    while (rent < length)
                        rent <<= 1;
                }
                States = AP.Rent(rent);

                if (rent > MAX_STATES_LENGTH)
                    throw new Exception();

                CESPacked.Copy(cesPackedGenerated, States, length);
                _Length = length;
                cesPackedGenerated = null;
                return (null, 1, removed);
            }
            if (numberInnerStates < 16)
            numberInnerStates = 16;

            //CESPacked[][] completedInnerStates = ArrayPool<CESPacked[]>.Shared.Rent(numberInnerStates);
            CESCollection[] slices = ArrayPool<CESCollection>.Shared.Rent(numberInnerStates);
            //numberInnerStates = slices.Length;

            ChildrenCount = numberInnerStates;

            Array.Sort(cesPackedGenerated, 0, length, CESPackedComparer.ByCP);

            for (int i = 0; i < numberInnerStates; i++)
            {
                int start = (int)(i * ((double)length / numberInnerStates));
                int end = (int)((i + 1) * ((double)length / numberInnerStates));
                int sliceLength = end - start;

                slices[i] = new CESCollection(Depth);
                slices[i].Initialize(cesPackedGenerated.AsSpan(start, sliceLength), this, i);
            }
            
            return (slices, numberInnerStates, removed);
        }

        public (CESPacked[], int) FindCompletedStates()
        {
            //CallMessages.Add("FindCompletedStates enter");
            //ThreadCheckAndIncrease();
            CESPacked[] full = AP.Rent(_Length);
            int index = 0;
            for (int i = 0; i < _Length; i++)
            {
                if (States[i].IsMaxProgress && States[i].IsMaxQuality)
                {
                    full[index++] = new CESPacked(States[i], true);
                }
            }

            //ThreadCount--;
            //CallMessages.Add("FindCompletedStates exit");
            CESPacked[] completed = AP.Rent(index);
            CESPacked.Copy(full, completed, index);
            AP.DisposeStates(full, full.Length);
            AP.Return(full);
            return (completed, index);
        }

        private void RemoveEqualStates()
        {
            //CallMessages.Add("RemoveEqualStates enter");
            //ThreadCheckAndIncrease();
            bool[] existing = ArrayPool<bool>.Shared.Rent(_Length);
            int existingCount = 0;
            lock (UniqueStates)
            {
                for (int i = 0; i < _Length; i++)
                {
                    if (!UniqueStates.Add(States[i].GetHashCode()))
                    {
                        existing[i] = true;
                        States[i].Dispose();
                        existingCount++;
                    }
                    else
                        existing[i] = false;

                }
            }
            if (existingCount > 0)
            {
                //ThreadCount--;
                Sort(existing);
                //ThreadCheckAndIncrease();
            }
            _Length -= existingCount;

            ArrayPool<bool>.Shared.Return(existing, true);
            //ThreadCount--;

            //CallMessages.Add("RemoveEqualStates exit");
        }

        private static int RemoveEqualStates(HashSet<int> uniqueStates, CESPacked[] states, ref int length)
        {
            bool[] existing = ArrayPool<bool>.Shared.Rent(length);
            int existingCount = 0;
            lock (uniqueStates)
            {
                for (int i = 0; i < length; i++)
                {
                    if (!uniqueStates.Add(states[i].GetHashCode()))
                    {
                        existing[i] = true;
                        states[i].Dispose();
                        existingCount++;
                    }
                    else
                        existing[i] = false;

                }
            }
            if (existingCount > 0)
                Sort(states, length, existing);
            length -= existingCount;

            ArrayPool<bool>.Shared.Return(existing, true);
            return existingCount;
        }

        public int RemoveWorseStates()
        {
            var sw2 = Stopwatch.StartNew();
            int removed;
            if (_Length > REMOVE_SORTING_TRESHOLD)
                removed = RemoveWorseStates_Sorting(States, ref _Length);
            else
                removed = RemoveWorseStates_TwoPointers(States, ref _Length);

            sw2.Stop();
            int ms2 = (int)sw2.ElapsedMilliseconds;

            //ThreadCheckAndIncrease();
            //ThreadCount--;
            //CallMessages.Add("RemoveWorseStates exit");
            return removed;
        }

        public int RemoveWorseStates(CESPacked[] best, int length)
        {
            int right = _Length - 1;
            int removedCount = 0;

            for (int i = 0; i < right; i++)
            {
                for (int j = 0; j < length; j++)
                {

                    if (States[i].AllWorseOrEqual(best[j]))
                    {
                        CESPacked temp = States[i];
                        States[i] = States[right];
                        States[right] = temp;

                        right--;

                        removedCount++;
                        i--;
                        break;
                    }
                }
            }

            _Length -= removedCount;
            int to = _Length + removedCount;
            for (int i = _Length; i < to; i++)
            {
                if (!States[i].IsDisposed)
                {
                    States[i].Dispose();
                }
                else
                    Debugger.Break();
            }

            return removedCount;
        }

        public static int RemoveWorseStates_TwoPointers(CESPacked[] packedArray, ref int length)
        {
            int right = length - 1;
            int removedCount = 0;

            for (int i = 0; i < right; i++)
            {
                bool isWorse = false;

                for (int j = i + 1; j <= right; j++)
                {
                    if (packedArray[i].AllWorseOrEqual(packedArray[j]))
                    {
                        isWorse = true;
                        break;
                    }
                }

                if (isWorse)
                {
                    CESPacked temp = packedArray[i];
                    packedArray[i] = packedArray[right];
                    packedArray[right] = temp;

                    right--;

                    removedCount++;
                    i--;
                }
            }

            length -= removedCount;
            int to = length + removedCount;
            for (int i = length; i < to; i++)
            {
                if (!packedArray[i].IsDisposed)
                {
                    packedArray[i].Dispose();
                }
                else
                    Debugger.Break();
            }

            return removedCount;
        }

        public static int RemoveWorseStates_Sorting(CESPacked[] packedArray, ref int length)
        {
            CESPackedComparer comparer = new CESPackedComparer();
            int removedCount = 0;
            bool[] check = ArrayPool<bool>.Shared.Rent(length);
            for (int i = 0; i < length; i++)
                check[i] = false;
            int currentFieldRemoved = 0;

            //if (length == 375) Debugger.Break();

            for (int field = 0; field <= 12; field++)
            {
                currentFieldRemoved = 0;
                comparer.SetIndex(field);

                Array.Sort(packedArray, 0, length, comparer);

                int bestItemIndex = 0;

                for (int i = 1; i < length; i++)
                {

                    if (packedArray[i].AllWorseOrEqual(packedArray[bestItemIndex]))
                    {
                        check[i] = true;
                        currentFieldRemoved++;
                    } 
                    else
                    {
                        bestItemIndex = i;
                    }
                }

                if (currentFieldRemoved > 0)
                {
                    Sort(packedArray, length, check);
                    removedCount += currentFieldRemoved;
                    length -= currentFieldRemoved;
                }

            }

            ArrayPool<bool>.Shared.Return(check, true);

            int to = length + removedCount;
            for (int i = length; i < to; i++)
            {
                if (!packedArray[i].IsDisposed)
                {
                    packedArray[i].Dispose();
                }
                //else
                    //Debugger.Break();
            }

            return removedCount;
        }

        public static int RemoveWorseStates_Multithreaded(CESPacked[] packedArray, ref int length)
        {
            if (length == 0) return 0;
            bool[] badIndexes = ArrayPool<bool>.Shared.Rent(length);
            for (int i = 0; i < length; i++)
            {
                badIndexes[i] = false;
            }
            //var copyArray = AP.Rent(length);
            //CESPacked.Copy(packedArray, copyArray, length);
            int l = length;
            int numProcessors = Environment.ProcessorCount;
            if (numProcessors < 0) numProcessors = 0;
            if (numProcessors > length - 1) numProcessors = length - 1;
            //TaskFactory f = new TaskFactory();
 
            Parallel.For(0, numProcessors, (i) =>
            {
                int start = (int)(Math.Floor(l / (double)numProcessors * i));
                int end = (int)Math.Floor(l / (double)numProcessors * (i + 1));

                CheckRange(packedArray, badIndexes, start, end);
            });

            int removed = badIndexes.Take(length).Count(x => x);

            Sort(packedArray, length, badIndexes);
            ArrayPool<bool>.Shared.Return(badIndexes, true);
            //AP.Return(copyArray);
            length -= removed;
            int to = length + removed;
            for (int i = length; i < to; i++)
            {
                if (!packedArray[i].IsDisposed)
                {
                    packedArray[i].Dispose();
                }
                else
                    Debugger.Break();
            }

            return removed;
        }

        private static void CheckRange(CESPacked[] copyArray, bool[] shouldRemove, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex - 1; i++)
            {
                if (shouldRemove[i]) continue;

                for (int j = i + 1; j < endIndex; j++)
                {
                    if (shouldRemove[i])
                        break;
                    if (shouldRemove[j])
                        continue;

                    if (copyArray[i].AllWorseOrEqual(copyArray[j]))
                    {
                        lock (shouldRemove) shouldRemove[i] = true;
                        break;
                    }
                    if (copyArray[j].AllWorseOrEqual(copyArray[i]))
                    {
                        lock (shouldRemove) shouldRemove[j] = true;
                    }
                }
            }

            //Debugger.Log(0, null, $"{startIndex} finished\r\n");
        }

        public void Sort(bool[] badIndexes)
        {
            //CallMessages.Add("Sort enter");
            //ThreadCheckAndIncrease();
            Sort(States, _Length, badIndexes);
            //ThreadCount--;
            //CallMessages.Add("Sort exit");
        }

        public static void Sort(CESPacked[] states, int length, bool[] badIndexes)
        {
            int badIndexCount = badIndexes.Take(length).Count(x => x);
            if (badIndexCount == length)
            {
                states = Array.Empty<CESPacked>();
                badIndexes = Array.Empty<bool>();
                badIndexCount = 0;
                return;
            }
            int newLength = length - badIndexCount;

            int firstTrue = 0;

            int lastFalse = length - 1;
            while (firstTrue < newLength)
            {
                if (badIndexes[firstTrue])
                {
                    while (badIndexes[lastFalse])
                        lastFalse--;

                    Swap(states, firstTrue, lastFalse);
                    badIndexes[firstTrue] = false;
                    badIndexes[lastFalse] = true;
                }
                firstTrue++;
            }
        }

        private static void Swap<T>(T[] source, long index1, long index2)
        {
            T temp = source[index1];
            source[index1] = source[index2];
            source[index2] = temp;
        }

        public bool IsDisposed { get; private set; }
        public void Dispose()
        {
            if (IsDisposed)
            {
                Debugger.Break();
                return;
            }
            lock (States){


                IsDisposed = true;
                AP.DisposeStates(States, _Length);
                AP.Return(States);
                States = null;
                UniqueStates = null;
                _Length = 0; }
        }
    }
}
