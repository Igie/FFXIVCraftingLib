using FFXIVCraftingLib.Actions;
using System.Reflection;
using System;
using CA = FFXIVCraftingLib.Actions.CraftingAction;
using System.Runtime.CompilerServices;
using System.Buffers;
using FFXIVCraftingLib.Solving.Solvers.Helpers;
using System.Diagnostics;
using BAP = FFXIVCraftingLib.Solving.Solvers.Helpers.ByteArrayPool;
using System.Reflection.Metadata.Ecma335;
using FFXIVCraftingLib.Solving.Solvers.DPTypes;

namespace FFXIVCraftingLib.Types
{
    public sealed class CESPackedComparer : IComparer<CESPacked>
    {
        public static readonly CESPackedComparer ByDurability = new CESPackedComparer(-1);
        public static readonly CESPackedComparer ByScore = new CESPackedComparer(-2);
        public static readonly CESPackedComparer ByCP = new CESPackedComparer(-3);
        private int _Index;

        public CESPackedComparer( int index = 0)
        {
            _Index = index;
        }

        public void SetIndex(int index)
        {
            if (index > 12 || index < -3)
                throw new ArgumentOutOfRangeException("index");

            _Index = index;
        }

        public int Compare(CESPacked one, CESPacked two)
        {
            if (one.IsDisposed && two.IsDisposed) return 0;
            if (one.IsDisposed) return 1;
            if (two.IsDisposed) return 1;

            if (_Index == -1)
                return one.Durability.CompareTo(two.Durability);
            if (_Index == -2)
                return -one.Score.CompareTo(two.Score);
            if (_Index == -3)
                return one.CPRemaining.CompareTo(two.CPRemaining);

            int result = Compare(_Index, one, two);
            if (result != 0) return result;

            for (int i = 0; i <= 12; i++)
            {
                if (_Index == i) continue;

                result = Compare(i, one, two);
                if (result != 0) return result;
            }

            return 0;
        }
        //public int Compare(CESPacked one, CESPacked two)
        //{
        //    if (one.IsDisposed && two.IsDisposed) return 0;
        //    if (one.IsDisposed) return 1;
        //    if (two.IsDisposed) return 1;

        //    switch(_Index)
        //    {
        //        case 0:
        //            if (one.Durability != two.Durability) return -one.Durability.CompareTo(two.Durability);
        //            break;

        //        case 1:
        //            if (one.CPRemaining != two.CPRemaining) return -one.CPRemaining.CompareTo(two.CPRemaining);
        //            break;

        //        case 2:
        //            if (one.Progress != two.Progress) return -one.Progress.CompareTo(two.Progress);
        //            break;

        //        case 3:
        //            if (one.Quality != two.Quality) return -one.Quality.CompareTo(two.Quality);
        //            break;

        //    }
        //    if (_Index > 3)
        //    {
        //        int bitFieldIndex = _Index - 3;
        //            if ((one.BitField1 & CESPacked.ShiftedBitSizes[bitFieldIndex]) != (two.BitField1 & CESPacked.ShiftedBitSizes[bitFieldIndex]))
        //                return -(one.BitField1 & CESPacked.ShiftedBitSizes[bitFieldIndex]).CompareTo(two.BitField1 & CESPacked.ShiftedBitSizes[bitFieldIndex]);
        //    }
        //    return 0;
        //}

