using FFXIVCraftingLib.Types;
using System;
using System.Diagnostics;
using FFXIVDataManager.GameData;
using FFXIVDataManager;

namespace FFXIVCraftingLib.Actions
{
    public enum CraftingActionResult
    {
        Success,
        CraftCompleted,
        NeedsBuff,
        NeedsNoBuff,
        BuffUsedUp,
        NotEnoughDurability,
        NotEnoughCP,
        FirstActionOnly,
        RecipeLevelTooHigh
    }

    [Flags]
    public enum CraftingActionType
    {
        None = 0,
        IncreasesProgress = 1,
        IncreasesQuality = 2,
        IsBuff = 4
    }

    public static class CraftingActions
    {
        public const byte NONE = 0;
        public const byte BasicSynthesis = 1;
        public const byte BasicTouch = 2;
        public const byte MastersMend = 3;
        public const byte WasteNot = 4;
        public const byte Veneration = 5;
        public const byte StandardTouch = 6;
        public const byte GreatStrides = 7;
        public const byte Innovation = 8;
        public const byte WasteNotII = 9;
        public const byte ByregotsBlessing = 10;
        public const byte MuscleMemory = 11;
        public const byte CarefulSynthesis = 12;
        public const byte Manipulation = 13;
        public const byte PrudentTouch = 14;
        public const byte Reflect = 15;
        public const byte PreparatoryTouch = 16;
        public const byte Groundwork = 17;
        public const byte DelicateSynthesis = 18;
        public const byte Observe = 19;
        public const byte FocusedSynthesis = 20;
        public const byte FocusedTouch = 21;
        public const byte TrainedEye = 22;
        public const byte TricksOfTheTrade = 23;
        public const byte PreciseTouch = 24;
        public const byte HastyTouch = 25;
        public const byte RapidSynthesis = 26;
        public const byte IntensiveSynthesis = 27;
        public const byte AdvancedTouch = 28;
        public const byte PrudentSynthesis = 29;
        public const byte TrainedFinesse = 30;
    }

    public enum CraftingActionID : byte
    {
        NONE = 0,
        BasicSynthesis = 1,
        BasicTouch = 2,
        MastersMend = 3,
        WasteNot = 4,
        Veneration = 5,
        StandardTouch = 6,
        GreatStrides = 7,
        Innovation = 8,
        WasteNotII = 9,
        ByregotsBlessing = 10,
        MuscleMemory = 11,
        CarefulSynthesis = 12,
        Manipulation = 13,
        PrudentTouch = 14,
        Reflect = 15,
        PreparatoryTouch = 16,
        Groundwork = 17,
        DelicateSynthesis = 18,
        Observe = 19,
        FocusedSynthesis = 20,
        FocusedTouch = 21,
        TrainedEye = 22,
        TricksOfTheTrade = 23,
        PreciseTouch = 24,
        HastyTouch = 25,
        RapidSynthesis = 26,
        IntensiveSynthesis = 27,
        AdvancedTouch = 28,
        PrudentSynthesis = 29,
        TrainedFinesse = 30,
    }


    public static class CraftingAction
    {
        static CraftingAction()
        {
            ActionsInfo = DataLoader.GetFile<ActionsInfo>();
        }

        public static readonly byte[] Ids =
            {
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
            };

        public static readonly string[] NameArray =
            {
                null,
                "Basic Synthesis",
                "Basic Touch",
                "Master's Mend",
                "Waste Not",
                "Veneration",
                "Standard Touch",
                "Great Strides",
                "Innovation",
                "Waste Not II",
                "Byregot's Blessing",
                "Muscle Memory",
                "Careful Synthesis",
                "Manipulation",
                "Prudent Touch",
                "Reflect",
                "Preparatory Touch",
                "Groundwork",
                "Delicate Synthesis",
                "Observe",
                "Focused Synthesis",
                "Focused Touch",
                "Trained Eye",
                "Tricks of the Trade",
                "Precise Touch",
                "Hasty Touch",
                "Rapid Synthesis",
                "Intensive Synthesis",
                "Advanced Touch",
                "Prudent Synthesis",
                "Trained Finesse",
        };

