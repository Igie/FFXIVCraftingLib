using FFXIVCraftingLib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVCraftingLib.Solving.Solvers.DPTypes
{
    public class DPStackItem : IDisposable
    {
        public CESCollection States;
        public bool RemoveWorse;
        public int CurrentLevel;

        public bool Completed;
        public (CESPacked[], int) Result;

        public DPStackItem(CESCollection states, bool removeWorse, int currentLevel = 0) 
        {
            States = states;
            RemoveWorse = removeWorse;
            CurrentLevel = currentLevel;
            Completed = false;
        }

        public void Dispose()
        {
            //States.Dispose();
            States = null;
            Result.Item1 = null;
            Result.Item2 = 0;
            //Result = null;
        }
    }
}