        public int Compare(int index, CESPacked one, CESPacked two)
        {
            return -one[index].CompareTo(two[index]); 
        }
    }
    public readonly struct CESPacked : IEquatable<CESPacked>, IComparable<CESPacked>, IDisposable
    {
        public static int MAX_ACTIONS_LENGTH = 25;
        public int this[int index]
        {
            get
            {
                if (index < 0 || index > 12)
                    throw new ArgumentOutOfRangeException("index");
                switch (index)
                {
                    case 0:
                        return Durability;

                    case 1:
                        return CPRemaining;

                    case 2:
                        return Progress;

                    case 3:
                        return Quality;

                }

                int bitFieldIndex = index - 3;
                return (int)(BitField1 & ShiftedBitSizes[bitFieldIndex]);
            }
        }

        public readonly byte Step;
        public readonly int Progress;
        public readonly int Quality;
        public readonly ushort CPRemaining;
        public readonly sbyte Durability;
        public readonly sbyte MaxDurability;
        private readonly byte BoolField;

        public bool IsMaxProgress { 
            get => (BoolField & 0b1) == 1;
            //set => SetBoolBitFieldValue(value, 0);
        }
        public bool IsMaxQuality
        {
            get => (BoolField & 0b10) == 0b10;
            //set => SetBoolBitFieldValue(value, 1);
        }
        public bool IsMaxCP
        {
            get => (BoolField & 0b100) == 0b100;
            //set => SetBoolBitFieldValue(ref BoolField, value, 2);
        }
        public bool IsMaxDurability
        {
            get => (BoolField & 0b1000) == 0b1000;
            //set => SetBoolBitFieldValue(value, 3);
        }

        public readonly uint BitField1;

        private readonly byte[] _Actions;
        public byte[] Actions { get => _Actions; }
        private readonly ushort _ActionsLength;
        public ushort ActionsLength { get => _ActionsLength; }

        public readonly ushort Time;

        public readonly UInt128 Score;
        //iq
        //wastenot
        //ven
        //inno
        //greats
        //muscle
        //manip
        //observe
        //combo
        //condition

        //max 11, 4 bits, offset 0
        public byte InnerQuietStack
        {
            get => (byte)(BitField1 & 0b1111);
            //set => SetBitFieldValue(value, 0b1111U, 0);
        }

        //max 8, 4 bits, offset 4
        public byte WasteNotStack
        {
            get => (byte)((BitField1 & 0b11110000) >> 4);
            //set => SetBitFieldValue(value, 0b1111U, 4);
        }

        //max 5, 3 bits, offset 8
        public byte VenerationStack
        {
            get => (byte)((BitField1 & 0b11100000000) >> 8);
            //set => SetBitFieldValue(value, 0b111U, 8);
        }

        //max 5, 3 bits, offset 11
        public byte InnovationStack
        {
            get => (byte)((BitField1 & 0b11100000000000) >> 11);
            //set => SetBitFieldValue(value, 0b111U, 11);
        }

        //max 3, 2 bits, offset 14
        public byte GreatStridesStack
        {
            get => (byte)((BitField1 & 0b1100000000000000) >> 14);
            //set => SetBitFieldValue(value, 0b11U, 14);
        }

        //max 6, 3 bits, offset 16
        public byte MuscleMemoryStack
        {
            get => (byte)((BitField1 & 0b1110000000000000000) >> 16);
            //set => SetBitFieldValue(value, 0b111U, 16);
        }

        //max 8, 4 bits, offset 19
        public byte ManipulationStack
        {
            get => (byte)((BitField1 & 0b11110000000000000000000) >> 19);
            //set => SetBitFieldValue(value, 0b1111U, 19);
        }

        //max 1, 1 bits, offset 23
        public bool ObserveStack
        {
            get => ((BitField1 & 0b100000000000000000000000) == 0b100000000000000000000000);
            //set => SetBitFieldValue(value ? 0b1U : 0b0U, 0b1, 23);
        }

        //max 3, 2 bits, offset 24
        public byte AdvancedTouchCombo
        {
            get => (byte)((BitField1 & 0b11000000000000000000000000) >> 24);
            //set => SetBitFieldValue(value, 0b11U, 24);
        }

        //max 6, 3 bits, offset 26
        public RecipeCondition Condition
        {
            get => (RecipeCondition)((BitField1 & 0b11100000000000000000000000000) >> 26);
            //set => SetBitFieldValue((uint)value, 0b111U, 26);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        //private void SetBitFieldValue(uint value, uint bitMask, int bitShift)
        //{
        //    BitField1 = (BitField1 & ~(bitMask << bitShift)) | (value & bitMask) << bitShift;
        //}

        private void SetBoolBitFieldValue(ref byte bitfield, bool value, int bitShift)
        {
            bitfield = (byte)(BoolField & ~(1 << bitShift) | Convert.ToByte(value) << bitShift);
        }

        public static readonly byte[] BitOffsets = new byte[]
        {
            0, 4, 8, 11, 14, 16, 19, 23, 24, 26
        };

        public static readonly byte[] BitSizes = new byte[]
        {
            0b1111, 0b1111, 0b111, 0b111, 0b11, 0b111, 0b1111, 0b1, 0b11, 0b111
        };

        public static readonly uint[] ShiftedBitSizes = new uint[]
        {
            0b00000000000000000000000001111, 
            0b00000000000000000000011110000, 
            0b00000000000000000011100000000, 
            0b00000000000000011100000000000, 
            0b00000000000001100000000000000, 
            0b00000000001110000000000000000, 
            0b00000011110000000000000000000, 
            0b00000100000000000000000000000, 
            0b00011000000000000000000000000, 
            0b11100000000000000000000000000
        };
        
        private int ID { get; init; }

        //default constructor should return 'empty' state, therefore disposed
        public CESPacked()
        {
            ID = InstanceCount;
            _IsDisposed = true;
            Score = 0;
        }

        public CESPacked(CESPacked other, bool cloneActions)
        {
            ID = -1;
            _IsDisposed = false;
            _Actions = null;

            Step = other.Step;
            Progress = other.Progress;
            Quality = other.Quality;
            CPRemaining = other.CPRemaining;
            Durability = other.Durability;
            MaxDurability = other.MaxDurability;

            SetBoolBitFieldValue(ref BoolField, other.IsMaxProgress, 0);
            SetBoolBitFieldValue(ref BoolField, other.IsMaxQuality, 1);
            SetBoolBitFieldValue(ref BoolField, other.IsMaxCP, 2);
            SetBoolBitFieldValue(ref BoolField, other.IsMaxDurability, 3);

            BitField1 = other.BitField1;
            Time = other.Time;
            Score = other.Score;

            if (other._Actions != null && cloneActions)
            {
                _Actions = BAP.Rent(other._Actions.Length, ID);
                Array.Copy(other._Actions, _Actions, other._ActionsLength);
                _ActionsLength = other.ActionsLength;
            }
            else
                _ActionsLength = 0;
        }

        public static void Copy(CESPacked[] from, CESPacked[] to, int length)
        {
            for (int i = 0; i < length; i++)
            {
                to[i] = new(from[i], true);
            }
        }
        public static void Copy(CESPacked[] from, int fromIndex, CESPacked[] to, int toIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                to[i + toIndex] = new CESPacked(from[i + fromIndex], true);
            }
        }
        private static object InstanceCountLockObject = new object();
        private static int _InstanceCount = 0;
        public static int InstanceCount
        {
            get
            {
                //lock (InstanceCountLockObject) return _InstanceCount++;
                return -1;
            }
        }
        public CESPacked(CraftingEngine e)
        {
            ID = InstanceCount;

            if (_Actions != null)
                Debugger.Break();

            Step = (byte)e.Step;
            Progress = e.CurrentProgress;
            Quality = e.CurrentQuality;
            CPRemaining = (ushort)e.CurrentCP;
            Durability = (sbyte)e.CurrentDurability;
            MaxDurability = (sbyte)e.CurrentRecipe.Durability;

            int progress = e.CurrentProgress;
            if (progress > e.CurrentRecipe.MaxProgress)
                progress = e.CurrentRecipe.MaxProgress;

            int quality = e.CurrentQuality;
            if (quality > e.CurrentRecipe.MaxQuality)
                quality = e.CurrentRecipe.MaxQuality;

            int progressBits = e.CurrentRecipe.MaxProgress.SignificantBits() + 1;
            int qualityBits = e.CurrentRecipe.MaxQuality.SignificantBits() + 1;
            int stepBits = 40.SignificantBits() + 1;
            int timeBits = 400.SignificantBits() + 1;
            int cpBits = e.MaxCP.SignificantBits() + 1;
            int durabilityBits = (e.CurrentRecipe.Durability + 20).SignificantBits() + 1;
            int stacksBits = ShiftedBitSizes[^1].HighBitPosition() + 1;
            if (progressBits + qualityBits + cpBits + durabilityBits + stacksBits > 128)
                throw new Exception("Too many bits!");
            Score = 1;
            Score = (Score << progressBits) | (UInt128)progress;
            Score = (Score << qualityBits) | (UInt128)quality;
            Score = (Score << stepBits) | (UInt128)(40 - Step);
            Score = (Score << timeBits) | (UInt128)(1000 - Time);
            Score = (Score << cpBits) | (UInt128)CPRemaining;
            Score = (Score << durabilityBits) | (UInt128)(Durability + 20);
            Score = (Score << stacksBits) | (UInt128)BitField1;


            var actions = e.GetCraftingActions();

            _Actions = BAP.Rent(actions.Length, ID);

            Array.Copy(actions, _Actions, actions.Length);
            _ActionsLength = (ushort)actions.Length;

            SetBoolBitFieldValue(ref BoolField, Progress >= e.CurrentRecipe.MaxProgress, 0);
            SetBoolBitFieldValue(ref BoolField, Quality >= e.CurrentRecipe.MaxQuality, 1);
            SetBoolBitFieldValue(ref BoolField, CPRemaining >= e.MaxCP, 2);
            SetBoolBitFieldValue(ref BoolField, Durability >= e.CurrentRecipe.Durability, 3);



            Time = 0;
            for (int i = 0; i < actions.Length; i++)
                Time += (ushort)(CA.GetIsBuff(actions[i]) ? 2 : 3);


            BitField1 =
                    (uint)(e.InnerQuietStack & BitSizes[0] |
                    (e.WasteNotStack & BitSizes[1]) << BitOffsets[1] |
                    (e.VenerationStack & BitSizes[2]) << BitOffsets[2] |
                    (e.InnovationStack & BitSizes[3]) << BitOffsets[3] |
                    (e.GreatStridesStack & BitSizes[4]) << BitOffsets[4] |
                    (e.MuscleMemoryStack & BitSizes[5]) << BitOffsets[5] |
                    (e.ManipulationStack & BitSizes[6]) << BitOffsets[6] |
                    (e.ObserveStack & BitSizes[7]) << BitOffsets[7] |
                    (e.AdvancedTouchCombo & BitSizes[8]) << BitOffsets[8] |
                    ((byte)e.Condition & BitSizes[9]) << BitOffsets[9]);
        }

        public CESPacked(CraftingEngine e, Span<byte> actions)
        {
            ID = InstanceCount;

            if (_Actions != null)
                Debugger.Break();

            Step = (byte)e.Step;
            Progress = e.CurrentProgress;
            Quality = e.CurrentQuality;
            CPRemaining = (ushort)e.CurrentCP;
            Durability = (sbyte)e.CurrentDurability;
            MaxDurability = (sbyte)e.CurrentRecipe.Durability;

            int progress = e.CurrentProgress;
            if (progress > e.CurrentRecipe.MaxProgress)
                progress = e.CurrentRecipe.MaxProgress;

            int quality = e.CurrentQuality;
            if (quality > e.CurrentRecipe.MaxQuality)
                quality = e.CurrentRecipe.MaxQuality;

            int progressBits = e.CurrentRecipe.MaxProgress.SignificantBits() + 1;
            int qualityBits = e.CurrentRecipe.MaxQuality.SignificantBits() + 1;
            int stepBits = 40.SignificantBits() + 1;
            int timeBits = 400.SignificantBits() + 1;
            int cpBits = e.MaxCP.SignificantBits() + 1;
            int durabilityBits = (e.CurrentRecipe.Durability + 20).SignificantBits() + 1;
            int stacksBits = ShiftedBitSizes[^1].HighBitPosition() + 1;
            if (progressBits + qualityBits + cpBits + durabilityBits + stacksBits > 128)
                throw new Exception("Too many bits!");
            Score = 1;
            Score = (Score << progressBits) | (UInt128)progress;
            Score = (Score << qualityBits) | (UInt128)quality;
            Score = (Score << stepBits) | (UInt128)(40 - Step);
            Score = (Score << timeBits) | (UInt128)(1000 - Time);
            Score = (Score << cpBits) | (UInt128)CPRemaining;
            Score = (Score << durabilityBits) | (UInt128)(Durability + 20);
            Score = (Score << stacksBits) | (UInt128)BitField1;

            _Actions = BAP.Rent(actions.Length, ID);

            for (int i = 0; i < actions.Length; i++)
                _Actions[i] = actions[i];

            _ActionsLength = (ushort)actions.Length;

            SetBoolBitFieldValue(ref BoolField, Progress >= e.CurrentRecipe.MaxProgress, 0);
            SetBoolBitFieldValue(ref BoolField, Quality >= e.CurrentRecipe.MaxQuality, 1);
            SetBoolBitFieldValue(ref BoolField, CPRemaining >= e.MaxCP, 2);
            SetBoolBitFieldValue(ref BoolField, Durability >= e.CurrentRecipe.Durability, 3);

            Time = 0;
            for (int i = 0; i < actions.Length; i++)
                Time += (ushort)(CA.GetIsBuff(actions[i]) ? 2 : 3);

            BitField1 =
                (uint)(e.InnerQuietStack & BitSizes[0] |
                (e.WasteNotStack & BitSizes[1]) << BitOffsets[1] |
                (e.VenerationStack & BitSizes[2]) << BitOffsets[2] |
                (e.InnovationStack & BitSizes[3]) << BitOffsets[3] |
                (e.GreatStridesStack & BitSizes[4]) << BitOffsets[4] |
                (e.MuscleMemoryStack & BitSizes[5]) << BitOffsets[5] |
                (e.ManipulationStack & BitSizes[6]) << BitOffsets[6] |
                (e.ObserveStack & BitSizes[7]) << BitOffsets[7] |
                (e.AdvancedTouchCombo & BitSizes[8]) << BitOffsets[8] |
                ((byte)e.Condition & BitSizes[9]) << BitOffsets[9]);
        }

        public static CESPacked GetState(CraftingEngine e, byte action)
        {
            e.RemoveActions();
            e.AddAction(true, action);
            return new CESPacked(e);
        }

        public static CESPacked GetState(CraftingEngine e, Span<byte> actions)
        {
            e.RemoveActions();
            e.AddActions(true, actions);
            var hue = new CESPacked(e);
            return new CESPacked(e);
        }

        public static CESPacked[] GetStates(CraftingEngine e, byte[] actions, out int length)
        {
            CESPacked[] result = CESPackedArrayPool.Rent(actions.Length);
            int index = 0;
            for (int i = 0; i < actions.Length; i++)
            {
                e.RemoveActions();
                e.AddAction(true, actions[i]);
                if (e.Step == 0) 
                    continue;
                CESPacked state = new CESPacked(e);
                result[index++] = state;
            }
            length = index;
            return result;
        }

        public int GetAvailableActions(byte[] result)
        {
            if (Durability <= 0 || IsMaxProgress)
                return 0;

            int index = 0;
            //special case when manipulation is on at max durability

            bool mustUseDurabilityAction = ManipulationStack > 0 && IsMaxDurability;

            //as first action
            if (Step == 0)
            {
                result[0] = CraftingActions.Reflect;
                result[1] = CraftingActions.MuscleMemory;
                result[2] = CraftingActions.TrainedEye;
                index = 3;
            }

            //buffs
            if (ManipulationStack == 0 && IsEnoughCP(CraftingActions.Manipulation))
                result[index++] = CraftingActions.Manipulation;

            if (WasteNotStack == 0 && IsEnoughCP(CraftingActions.WasteNotII) && !mustUseDurabilityAction)
                result[index++] = CraftingActions.WasteNotII;
            
            if (WasteNotStack == 0 && IsEnoughCP(CraftingActions.WasteNot) && !mustUseDurabilityAction)
                result[index++] = CraftingActions.WasteNot;

            if (InnovationStack == 0 && IsEnoughCP(CraftingActions.Innovation) && !mustUseDurabilityAction)
                result[index++] = CraftingActions.Innovation;

            if (VenerationStack == 0  && IsEnoughCP(CraftingActions.Veneration) && !mustUseDurabilityAction)
                result[index++] = CraftingActions.Veneration;

            if (GreatStridesStack == 0 && IsEnoughCP(CraftingActions.GreatStrides) && !mustUseDurabilityAction)
                result[index++] = CraftingActions.GreatStrides;

            if (!ObserveStack && IsEnoughCP(CraftingActions.Observe) && !mustUseDurabilityAction)
                result[index++] = CraftingActions.Observe;

            if (IsEnoughCP(CraftingActions.MastersMend)) {
                if (MaxDurability - Durability >= 30 && ManipulationStack == 0)
                    result[index++] = CraftingActions.MastersMend;
                if (MaxDurability - Durability >= 35 && ManipulationStack > 0)
                    result[index++] = CraftingActions.MastersMend;
            }
            //progress or quality first?

            if (!IsMaxProgress  && !IsMaxQuality && IsEnoughCP(CraftingActions.DelicateSynthesis))
                result[index++] = CraftingActions.DelicateSynthesis;

            if (!IsMaxQuality)
            {
                if (ObserveStack && IsEnoughCP(CraftingActions.FocusedTouch))
                    result[index++] = CraftingActions.FocusedTouch;

                if (WasteNotStack == 0 && IsEnoughCP(CraftingActions.PrudentTouch))
                    result[index++] = CraftingActions.PrudentTouch;

                if (IsEnoughCP(CraftingActions.PreparatoryTouch))
                    result[index++] = CraftingActions.PreparatoryTouch;

                if (InnerQuietStack > 0 && IsEnoughCP(CraftingActions.ByregotsBlessing))
                    result[index++] = CraftingActions.ByregotsBlessing;

                if (InnerQuietStack == 10 && IsEnoughCP(CraftingActions.TrainedFinesse))
                    result[index++] = CraftingActions.TrainedFinesse;

                if (IsEnoughCP(CraftingActions.BasicTouch))
                    switch (AdvancedTouchCombo)
                    {
                        case 0:
                            result[index++] = CraftingActions.BasicTouch;
                            break;

                        case 1:
                            result[index++] = CraftingActions.StandardTouch;
                            break;

                        case 2:
                            result[index++] = CraftingActions.AdvancedTouch;
                            break;
                    }
            }

            if (!IsMaxProgress)
            {
                if (ObserveStack && IsEnoughCP(CraftingActions.FocusedSynthesis))
                    result[index++] = CraftingActions.FocusedSynthesis;

                if (WasteNotStack == 0 && IsEnoughCP(CraftingActions.PrudentSynthesis))
                    result[index++] = CraftingActions.PrudentSynthesis;

                if ((Durability >= 20 || (WasteNotStack > 0 && Durability >= 10)) && IsEnoughCP(CraftingActions.Groundwork))
                    result[index++] = CraftingActions.Groundwork;

                if (IsEnoughCP(CraftingActions.CarefulSynthesis))
                    result[index++] = CraftingActions.CarefulSynthesis;

                if (IsEnoughCP(CraftingActions.BasicSynthesis))
                    result[index++] = CraftingActions.BasicSynthesis;
            }

                return index;
        }

        private bool IsEnoughCP(byte id)
        {
            return CA.GetCPCost(id) <= CPRemaining;
        }

        public int FillStates(CraftingEngine e, CESPacked[] resultBuffer, byte[] actionsBuffer)
        {
            if (resultBuffer.Length < CESCollection.MAX_ACTIONS || actionsBuffer.Length < CESCollection.MAX_ACTIONS)
                throw new Exception();
            if (_Actions == null)
            {
                throw new Exception();
                return 0;
            }

            if (_Actions[0] == 0 && _ActionsLength > 0)
                Debugger.Break();

            if (IsDisposed) throw new Exception("Whatcha doing?");
            int actionsLength = GetAvailableActions(actionsBuffer);
            if (actionsLength == 0)
            {
                return 0;
            }
            Span<byte> newActions = stackalloc byte[_ActionsLength + 1];

            int index = 0;
            for (int i = 0; i < actionsLength; i++) 
            {
                e.RemoveActions(true);
                e.SetState(this);
                e.AddAction(false, actionsBuffer[i]);
                e.ExecuteActions(false);

                if (e.Step < newActions.Length)
                    continue;

                _Actions.CopyToSpan(newActions, _ActionsLength);
                newActions[_ActionsLength] = actionsBuffer[i];

                var state = new CESPacked(e, newActions);

                if (!state.IsWorse(this))
                {
                    resultBuffer[index++] = state;
                }
                else
                    state.Dispose();
            }

            return index;
        }

        public bool IsWorse(CESPacked o)
        {
            return AtLeastOneWorse(o) && AllWorseOrEqual(o);
        }
        public bool IsBetter(CESPacked o)
        {
            return AtLeastOneBetter(o) && AllBetterOrEqual(o);
        }

        public bool AtLeastOneWorse(CESPacked o)
        {
            return
                Progress < o.Progress ||
                Quality < o.Quality ||
                CPRemaining < o.CPRemaining ||
                Durability < o.Durability ||
                AtLeastOneBitFieldWorse(o);
        }

        public bool AllBetter(CESPacked o)
        {
            return
                Progress > o.Progress &&
                Quality > o.Quality &&
                CPRemaining > o.CPRemaining &&
                Durability > o.Durability &&
                AllBitFieldsBetter(o);
        }

        public bool AllWorse(CESPacked o)
        {
            return
                Progress < o.Progress &&
                Quality < o.Quality &&
                CPRemaining < o.CPRemaining &&
                Durability < o.Durability &&
                AllBitFieldsWorse(o);
        }

        public bool AllBetterOrEqual(CESPacked o)
        {
            return
                Progress >= o.Progress &&
                Quality >= o.Quality &&
                CPRemaining >= o.CPRemaining &&
                Durability >= o.Durability &&
                AllBitFieldsBetterOrEqual(o);
        }

        public bool AllWorseOrEqual(CESPacked o)
        {
            return
                Progress <= o.Progress &&
                Quality <= o.Quality &&
                CPRemaining <= o.CPRemaining &&
                Durability <= o.Durability &&
                AllBitFieldsWorseOrEqual(o);
        }

        public bool AtLeastOneBetter(CESPacked o)
        {
            return
                Progress > o.Progress ||
                Quality > o.Quality ||
                CPRemaining > o.CPRemaining ||
                Durability > o.Durability ||
                AtLeastOneBitFieldBetter(o);
        }

        public bool AtLeastOneBitFieldWorse(CESPacked o)
        {
           return (BitField1 & ShiftedBitSizes[0]) < (o.BitField1 & ShiftedBitSizes[0]) ||
                (BitField1 & ShiftedBitSizes[1]) < (o.BitField1 & ShiftedBitSizes[1]) ||
                (BitField1 & ShiftedBitSizes[2]) < (o.BitField1 & ShiftedBitSizes[2]) ||
                (BitField1 & ShiftedBitSizes[3]) < (o.BitField1 & ShiftedBitSizes[3]) ||
                (BitField1 & ShiftedBitSizes[4]) < (o.BitField1 & ShiftedBitSizes[4]) ||
                (BitField1 & ShiftedBitSizes[5]) < (o.BitField1 & ShiftedBitSizes[5]) ||
                (BitField1 & ShiftedBitSizes[6]) < (o.BitField1 & ShiftedBitSizes[6]) ||
                (BitField1 & ShiftedBitSizes[7]) < (o.BitField1 & ShiftedBitSizes[7]) ||
                (BitField1 & ShiftedBitSizes[8]) < (o.BitField1 & ShiftedBitSizes[8]);
        }

        public bool AtLeastOneBitFieldBetter(CESPacked o)
        {
            return (BitField1 & ShiftedBitSizes[0]) > (o.BitField1 & ShiftedBitSizes[0]) ||
                 (BitField1 & ShiftedBitSizes[1]) > (o.BitField1 & ShiftedBitSizes[1]) ||
                 (BitField1 & ShiftedBitSizes[2]) > (o.BitField1 & ShiftedBitSizes[2]) ||
                 (BitField1 & ShiftedBitSizes[3]) > (o.BitField1 & ShiftedBitSizes[3]) ||
                 (BitField1 & ShiftedBitSizes[4]) > (o.BitField1 & ShiftedBitSizes[4]) ||
                 (BitField1 & ShiftedBitSizes[5]) > (o.BitField1 & ShiftedBitSizes[5]) ||
                 (BitField1 & ShiftedBitSizes[6]) > (o.BitField1 & ShiftedBitSizes[6]) ||
                 (BitField1 & ShiftedBitSizes[7]) > (o.BitField1 & ShiftedBitSizes[7]) ||
                 (BitField1 & ShiftedBitSizes[8]) > (o.BitField1 & ShiftedBitSizes[8]);
        }
        public bool AllBitFieldsWorse(CESPacked o)
        {
            return (BitField1 & ShiftedBitSizes[0]) < (o.BitField1 & ShiftedBitSizes[0]) &&
                 (BitField1 & ShiftedBitSizes[1]) < (o.BitField1 & ShiftedBitSizes[1]) &&
                 (BitField1 & ShiftedBitSizes[2]) < (o.BitField1 & ShiftedBitSizes[2]) &&
                 (BitField1 & ShiftedBitSizes[3]) < (o.BitField1 & ShiftedBitSizes[3]) &&
                 (BitField1 & ShiftedBitSizes[4]) < (o.BitField1 & ShiftedBitSizes[4]) &&
                 (BitField1 & ShiftedBitSizes[5]) < (o.BitField1 & ShiftedBitSizes[5]) &&
                 (BitField1 & ShiftedBitSizes[6]) < (o.BitField1 & ShiftedBitSizes[6]) &&
                 (BitField1 & ShiftedBitSizes[7]) < (o.BitField1 & ShiftedBitSizes[7]) &&
                 (BitField1 & ShiftedBitSizes[8]) < (o.BitField1 & ShiftedBitSizes[8]);
        }

        public bool AllBitFieldsBetter(CESPacked o)
        {
            return (BitField1 & ShiftedBitSizes[0]) > (o.BitField1 & ShiftedBitSizes[0]) &&
                 (BitField1 & ShiftedBitSizes[1]) > (o.BitField1 & ShiftedBitSizes[1]) &&
                 (BitField1 & ShiftedBitSizes[2]) > (o.BitField1 & ShiftedBitSizes[2]) &&
                 (BitField1 & ShiftedBitSizes[3]) > (o.BitField1 & ShiftedBitSizes[3]) &&
                 (BitField1 & ShiftedBitSizes[4]) > (o.BitField1 & ShiftedBitSizes[4]) &&
                 (BitField1 & ShiftedBitSizes[5]) > (o.BitField1 & ShiftedBitSizes[5]) &&
                 (BitField1 & ShiftedBitSizes[6]) > (o.BitField1 & ShiftedBitSizes[6]) &&
                 (BitField1 & ShiftedBitSizes[7]) > (o.BitField1 & ShiftedBitSizes[7]) &&
                 (BitField1 & ShiftedBitSizes[8]) > (o.BitField1 & ShiftedBitSizes[8]);
        }

        public bool AllBitFieldsWorseOrEqual(CESPacked o)
        {
            return (BitField1 & ShiftedBitSizes[0]) <= (o.BitField1 & ShiftedBitSizes[0]) &&
                 (BitField1 & ShiftedBitSizes[1]) <= (o.BitField1 & ShiftedBitSizes[1]) &&
                 (BitField1 & ShiftedBitSizes[2]) <= (o.BitField1 & ShiftedBitSizes[2]) &&
                 (BitField1 & ShiftedBitSizes[3]) <= (o.BitField1 & ShiftedBitSizes[3]) &&
                 (BitField1 & ShiftedBitSizes[4]) <= (o.BitField1 & ShiftedBitSizes[4]) &&
                 (BitField1 & ShiftedBitSizes[5]) <= (o.BitField1 & ShiftedBitSizes[5]) &&
                 (BitField1 & ShiftedBitSizes[6]) <= (o.BitField1 & ShiftedBitSizes[6]) &&
                 (BitField1 & ShiftedBitSizes[7]) <= (o.BitField1 & ShiftedBitSizes[7]) &&
                 (BitField1 & ShiftedBitSizes[8]) <= (o.BitField1 & ShiftedBitSizes[8]);
        }

        public bool AllBitFieldsBetterOrEqual(CESPacked o)
        {
            return (BitField1 & ShiftedBitSizes[0]) >= (o.BitField1 & ShiftedBitSizes[0]) &&
                 (BitField1 & ShiftedBitSizes[1]) >= (o.BitField1 & ShiftedBitSizes[1]) &&
                 (BitField1 & ShiftedBitSizes[2]) >= (o.BitField1 & ShiftedBitSizes[2]) &&
                 (BitField1 & ShiftedBitSizes[3]) >= (o.BitField1 & ShiftedBitSizes[3]) &&
                 (BitField1 & ShiftedBitSizes[4]) >= (o.BitField1 & ShiftedBitSizes[4]) &&
                 (BitField1 & ShiftedBitSizes[5]) >= (o.BitField1 & ShiftedBitSizes[5]) &&
                 (BitField1 & ShiftedBitSizes[6]) >= (o.BitField1 & ShiftedBitSizes[6]) &&
                 (BitField1 & ShiftedBitSizes[7]) >= (o.BitField1 & ShiftedBitSizes[7]) &&
                 (BitField1 & ShiftedBitSizes[8]) >= (o.BitField1 & ShiftedBitSizes[8]);
        }

        public bool IsEqualTo(CESPacked o)
        {
            return
                BitField1 == o.BitField1 &&
                Progress == o.Progress &&
                Quality == o.Quality &&
                CPRemaining == o.CPRemaining &&
                Durability == o.Durability;
        }

        public bool IsNotEqualTo(CESPacked o)
        {
            return
                BitField1 != o.BitField1 ||
                Progress != o.Progress ||
                Quality != o.Quality ||
                CPRemaining != o.CPRemaining ||
                Durability != o.Durability;
        }

       

        public override int GetHashCode()
        {
            int hash = 0b101010;
            hash = (hash * 31) ^ (Step << 8);
            hash ^= 0b101010;
            hash += hash * 31 ^ Progress;
            hash += hash * 31 ^ Quality;
            hash += hash * 31 ^ CPRemaining;
            hash += hash * 31 ^ Durability;
            hash += hash * 31 ^ BitField1.GetHashCode();
            return hash;
        }

        public bool Equals(CESPacked other)
        {
            return IsEqualTo(other);
        }

        private readonly bool _IsDisposed;
        public bool IsDisposed { get => _IsDisposed; }
        public void Dispose()
        {

            return;
            if (_IsDisposed) return;
            
            if (_Actions != null)
            BAP.Return(_Actions, ID);
            //_Actions = null;
            //_ActionsLength = 0;
            //Score = 0;
            //IsDisposed = true;
        }

        public int CompareTo(CESPacked other)
        {
            if (Score > other.Score) return 1;
            if (Score < other.Score) return -1;
            return 0;
        }
    }

