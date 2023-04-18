using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVCraftingLib.Solving.Solvers.Helpers
{
    public static class ByteArrayExtensions
    {
        public static void CopyToSpan(this byte[] array, Span<byte> to, int length)
        {
            for (int i = 0; i < length; i++)
            {
                to[i] = array[i];
            }
        }
    }
}
