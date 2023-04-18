using FFXIVCraftingLib.Actions;
using FFXIVCraftingLib.Solving.Solvers.Helpers;
using FFXIVCraftingLib.Types;
using FFXIVDataManager;
using FFXIVDataManager.GameData;
using System;
using System.Diagnostics;

using CA = FFXIVCraftingLib.Actions.CraftingAction;

namespace FFXIVCraftingLib
{
    public sealed class CraftingEngine : ICloneable
    {
        public int ActionCount = 40;
        private int level;

        public int Level
        {
            get => level;
            set
            {
                if (level == value) return;
                ActualLevel = Utils.GetPlayerLevel(value);
                level = value;
                //if (CurrentRecipe != null)
                //    LevelDifference = LevelDifferences.GetCraftingLevelDifference(ActualLevel - CurrentRecipe.Level);
                ExecuteActions();
            }
        }

        private int _BaseCraftsmanship;
        private int _BaseControl;
        private int _BaseMaxCP;


        public int BaseCraftsmanship
        {
            get => _BaseCraftsmanship;
            set
            {
                if (_BaseCraftsmanship == value) return;
                _BaseCraftsmanship = value;
                ExecuteActions();
            }
        }

        public int BaseControl
        {
            get => _BaseControl;
            set
            {
                if (_BaseControl == value) return;
                _BaseControl = value;
                ExecuteActions();
            }
        }

        public int BaseMaxCP
        {
            get => _BaseMaxCP;
            set
            {
                if (_BaseMaxCP == value) return;
                _BaseMaxCP = value;
                ExecuteActions();
            }
        }

        public int ActualLevel { get; private set; }
        public int Craftsmanship
        {
            get => BaseCraftsmanship + CraftsmanshipBuff;
            set => BaseCraftsmanship = value - CraftsmanshipBuff;
        }
        public int Control
        {

            get => BaseControl + ControlBuff;
            set => BaseControl = value - ControlBuff;
        }
        public double ActualControl { get; private set; }
        public int MaxCP
        {
            get => _BaseMaxCP + MaxCPBuff;
            set => BaseMaxCP = value - MaxCPBuff;
        }

        public int CraftsmanshipBuff { get; set; }

        public int ControlBuff { get; set; }

        public int MaxCPBuff { get; set; }

        public int Step { get; private set; }

        public int CurrentDurability { get; set; }

        public int CurrentProgress { get; set; }
        public int CurrentCollectability => CurrentQuality / 10;

        private int _CurrentQuality;

        public int CurrentQuality
        {
            get => _CurrentQuality + StartingQuality;
            set => _CurrentQuality = value - StartingQuality;
        }
        public int StartingQuality { get; set; }
        public int CurrentCP { get; set; }

        public CraftingSimStepSettings[] StepSettings { get; private set; }

        private RecipeInfo _CurrentRecipe { get; set; }

        public RecipeInfo CurrentRecipe
        {
            get => _CurrentRecipe;
            private set
            {
                if (_CurrentRecipe == value) return;
                _CurrentRecipe = value;
                ExecuteActions();

            }
        }

        public byte[] CraftingActions { get; private set; }
        public int InnerQuietStack { get; set; }
        public int WasteNotStack { get; set; }
        public int VenerationStack { get; set; }
        public int GreatStridesStack { get; set; }
        public int InnovationStack { get; set; }
        public int MuscleMemoryStack { get; set; }
        public int ManipulationStack { get; set; }
        public int ObserveStack { get; set; }

        private bool InnerQuietRemove = false;
        private bool GreatStridesRemove = false;
        private bool MuscleMemoryRemove = false;

        public int AdvancedTouchCombo { get; set; }

        private int _CraftingActionsLength;
        public int CraftingActionsLength
        {
            get => _CraftingActionsLength;
            private set
            {
                _CraftingActionsLength = value;
                if (value > ActionCount)
                    Debugger.Break();
            }
        }