    public class CraftingEngineState : IEquatable<CraftingEngineState>
    {
        public byte Step;
        public int Progress;
        public int Quality;
        public ushort CP;
        public byte Durability;

        public RecipeCondition Condition;

        public byte InnerQuietStack;
        public byte WasteNotStack;
        public byte VenerationStack;
        public byte InnovationStack;
        public byte GreatStridesStack;
        public byte MuscleMemoryStack;
        public byte ManipulationStack;
        public bool ObserveStack;

        public byte AdvancedTouchCombo;

        public CraftingEngineState Clone()
        {
            return new CraftingEngineState
            {
                Step = Step,
                Progress = Progress,
                Quality = Quality,
                CP = CP,
                Durability = Durability,
                Condition = Condition,
                InnerQuietStack = InnerQuietStack,
                WasteNotStack = WasteNotStack,
                VenerationStack = VenerationStack,
                GreatStridesStack = GreatStridesStack,
                InnovationStack = InnovationStack,
                MuscleMemoryStack = MuscleMemoryStack,
                ManipulationStack = ManipulationStack,
                ObserveStack = ObserveStack,
                AdvancedTouchCombo = AdvancedTouchCombo,
            };
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash ^= Step * 7;
            hash ^= Progress * 7;
            hash ^= Quality * 7;
            hash ^= Durability * 7;
            hash ^= (int)Condition * 7;
            hash ^= InnerQuietStack.GetHashCode() * 13;
            hash ^= WasteNotStack.GetHashCode() * 13;
            hash ^= VenerationStack.GetHashCode() * 13;
            hash ^= GreatStridesStack.GetHashCode() * 13;
            hash ^= InnovationStack.GetHashCode() * 13;
            hash ^= MuscleMemoryStack.GetHashCode() * 13;
            hash ^= ManipulationStack.GetHashCode() * 13;
            hash ^= ObserveStack.GetHashCode() * 13;
            hash ^= AdvancedTouchCombo * 13;
            return hash;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CraftingEngineState);
        }

