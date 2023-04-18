using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVCraftingLib
{
    public class SRandom
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> Random =
            new(() => new Random(Interlocked.Increment(ref seed)));



        public static double NextDouble()
        {
            return Random.Value.NextDouble();
        }

        public static int Next()
        {
            return Random.Value.Next();
        }

        public static int Next(int maxValue)
        {
            return Random.Value.Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            return Random.Value.Next(minValue, maxValue);
        }

        public static T GetRandom<T>(T[] array)
        {
            return array[Next(array.Length)];
        }

        public static bool GetChance(double probability)
        {
            return Random.Value.NextDouble() < probability;
        }
    }

    public static class RandomExtensions
    {
        public static T GetRandom<T>(this T[] array)
        {
            return array[SRandom.Next(array.Length)];
        }
    }
}