        public int CraftingActionsTime
        {
            get
            {
                int time = 0;
                for (int i = 0; i < CraftingActionsLength; i++)
                    time += CA.GetIsBuff(CraftingActions[i]) ? 2 : 3;
                return time;
            }
        }


        public ScoreDelegate ScoreFunction { get; set; }

        private LevelDifferencesInfo LevelDifferences { get; set; }

        public CraftingEngine()
        {
            LevelDifferences = DataLoader.GetFile<LevelDifferencesInfo>();
            CraftingActions = new byte[ActionCount];
            CraftingActionsLength = 0;
            StepSettings = new CraftingSimStepSettings[ActionCount];
            for (int i = 0; i < ActionCount; i++)
                StepSettings[i] = new CraftingSimStepSettings();
        }

        public bool Solved => CurrentRecipe != null && CurrentProgress >= CurrentRecipe.MaxProgress && CurrentQuality >= CurrentRecipe.MaxQuality;

        public CraftingEngine Clone(bool copyActions = false)
        {
            CraftingEngine result = new CraftingEngine();
            result.SetRecipe(CurrentRecipe);
            CopyTo(result, copyActions);
            return result;
        }

        public object Clone()
        {
            return Clone(false);
        }

        public void CopyTo(CraftingEngine sim, bool copyActions = false)
        {
            sim.Level = Level;
            sim.BaseCraftsmanship = BaseCraftsmanship;
            sim.BaseControl = BaseControl;
            sim.BaseMaxCP = BaseMaxCP;
            sim.CraftsmanshipBuff = CraftsmanshipBuff;
            sim.ControlBuff = ControlBuff;
            sim.MaxCPBuff = MaxCPBuff;
            sim.StartingQuality = StartingQuality;
            sim.StepSettings = new CraftingSimStepSettings[ActionCount];

            for (int i = 0; i < ActionCount; i++)
                sim.StepSettings[i] = StepSettings[i].Clone();

            if (copyActions)
            {
                sim.RemoveActions();
                sim.AddActions(false, GetCraftingActions());

                sim.Step = Step;
                sim.CurrentDurability = CurrentDurability;
                sim.CurrentProgress = CurrentProgress;
                sim.CurrentQuality = CurrentQuality;
                sim.CurrentCP = CurrentCP;

                sim.InnerQuietStack = InnerQuietStack;
                sim.WasteNotStack = WasteNotStack;
                sim.VenerationStack = VenerationStack;
                sim.GreatStridesStack = GreatStridesStack;
                sim.InnovationStack = InnovationStack;
                sim.MuscleMemoryStack = MuscleMemoryStack;
                sim.ManipulationStack = ManipulationStack;
                sim.ObserveStack = ObserveStack;
                sim.CraftingActionsLength = CraftingActionsLength;
            }
            else
                sim.CraftingActionsLength = 0;
        }
        public void AddAction(bool execute, CraftingActionID action)
        {
            AddAction(execute, (byte)action);
        }

        public void AddAction(bool execute, byte action)
        {
            if (CraftingActionsLength >= ActionCount)
                return;

            if (CA.GetLevel(action) <= Level)
            {
                CraftingActions[CraftingActionsLength] = action;
                CraftingActionsLength++;
            }

            if (CraftingActionsLength > ActionCount)
                Debugger.Break();
            if (execute)
                ExecuteActions();
        }
        public void AddActions(bool execute, params CraftingActionID[] actions)
        {
            Span<byte> acs = stackalloc byte[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                acs[i] = (byte)actions[i];
            }
            AddActions(execute, acs);
        }

        public void AddByteActions(bool execute, params byte[] actions)
        {
            if (CraftingActionsLength >= ActionCount)
                return;
            for (int i = 0; i < actions.Length; i++)
            {
                if (CA.GetLevel(actions[i]) <= Level)
                {
                    CraftingActions[CraftingActionsLength] = actions[i];
                    CraftingActionsLength++;
                }
            }
            if (CraftingActionsLength > ActionCount)
                Debugger.Break();
            if (execute)
                ExecuteActions();
        }

