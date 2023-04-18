using FFXIVCraftingLib.Actions;
using System.Diagnostics;

namespace FFXIVCraftingLib.Solving.GeneticAlgorithm
{
    public class Chromosome : IComparable<Chromosome>, IEquatable<Chromosome>
    {
        public CraftingEngine Engine { get; private set; }
        public Population Population { get; private set; }
        public byte[] Values { get; set; }
        public byte[] UsableValues { get; set; }
        public byte[] AvailableValues { get; set; }
        public ulong Fitness { get; set; }

        public int Hash { get; private set; }

        public int Size { get; private set; }

        public Chromosome(CraftingEngine engine, byte[] availableValues, int valueCount, byte[] values)
        {
            Engine = engine;
            AvailableValues = availableValues;
            Values = new byte[valueCount];
            values.CopyTo(Values, 0);
            Fitness = Evaluate();
        }

        public Chromosome(CraftingEngine engine, byte[] availableValues, int valueCount)
        {
            Engine = engine;
            AvailableValues = availableValues;
            Values = new byte[valueCount];


            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = AvailableValues.GetRandom();
            }
            Fitness = Evaluate();
        }

        public Chromosome Clone()
        {
            return new Chromosome(Engine, AvailableValues, Values.Length, Values);
        }

        public ulong Evaluate()
        {
            Engine.RemoveActions();
            UsableValues = Values.Where(x => x > 0).ToArray();
            try
            {
                Engine.AddActions(true, UsableValues);
            }
            catch (Exception e)
            {
                Debugger.Break();
            }

            Size = Engine.CraftingActionsLength;
            UsableValues = UsableValues.Take(Size).ToArray();
            Hash = GetHashCode();
            return Engine.Score;
        }

        public int CompareTo(Chromosome other)
        {
            if (this == other)
                return 0;
            if (this == null && other == null)
                return 0;
            if (this != null && other == null)
                return -1;
            if (this == null && other != null)
                return 1;

            if (Fitness > other.Fitness)
                return -1;
            if (Fitness == other.Fitness)
                return 0;
            if (Fitness < other.Fitness)
                return 1;
            return 0;
        }

        public override string ToString()
        {
            return $"Chromosome {Fitness}";
        }

        public override int GetHashCode()
        {
            int result = 7;
            for (int i = 0; i < UsableValues.Length; i++)
            {
                result ^= UsableValues[i];
                result *= 29;
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            Chromosome other = obj as Chromosome;
            return Equals(other);
        }

        public bool Equals(Chromosome other)
        {
            if (other is null)
                return false;
            if (Fitness != other.Fitness)
                return false;


            if (UsableValues.Length != other.UsableValues.Length)
                return false;

            if (Hash == other.Hash)
                return true;

            if (Engine.CurrentProgress == other.Engine.CurrentProgress &&
                Engine.CurrentQuality == other.Engine.CurrentQuality &&
                Engine.CurrentDurability == other.Engine.CurrentDurability &&
                Engine .CurrentCP == other.Engine.CurrentCP &&
                Engine.CraftingActionsLength == other.Engine.CraftingActionsLength &&
                Engine.Score == other.Engine.Score)
                return true;

            return Hash == other.Hash;
            bool eq = true;
            for (int i = 0; i < UsableValues.Length; i++)
                if (UsableValues[i] != other.UsableValues[i])
                {
                    eq = false;
                    break;
                }

            if (eq && Hash != other.Hash)
                Debugger.Break();
            return true;
        }

        public static bool operator ==(Chromosome left, Chromosome right)
        {
            if (left is null)
                if (right is null)
                    return true;
                else
                    return right.Equals(left);
            return left.Equals(right);
        }

        public static bool operator !=(Chromosome left, Chromosome right)
        {
            if (left is null)
                if (right is null)
                    return false;
                else
                    return !right.Equals(left);

            return !left.Equals(right);
        }
    }
}
