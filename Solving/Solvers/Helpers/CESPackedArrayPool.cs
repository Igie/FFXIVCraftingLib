using FFXIVCraftingLib.Types;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVCraftingLib.Solving.Solvers.Helpers
{
    public static class CESPackedArrayPool
    {
        private static ArrayPool<CESPacked> Pool = ArrayPool<CESPacked>.Shared;
        private static int _Rented;
        public static int Rented => _Rented;

        public static ConcurrentDictionary<int, int> Refs = new();
        private static ConcurrentDictionary<nint, int> Id = new();
        private static int ID = 0;
        public unsafe static CESPacked[] Rent(int minimumLength)
        {
            //lock (Pool)
            {
                if (minimumLength == 0)
                    return Array.Empty<CESPacked>();
                _Rented++;
                //var states = Pool.Rent(minimumLength);
                var states = new CESPacked[minimumLength];
                //nint h = states.GetHashCode();
                //if (!Id.ContainsKey(h))
                //    Id[h] = ++ID;

                //int id = Id[h];

                //if (!Refs.ContainsKey(id))
                //    Refs[id] = 1;
                //else
                //    Refs[id]++;

                //if (id == 2 &&Refs[id] >= 1)
                //    Debugger.Break();

                for (int i = 0; i < states.Length; i++)
                {
                    if (!states[i].IsDisposed && states[i].Actions != null)
                        Debugger.Break();

                    //states[i] = new CESPacked();
                }
                return states;
            }
        }

        public static unsafe void Return(CESPacked[] array, bool clearArray = true)
        {
            //lock (Pool)
            {
                if (array.Length == 0)
                    return;
                _Rented--;

                //nint h = array.GetHashCode();
                //int id = Id[h];

                //if (!Refs.ContainsKey(id))
                //    throw new Exception();
                //else
                //    Refs[id]--;

                //if (Refs[id] > 2)
                //    Debugger.Break();

                for (int i = 0; i < array.Length; i++)
                {
                    if (!array[i].IsDisposed)
                    {
                        //Debugger.Break();
                        array[i].Dispose();
                    }

                    //array[i] = null;
                }
                //Pool.Return(array, clearArray);
            }
        }

        public static void DisposeStates(CESPacked[] array, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (!array[i].IsDisposed)
                    array[i].Dispose();
                else
                    Debugger.Break();
            }
        }

        public static void DisposeStates((CESPacked[] Array, int Length) states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (!states.Array[i].IsDisposed)
                    states.Array[i].Dispose();
                else
                    Debugger.Break();
            }
        }
    }

    public static class ByteArrayPool
    {
        private static ArrayPool<byte> Pool = ArrayPool<byte>.Shared;
        private static int _Rented;
        private static ConcurrentDictionary<int, byte[]> ArrayOwners = new ConcurrentDictionary<int, byte[]>();
        private static ConcurrentDictionary<byte[], int?> IDOwners = new();
        public static int Rented => _Rented;

        public static unsafe byte[] Rent(int minimumLength, int owner)
        {
            //lock (Pool)
            {
                _Rented++;
                
                //var array = Pool.Rent(minimumLength);
                var array = new byte[minimumLength];
                if (owner >= 0)
                {
                    if (ArrayOwners.ContainsKey(owner))
                    {
                        Debugger.Break();
                    }

                    if (IDOwners.ContainsKey(array))
                    {
                        Debugger.Break();
                    }

                    ArrayOwners[owner] = array;
                    IDOwners[array] = owner;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] != 0)
                    {
                        Debugger.Break();
                    }
                }
                return array;
            }
        }

        public static void Return(byte[] array, int owner, bool clearArray = true)
        {
            //lock (Pool)
            {
                _Rented--;

                if (owner >= 0)
                {
                    if (!ArrayOwners.ContainsKey(owner))
                    {
                        Debugger.Break();
                    }

                    if (!IDOwners.ContainsKey(array))
                    {
                        Debugger.Break();
                    }

                    ArrayOwners.Remove(owner, out var _);
                    IDOwners.Remove(array, out var _);
                }
                //Pool.Return(array, clearArray);
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = 0;
                    //if (array[i] != 0)
                    //    Debugger.Break();
                }
            }
        }
    }
}