        public void AddActions(bool execute, Span<byte> actions)
        {
            if (CraftingActionsLength >= ActionCount)
                return;
            for (int i = 0; i < actions.Length; i++)
            {
                if (CA.GetLevel(actions[i]) <= Level)
                {
                    CraftingActions[CraftingActionsLength] = actions[i];
                    CraftingActionsLength++;
                }
            }
            if (CraftingActionsLength > ActionCount)
                Debugger.Break();
            if (execute)
                ExecuteActions();
        }

        public void RemoveActionAt(int index)
        {
            if (index >= CraftingActionsLength || index < 0)
                throw new IndexOutOfRangeException();

            for (int i = index; i < CraftingActionsLength - 1; i++)
            {
                CraftingActions[i] = CraftingActions[i + 1];
            }

            CraftingActions[CraftingActionsLength - 1] = 0;
            CraftingActionsLength--;
            ExecuteActions();
        }

        public void RemoveActions(bool reset = true)
        {
            if (CraftingActionsLength > ActionCount)
                Debugger.Break();
            for (int i = 0; i < CraftingActionsLength; i++)
                CraftingActions[i] = 0;
            CraftingActionsLength = 0;
            if (reset)
                ExecuteActions();
        }

        public void RemoveRedundantActions()
        {
            for (int i = Step; i < CraftingActionsLength; i++)
                CraftingActions[i] = 0;
            CraftingActionsLength = Step;
        }

        public byte[] GetCraftingActions()
        {
            byte[] result = new byte[CraftingActionsLength];
            Array.Copy(CraftingActions, result, CraftingActionsLength);
            return result;
        }
        //InnerQuiet,
        //WasteNot,
        //SteadyHand,
        //GreatStrides,
        //Ingenuity,
        //IngenuityII,
        //WasteNotII,
        //Manipulation,
        //Innovation,
        //Reclaim,
        //ComfortZone,
        //SteadyHandII,
        //NameoftheElements,
        //Nameless,
        //MakersMark,
        //CraftersSoul,
        //Whistle,
        //CollectableSynthesis,
        //StrokeofGenius,
        //InitialPreparations,
        //Reusing,
        //FinalAppraisal,
        //MuscleMemory,
        //Veneration,
        //HeartandSoul,
        public StatusStacksInfo[] GetCraftingStatuses()
        {
            List<StatusStacksInfo> result = new List<StatusStacksInfo>();
            if (InnerQuietStack > 0) result.Add(new(StatusIndex.InnerQuiet, InnerQuietStack));
            if (MuscleMemoryStack > 0) result.Add(new(StatusIndex.MuscleMemory, MuscleMemoryStack));
            if (WasteNotStack > 0) result.Add(new(StatusIndex.WasteNot, WasteNotStack));
            if (GreatStridesStack > 0) result.Add(new(StatusIndex.GreatStrides, GreatStridesStack));
            if (ManipulationStack > 0) result.Add(new(StatusIndex.Manipulation, ManipulationStack));
            if (InnovationStack > 0) result.Add(new(StatusIndex.Innovation, InnovationStack));
            if (VenerationStack > 0) result.Add(new(StatusIndex.Veneration, VenerationStack));
            return result.ToArray();
            
        }

        //public CraftingEngineState GenerateState()
        //{
        //    CraftingEngineState stats = new CraftingEngineState();
        //    stats.Step = (byte)Step;
        //    stats.Progress = CurrentProgress;
        //    stats.Quality = CurrentQuality;
        //    stats.CP = (ushort)CurrentCP;
        //    stats.Durability = (byte)CurrentDurability;
        //    stats.Condition = GetStepSettings().RecipeCondition;
        //    stats.InnerQuietStack = (byte)InnerQuietStack;
        //    stats.WasteNotStack = (byte)WasteNotStack;
        //    stats.VenerationStack = (byte)VenerationStack;
        //    stats.GreatStridesStack = (byte)GreatStridesStack;
        //    stats.InnovationStack = (byte)InnovationStack;
        //    stats.MuscleMemoryStack = (byte)MuscleMemoryStack;
        //    stats.ManipulationStack = (byte)ManipulationStack;
        //    stats.ObserveStack = ObserveStack == 1;
        //    stats.AdvancedTouchCombo = (byte)AdvancedTouchCombo;
        //    return stats;
        //}