        public bool Equals(CraftingEngineState other)
        {
            if (other is null) return false;

            return Step == other.Step &&
                Progress == other.Progress &&
                Quality == other.Quality &&
                CP == other.CP &&
                Condition == other.Condition &&
                Durability == other.Durability &&
                InnerQuietStack == other.InnerQuietStack &&
                WasteNotStack == other.WasteNotStack &&
                VenerationStack == other.VenerationStack &&
                GreatStridesStack == other.GreatStridesStack &&
                InnovationStack == other.InnovationStack &&
                MuscleMemoryStack == other.MuscleMemoryStack &&
                ManipulationStack == other.ManipulationStack &&
                ObserveStack == other.ObserveStack &&
                AdvancedTouchCombo == other.AdvancedTouchCombo;
        }

        public bool NotEquals(CraftingEngineState other)
        {
            if (other is null) return true;

            return Step != other.Step ||
                Progress != other.Progress ||
                Quality != other.Quality ||
                CP != other.CP ||
                Condition != other.Condition ||
                Durability != other.Durability ||
                InnerQuietStack != other.InnerQuietStack ||
                WasteNotStack != other.WasteNotStack ||
                VenerationStack != other.VenerationStack ||
                GreatStridesStack != other.GreatStridesStack ||
                InnovationStack != other.InnovationStack ||
                MuscleMemoryStack != other.MuscleMemoryStack ||
                ManipulationStack != other.ManipulationStack ||
                ObserveStack != other.ObserveStack ||
                AdvancedTouchCombo != other.AdvancedTouchCombo;
        }

        public static bool operator ==(CraftingEngineState left, CraftingEngineState right)
        {
            if (left is null && right is null) return true;
            if ((left is not null && right is null) || (left is null && right is not null)) return false;

            return left.Equals(right);
        }

        public static bool operator !=(CraftingEngineState left, CraftingEngineState right)
        {
            if (left is null && right is null) return false;
            if ((left is not null && right is null) || (left is null && right is not null)) return true;

            return left.NotEquals(right);
        }


        public override string ToString()
        {
            return base.ToString() + $"Step:{Step} P:{Progress} Q:{Quality} SP:{CP} C:{Condition} D:{Durability} ATC:{AdvancedTouchCombo}";
        }
    }
}