        public static string GetName(byte action)
        {
            return NameArray[action];
        }

        public static readonly int[] LevelArray =
        {
            0,
            1,
            5,
            7,
            15,
            15,
            18,
            21,
            26,
            47,
            50,
            54,
            62,
            65,
            66,
            69,
            71,
            72,
            76,
            13,
            67,
            68,
            80,
            13,
            53,
            9,
            9,
            78,
            84,
            88,
            90,
        };

        public static int GetLevel(byte action)
        {
            return LevelArray[action];
        }

        public static readonly bool[] IsBuffArray =
        {
            false,
            false,
            false,
            false,
            true,
            true,
            false,
            true,
            true,
            true,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
        };

        public static bool GetIsBuff(byte action)
        {
            return IsBuffArray[action];
        }

        public static readonly bool[] IncreasesProgressArray =
        {
            false,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            true,
            true,
            false,
            false,
            false,
            false,
            true,
            true,
            false,
            true,
            false,
            false,
            false,
            false,
            false,
            true,
            true,
            false,
            true,
            false,
        };

        public static bool GetIncreasesProgress(byte action)
        {
            return IncreasesProgressArray[action];
        }

        public static readonly bool[] IncreasesQualityArray =
        {
            false,
            false,
            true,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            true,
            true,
            true,
            false,
            true,
            false,
            false,
            true,
            true,
            false,
            true,
            true,
            false,
            false,
            true,
            false,
            true,
        };

        public static bool GetIncreasesQuality(byte action)
        {
            return IncreasesQualityArray[action];
        }

        public static readonly int[] DurabilityCostArray =
        {
            0,
            10,
            10,
            -30,
            0,
            0,
            10,
            0,
            0,
            0,
            10,
            10,
            10,
            0,
            5,
            10,
            20,
            20,
            10,
            0,
            10,
            10,
            0,
            0,
            10,
            10,
            10,
            10,
            10,
            5,
            0,
        };

        public static int GetDurabilityCost(byte action)
        {
            return DurabilityCostArray[action];
        }

        public static readonly int[] CPCostArray =
        {
            0,
            0,
            18,
            88,
            56,
            18,
            32,
            32,
            18,
            98,
            24,
            6,
            7,
            96,
            25,
            6,
            40,
            18,
            32,
            7,
            5,
            18,
            250,
            -20,
            18,
            0,
            0,
            6,
            46,
            18,
            32,
        };

        public static int GetCPCost(byte action)
        {
            return CPCostArray[action];
        }

        public static readonly double[] SuccessArry =
        {
            0,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            0.6,
            0.5,
            1,
            1,
            1,
            1,
        };

        public static double GetSuccess(byte action)
        {
            return SuccessArry[action];
        }

        public static readonly bool[] AsFirstActionOnlyArray =
        {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
        };

        public static bool GetAsFirstActionOnly(byte action)
        {
            return AsFirstActionOnlyArray[action];
        }

        public static readonly bool[] AddsBuffArray =
        {
            false,
            false,
            false,
            false,
            true,
            true,
            false,
            true,
            true,
            true,
            false,
            true,
            false,
            true,
            false,
            true,
            false,
            false,
            false,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
        };

        public static bool GetAddsBuff(byte action)
        {
            return AddsBuffArray[action];
        }

        public static ImageInfo GetImage(byte action, ClassJobInfo classJob)
        {
            return ActionsInfo[GetName(action)].Images[classJob];
        }

        private static ActionsInfo ActionsInfo { get; set; }


        public static readonly double[] EfficiencyArray =
        {
            0,
            0,
            1,
            0,
            0,
            0,
            1.25,
            0,
            0,
            0,
            0,
            3,
            0,
            0,
            1,
            1,
            2,
            0,
            1,
            0,
            2,
            1.5,
            0,
            0,
            1.5,
            1,
            0,
            4,
            1.5,
            1.8,
            1,
        };
    }
}