        public CESPacked GetState()
        {
            return new CESPacked(this);
        }

        public void SetState(CESPacked state)
        {
            RemoveActions();
            Step = state.Step;
            CurrentProgress = state.Progress;
            CurrentQuality = state.Quality;
            CurrentCP = state.CPRemaining;
            CurrentDurability = state.Durability;
            InnerQuietStack = state.InnerQuietStack;
            
            WasteNotStack = state.WasteNotStack;
            VenerationStack = state.VenerationStack;
            GreatStridesStack = state.GreatStridesStack;
            InnovationStack = state.InnovationStack;
            MuscleMemoryStack = state.MuscleMemoryStack;
            ManipulationStack = state.ManipulationStack;
            ObserveStack = state.ObserveStack ? 1 : 0;
            AdvancedTouchCombo = state.AdvancedTouchCombo;
        }

        public void SetRecipe(RecipeInfo recipe)
        {
            if (CurrentRecipe == recipe || recipe == null)
                return;
            CurrentRecipe = recipe;
            StartingQuality = 0;
            ExecuteActions();
        }

        public void ExecuteActions(bool reset = true)
        {
            if (reset)
            {
                CurrentCP = MaxCP;
                CurrentProgress = 0;
                CurrentQuality = StartingQuality;

                InnerQuietStack = 0;
                WasteNotStack = 0;
                VenerationStack = 0;
                GreatStridesStack = 0;
                InnovationStack = 0;
                MuscleMemoryStack = 0;
                ManipulationStack = 0;
                ObserveStack = 0;
                AdvancedTouchCombo = 0;
                Step = 0;
            }
            if (CurrentRecipe is null) return;

            if (reset)
                CurrentDurability = CurrentRecipe.Durability;

            if (CraftingActionsLength == 0) return;

            for (int i = 0; i < CraftingActionsLength; i++)
            {
                byte action = CraftingActions[i];
                CraftingActionID actionID = (CraftingActionID)action;
                int cpCost = CA.GetCPCost(action);

                switch (AdvancedTouchCombo)
                {
                    case 0:
                        if (actionID == CraftingActionID.BasicTouch)
                            AdvancedTouchCombo = 1;
                        break;

                    case 1:
                        if (actionID == CraftingActionID.StandardTouch)
                            AdvancedTouchCombo = 2;
                        else
                            AdvancedTouchCombo = 0;
                        break;

                    case 2:
                        if (actionID == CraftingActionID.AdvancedTouch)
                            AdvancedTouchCombo = 3;
                        else
                            AdvancedTouchCombo = 0;
                        break;
                }

                if (AdvancedTouchCombo == 2 || AdvancedTouchCombo == 3)
                {
                    cpCost = 18;
                    if (AdvancedTouchCombo == 3)
                        AdvancedTouchCombo = 0;
                }
                if (GetStepSettings().RecipeCondition == RecipeCondition.Pliant)
                    cpCost = (int)Math.Ceiling(cpCost / 2d);

                if (CurrentDurability <= 0 ||
                    (CA.GetAsFirstActionOnly(action) && Step > 0) ||
                    CurrentProgress >= CurrentRecipe.MaxProgress ||
                    cpCost > CurrentCP ||
                    CheckAction(actionID) != CraftingActionResult.Success)
                {
                    RemoveRedundantActions();
                    return;
                }

                if (CA.GetIncreasesProgress(action))
                {
                    CurrentProgress += GetProgressIncrease(GetEfficiency(actionID));
                    if (MuscleMemoryStack > 0)
                        MuscleMemoryRemove = true;
                }

                if (CA.GetIncreasesQuality(action))
                {
                    if (actionID is CraftingActionID.TrainedEye)
                        CurrentQuality = CurrentRecipe.MaxQuality;
                    else
                        CurrentQuality += GetQualityIncrease(GetEfficiency(actionID));
                    if (InnerQuietStack < 10)
                        InnerQuietStack++;

                    if (actionID is CraftingActionID.PreciseTouch ||
                        actionID is CraftingActionID.PreparatoryTouch)
                    {
                        if (InnerQuietStack < 10)
                            InnerQuietStack++;
                    }

                    if (actionID == CraftingActionID.ByregotsBlessing)
                        InnerQuietRemove = true;

                    if (GreatStridesStack > 0)
                        GreatStridesRemove = true;
                }

                int durabilityCost = GetDurabilityCost(action);


                CurrentDurability -= durabilityCost;
                if (CurrentDurability > CurrentRecipe.Durability)
                    CurrentDurability = CurrentRecipe.Durability;
                CurrentCP -= cpCost;
                if (CurrentCP > MaxCP)
                    CurrentCP = MaxCP;

                if (WasteNotStack > 0)
                    WasteNotStack--;

                if (VenerationStack > 0)
                    VenerationStack--;
                if (GreatStridesStack > 0)
                    GreatStridesStack--;
                if (InnovationStack > 0)
                    InnovationStack--;
                if (MuscleMemoryStack > 0)
                    MuscleMemoryStack--;
                if (ManipulationStack > 0)
                {
                    if (CurrentDurability > 0 && actionID is not CraftingActionID.Manipulation)
                    {
                        ManipulationStack--;
                        CurrentDurability += 5;
                        if (CurrentDurability > CurrentRecipe.Durability)
                            CurrentDurability = CurrentRecipe.Durability;
                    }
                    
                }
                if (ObserveStack > 0)
                    ObserveStack--;

                if (InnerQuietRemove)
                {
                    InnerQuietRemove = false;
                    InnerQuietStack = 0;
                }

                if (GreatStridesRemove)
                {
                    GreatStridesRemove = false;
                    GreatStridesStack = 0;
                }

                if (MuscleMemoryRemove)
                {
                    MuscleMemoryRemove = false;
                    MuscleMemoryStack = 0;
                }
                Step++;

                if (CA.GetAddsBuff(action))
                    AddBuff(actionID);
            }
        }

