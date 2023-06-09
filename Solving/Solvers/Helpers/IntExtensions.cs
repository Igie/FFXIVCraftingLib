﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVCraftingLib.Solving.Solvers.Helpers
{
    public static class IntExtensions
    {
        internal static readonly byte[] msbPos256 = new byte[] {
        255, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7};

        public static int SignificantBits(this int value)
        {
            return HighBitPosition((uint)value) + 1;
        }

        public static int HighBitPosition(this ushort value)
        {
            byte hiByte = (byte)(value >> 8);
            if (hiByte != 0) return 8 + msbPos256[hiByte];
            return (sbyte)msbPos256[(byte)value];
        }
        public static int HighBitPosition(this uint value)
        {
            byte hiByte = (byte)(value >> 24);
            if (hiByte != 0) return 24 + msbPos256[hiByte];
            hiByte = (byte)(value >> 16);
            return (hiByte != 0) ? 16 + msbPos256[hiByte] : HighBitPosition((ushort)value);
        }

        public static int NextBiggest10(this int value)
        {
            int result = 10;
            while(value > 0) {
                result *= 10;
                value /= 10;
            }
            return result;
        }
    }
}
