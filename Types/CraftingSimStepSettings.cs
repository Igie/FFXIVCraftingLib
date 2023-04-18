namespace FFXIVCraftingLib.Types
{

    public enum RecipeCondition : byte
    {
        Normal = 0,
        Poor = 1,
        Good = 2,
        Excellent = 3,
        Centered = 4,
        Sturdy = 5,
        Pliant = 6
    }

    public class CraftingSimStepSettings
    {
        public RecipeCondition RecipeCondition { get; set; }

        public CraftingSimStepSettings()
        {
            RecipeCondition = RecipeCondition.Normal;
        }

        public CraftingSimStepSettings Clone()
        {
            return new CraftingSimStepSettings
            {
                RecipeCondition = RecipeCondition
            };
        }
    }
}