        public int GetDurabilityCost(byte action)
        {
            int durabilityCost = CA.GetDurabilityCost(action);

            if (durabilityCost > 0)
            {
                if (GetStepSettings().RecipeCondition == RecipeCondition.Sturdy)
                    durabilityCost /= 2;
                if (WasteNotStack > 0)
                    durabilityCost = (int)Math.Ceiling(durabilityCost / 2d);
            }

            return durabilityCost;
        }

        public double GetEfficiency(CraftingActionID action)
        {
            switch (action)
            {
                case CraftingActionID.BasicSynthesis:
                    if (Level >= 31)
                        return 1.2d;
                    return 1d;

                case CraftingActionID.ByregotsBlessing:
                    return 1d + 0.2 * InnerQuietStack;

                case CraftingActionID.CarefulSynthesis:
                    if (Level >= 82)
                        return 1.8;
                    return 1.5d;

                case CraftingActionID.Groundwork:
                    double baseEff = 3;
                    if (Level >= 86)
                        baseEff = 3.6;
                    if (CurrentDurability < GetDurabilityCost((byte)action))
                        return baseEff / 2;
                    return baseEff;

                case CraftingActionID.TrainedEye:
                    return (double)CurrentRecipe.MaxQuality / GetQualityIncrease(1);

                case CraftingActionID.RapidSynthesis:
                    if (Level >= 63)
                        return 5d;
                    return 2.5;
            }

            return CA.EfficiencyArray[(byte)action];
        }

