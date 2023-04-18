using FFXIVDataManager;
using FFXIVDataManager.GameData;
using FFXIVDataManager.Stream;

namespace FFXIVCraftingLib.Types
{
    public class CraftingRotationsInfo : LoadableData
    {
        private RecipesInfo RecipesInfo { get; set; }
        public bool IsLoading { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public int LoadTime { get; set; } = 0;

        public Dictionary<AbstractRecipeInfo, List<CraftingRotationInfo>> CraftingRotationDictionary { get; set; }

        public void AddRotation(CraftingEngine engine)
        {
            if (engine?.CurrentRecipe == null) return;
            var list = CraftingRotationDictionary[engine.CurrentRecipe.AbstractData];
            var rotation = CraftingRotationInfo.FromSim(engine, true);
            if (rotation == null) return;
            if (rotation == null) return;
            int level = rotation.MinLevel;
            if (rotation != null && !list.Contains(rotation))
                list.Add(rotation);
            rotation = CraftingRotationInfo.FromSim(engine, false);
            if (rotation != null && !list.Contains(rotation) && level < rotation.MinLevel)
                list.Add(rotation);
        }

        public void RemoveRotation(CraftingEngine engine, CraftingRotationInfo rotation)
        {
            if (engine?.CurrentRecipe == null) return;
            var list = CraftingRotationDictionary[engine.CurrentRecipe.AbstractData];
            if (rotation != null && list.Contains(rotation))
                list.Remove(rotation);
        }

        public void ReadFrom(DataStream s)
        {
            IsLoading = true;
            RecipesInfo = DataLoader.GetFile<RecipesInfo>();

            CraftingRotationDictionary = new Dictionary<AbstractRecipeInfo, List<CraftingRotationInfo>>();
            foreach (var abstractRecipe in RecipesInfo.AbstractRecipes)
                CraftingRotationDictionary.Add(abstractRecipe, new List<CraftingRotationInfo>());

            int count = s.ReadS32();

            for (int i = 0; i < count; i++)
            {
                AbstractRecipeInfo abstractRecipe = new AbstractRecipeInfo
                {
                    Level = s.ReadS32(),
                    RequiredCraftsmanship = s.ReadS32(),
                    RequiredControl = s.ReadS32(),
                    Durability = s.ReadS32(),
                    MaxProgress = s.ReadS32(),
                    MaxQuality = s.ReadS32(),
                    ProgressDivider = (byte)s.ReadByte(),
                    QualityDivider = (byte)s.ReadByte(),
                    ProgressModifier = (byte)s.ReadByte(),
                    QualityModifier = (byte)s.ReadByte(),
                };

                List<CraftingRotationInfo> rotations = CraftingRotationDictionary[abstractRecipe];
                if (rotations == null) throw new Exception();

                int rotationCount = s.ReadS32();

                for (int j = 0; j < rotationCount; j++)
                {
                    CraftingRotationInfo rotation = new CraftingRotationInfo();
                    rotation.MinLevel = s.ReadS32();
                    rotation.MaxCraftsmanship = s.ReadS32();
                    rotation.MinCraftsmanship = s.ReadS32();
                    rotation.MinControl = s.ReadS32();
                    rotation.CP = s.ReadS32();
                    rotation.IngredientQuality = s.ReadS32();
                    rotation.Quality = s.ReadS32();
                    rotation.QualityPercentage = s.ReadS32();
                    rotation.QualityPercentage = s.ReadS32();
                    int actionCount = s.ReadS32();
                    byte[] actions = new byte[actionCount];
                    for (int k = 0; k < actionCount; k++)
                        actions[k] = (byte)s.ReadS32();
                    rotation.Rotation = actions;
                    rotations.Add(rotation);
                }
            }

            IsLoaded = true;
        }

        public void ReadFromGameData()
        {
            IsLoading = true;
            RecipesInfo = DataLoader.GetFile<RecipesInfo>();

            CraftingRotationDictionary = new Dictionary<AbstractRecipeInfo, List<CraftingRotationInfo>>();
            foreach (var abstractRecipe in RecipesInfo.AbstractRecipes)
                CraftingRotationDictionary.Add(abstractRecipe, new List<CraftingRotationInfo>());

            IsLoaded = true;
        }

        public void WriteTo(DataStream s)
        {
            RecipesInfo = DataLoader.GetFile<RecipesInfo>();

            int count = RecipesInfo.AbstractRecipes.Length;
            s.WriteS32(count);
            for (int i = 0; i < count; i++)
            {
                AbstractRecipeInfo abstractRecipe = RecipesInfo.AbstractRecipes[i];
                s.WriteS32(abstractRecipe.Level);
                s.WriteS32(abstractRecipe.RequiredCraftsmanship);
                s.WriteS32(abstractRecipe.RequiredControl);
                s.WriteS32(abstractRecipe.Durability);
                s.WriteS32(abstractRecipe.MaxProgress);
                s.WriteS32(abstractRecipe.MaxQuality);
                s.WriteByte(abstractRecipe.ProgressDivider);
                s.WriteByte(abstractRecipe.QualityDivider);
                s.WriteByte(abstractRecipe.ProgressModifier);
                s.WriteByte(abstractRecipe.QualityModifier);

                List<CraftingRotationInfo> rotations = CraftingRotationDictionary[abstractRecipe];
                if (rotations == null) throw new Exception();

                int rotationCount = rotations.Count;
                s.WriteS32(rotationCount);

                for (int j = 0; j < rotationCount; j++)
                {
                    var rotation = rotations[j];
                    s.WriteS32(rotation.MinLevel);
                    s.WriteS32(rotation.MaxCraftsmanship);
                    s.WriteS32(rotation.MinCraftsmanship);
                    s.WriteS32(rotation.MinControl);
                    s.WriteS32(rotation.CP);
                    s.WriteS32(rotation.IngredientQuality);
                    s.WriteS32(rotation.Quality);
                    s.WriteS32(rotation.QualityPercentage);
                    s.WriteS32(rotation.QualityPercentage);
                    int actionCount = rotation.Rotation.Array.Length;
                    s.WriteS32(actionCount);
                    for (int k = 0; k < actionCount; k++)
                        s.WriteS32(rotation.Rotation.Array[k]);
                }
            }
        }
    }
}