        public int GetProgressIncrease(double efficiency)
        {
            double realEfficiency = efficiency;
            if (MuscleMemoryStack > 0)
                realEfficiency += efficiency;
            if (VenerationStack > 0)
                realEfficiency += efficiency * 0.5;

            int value = (int)(Craftsmanship * 10 / (double)CurrentRecipe.ProgressDivider + 2);
            if (ActualLevel <= CurrentRecipe.Level)
                value = (int)(value * CurrentRecipe.ProgressModifier / 100d);
            return (int)(value * realEfficiency);
        }

        public static int GetProgress(double efficiency, double craftsmanship, int clvl, int rlvl, byte progressDivider = 100, byte progressModifier = 100)
        {
            int value = (int)(craftsmanship * 10 / progressDivider + 2);
            if (clvl <= rlvl)
                value = (int)(value * progressModifier / 100d);
            return (int)(value * efficiency);
        }

        public int GetQualityIncrease(double efficiency)
        {
            ActualControl = Control;

            double buffMult = 1;
            if (GreatStridesStack > 0)
                buffMult += 1;
            if (InnovationStack > 0)
                buffMult += 0.5;

            double iqBuff = 1;
            iqBuff += InnerQuietStack * 0.1;
            double conditionMod = 1;
            CraftingSimStepSettings settings = GetStepSettings();
            if (settings.RecipeCondition == RecipeCondition.Good)
                conditionMod = 1.5;
            if (settings.RecipeCondition == RecipeCondition.Excellent)
                conditionMod = 4;
            if (settings.RecipeCondition == RecipeCondition.Poor)
                conditionMod = 0.5;

            buffMult *= iqBuff;

            double value = (int)(ActualControl * 10 / CurrentRecipe.QualityDivider + 35);
            if (ActualLevel <= CurrentRecipe.Level)
                value = (int)(value * CurrentRecipe.QualityModifier / 100d);
            return (int)(value * conditionMod * buffMult * efficiency);
        }

        public static int GetQuality(double efficiency, double control, int cLvl, int rLvl, byte qualityDivider = 100, byte qualityModifier = 100)
        {
            double value = (int)(control * 10 / qualityDivider + 35);
            if (cLvl <= rLvl)
                value = (int)(value * qualityModifier / 100d);
            return (int)(value * efficiency);
        }

        public CraftingActionResult CheckAction(CraftingActionID action)
        {
            switch (action)
            {
                case CraftingActionID.ByregotsBlessing:
                    if (InnerQuietStack == 0)
                        return CraftingActionResult.NeedsBuff;
                    break;
                case CraftingActionID.FocusedSynthesis:
                case CraftingActionID.FocusedTouch:
                    if (ObserveStack == 0)
                        return CraftingActionResult.NeedsBuff;
                    break;

                case CraftingActionID.IntensiveSynthesis:
                case CraftingActionID.PreciseTouch:
                case CraftingActionID.TricksOfTheTrade:
                    var condition = GetStepSettings().RecipeCondition;
                    if (condition == RecipeCondition.Good || condition == RecipeCondition.Excellent)
                        return CraftingActionResult.Success;
                    return CraftingActionResult.NeedsBuff;
                
                case CraftingActionID.PrudentSynthesis:
                case CraftingActionID.PrudentTouch:
                    if (WasteNotStack > 0)
                        return CraftingActionResult.NeedsNoBuff;
                    return CraftingActionResult.Success;

                case CraftingActionID.TrainedEye:
                    if (CurrentRecipe.ClassJobLevel + 10 <= Level)
                        return CraftingActionResult.Success;
                    return CraftingActionResult.RecipeLevelTooHigh;
                    
                case CraftingActionID.TrainedFinesse:
                    if (InnerQuietStack == 10)
                        return CraftingActionResult.Success;
                    return CraftingActionResult.NeedsBuff;
            }

            return CraftingActionResult.Success;
        }

        public bool IsActionAvailable(byte action)
        {
            return CheckAction((CraftingActionID)action) == CraftingActionResult.Success;
        }

        public void AddBuff(CraftingActionID actionID)
        {
            switch(actionID)
            {
                //BasicSynthesis = 1,
                //BasicTouch = 2,
                //MastersMend = 3,
                case CraftingActionID.WasteNot:
                    WasteNotStack = 4;
                    break;
                case CraftingActionID.Veneration:
                    VenerationStack = 4;
                    break;
                //StandardTouch = 6,
                case CraftingActionID.GreatStrides:
                    GreatStridesStack = 3;
                    break;
                case CraftingActionID.Innovation:
                    InnovationStack = 4;
                    break;

                case CraftingActionID.WasteNotII:
                    WasteNotStack = 8;
                    break;
                //ByregotsBlessing = 10,
                case CraftingActionID.MuscleMemory:
                    MuscleMemoryStack = 5;
                    break;
                //CarefulSynthesis = 12,
                case CraftingActionID.Manipulation:
                    ManipulationStack = 8;
                    break;
                //PrudentTouch = 14,
                case CraftingActionID.Reflect:
                    InnerQuietStack = 2;
                    break;
                //PreparatoryTouch = 16,
                //Groundwork = 17,
                //DelicateSynthesis = 18,
                case CraftingActionID.Observe:
                    ObserveStack = 1;
                    break;
                //FocusedSynthesis = 20,
                //FocusedTouch = 21,
                //TrainedEye = 22,
                //TricksOfTheTrade = 23,
                //PreciseTouch = 24,
                //HastyTouch = 25,
                //RapidSynthesis = 26,
                //IntensiveSynthesis = 27,
                //AdvancedTouch = 28,
                //PrudentSynthesis = 29,
                //TrainedFinesse = 30,
                default: throw new Exception();
            }
        }

        public CraftingSimStepSettings GetStepSettings()
        {
            if (Step > ActionCount - 1)
                Debugger.Break();
            return StepSettings[Step];
        }

        public void SetStepSetting(int step, CraftingSimStepSettings settings)
        {
            if (settings == null)
            {
                StepSettings[step].RecipeCondition = RecipeCondition.Normal;
                return;
            }
            StepSettings[step] = settings;
        }

        public RecipeCondition Condition
        {
            get => GetStepSettings().RecipeCondition;
        }

        public bool CustomRecipe
        {
            get
            {
                return StepSettings.Any(x => x.RecipeCondition != RecipeCondition.Normal);
            }
        }

        public ulong Score
        {
            get
            {
                if (ScoreFunction == null)
                    return GetScore();
                return ScoreFunction(this);
            }
        }

        public override string ToString()
        {
            return base.ToString() + CraftingActionsLength.ToString();
        }

        public ulong GetScore()
        {
            if (CurrentRecipe == null) return 0;
            ulong score = 1;

            int progress = CurrentProgress;
            if (progress > CurrentRecipe.MaxProgress)
                progress = CurrentRecipe.MaxProgress;

            int quality = CurrentQuality;
            if (quality > CurrentRecipe.MaxQuality)
                quality = CurrentRecipe.MaxQuality;

            int progressBits = CurrentRecipe.MaxProgress.SignificantBits();
            int qualityBits = CurrentRecipe.MaxQuality.SignificantBits();
            int stepBits = 40.SignificantBits();
            int timeBits = 120.SignificantBits();
            int cpBits = MaxCP.SignificantBits();
            int durabilityBits = (CurrentRecipe.Durability + 20).SignificantBits();
            if (progressBits + qualityBits + timeBits + cpBits + durabilityBits > sizeof(ulong) * 8)
                throw new Exception();
            score = (score << progressBits) | (UInt64)progress;
            score = (score << qualityBits) | (UInt64)quality;
            //score = (score << stepBits) | (UInt64)(40 - Step);
            score = (score << timeBits) | (UInt64) (120 - CraftingActionsTime);
            score = (score << cpBits) | (UInt64)CurrentCP;
            score = (score << durabilityBits) | (UInt64)(CurrentDurability + 20);

            return score;
        }
        public delegate ulong ScoreDelegate(CraftingEngine sim);
    }
}
