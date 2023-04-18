using FFXIVCraftingLib.Actions;

namespace FFXIVCraftingLib.Types
{
    public class CraftingRotationInfo : IEquatable<CraftingRotationInfo>
    {
        public int MinLevel { get; set; }
        public int MaxCraftsmanship { get; set; }
        public int MinCraftsmanship { get; set; }
        public int MinControl { get; set; }
        public int CP { get; set; }

        public int IngredientQuality { get; set; }
        public int Quality { get; set; }

        public int QualityPercentage { get; set; }
        public int RotationTime { get; private set; }

        public string InfoString => $"CRF:{MaxCraftsmanship}/{MinCraftsmanship} CTRL:{MinControl} CP:{CP}, QUALITY:{Quality}, TIME:{RotationTime}";
        private ExtendedArray<byte> _Rotation;
        public ExtendedArray<byte> Rotation
        {
            get => _Rotation;
            set
            {
                _Rotation = value;
                int time = Rotation.Array.Sum(t => CraftingAction.GetIsBuff(t) ? 2 : 3);

                RotationTime = time;
            }
        }


        public override bool Equals(object obj)
        {
            return base.Equals(obj as CraftingRotationInfo);
        }

        public bool IsBetterThanOrEqual(CraftingRotationInfo other)
        {
            return MinLevel <= other.MinLevel &&
                MaxCraftsmanship >= other.MaxCraftsmanship &&
                MinCraftsmanship <= other.MinCraftsmanship &&
                MinControl <= other.MinControl &&
                CP <= other.CP &&
                IngredientQuality <= other.IngredientQuality &&
                Quality >= other.Quality &&
                RotationTime <= other.RotationTime;
        }

        public static bool operator ==(CraftingRotationInfo left, CraftingRotationInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (!ReferenceEquals(left, null))
                return left.Equals(right);
            if (!ReferenceEquals(right, null))
                return right.Equals(left);
            throw new Exception();
        }

        public static bool operator !=(CraftingRotationInfo left, CraftingRotationInfo right)
        {
            if (ReferenceEquals(left, right))
                return false;
            if (!ReferenceEquals(left, null))
                return !left.Equals(right);
            if (!ReferenceEquals(right, null))
                return !right.Equals(left);
            throw new Exception();
        }

        public bool IsBetterThan(CraftingRotationInfo other)
        {
            return IsBetterThanOrEqual(other) && !Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 7;
            hash ^= MinLevel;
            hash ^= MaxCraftsmanship * 7;
            hash ^= MinCraftsmanship * 3;
            hash ^= MinControl * 13;
            hash ^= CP * 7;
            hash ^= IngredientQuality * 7;
            hash ^= Quality * 7;
            hash ^= Rotation.GetHashCode() * 29;
            return hash;
        }

        public bool Equals(CraftingRotationInfo other)
        {
            if (other is null)
                return false;
            return MinLevel == other.MinLevel &&
                MaxCraftsmanship == other.MaxCraftsmanship &&
                MinCraftsmanship == other.MinCraftsmanship &&
                MinControl == other.MinControl &&
                CP == other.CP &&
                IngredientQuality == other.IngredientQuality &&
                Quality == other.Quality &&
                Rotation.Equals(other.Rotation);
        }

        public static CraftingRotationInfo FromSim(CraftingEngine engine, bool findMinLevel)
        {
            if (engine == null || engine.CurrentProgress < engine.CurrentRecipe.MaxProgress)
                return null;
            CraftingRotationInfo result = new CraftingRotationInfo();
            CraftingEngine s = engine.Clone(false);
            var actions = engine.GetCraftingActions();
            s.AddActions(true, actions);
            result.CP = s.MaxCP - s.CurrentCP;
            result.IngredientQuality = s.StartingQuality;
            result.Quality = Math.Min(s.CurrentQuality, s.CurrentRecipe.MaxQuality);
            result.QualityPercentage = (int)Math.Floor((double)result.Quality / s.CurrentRecipe.MaxQuality * 100);
            result.Rotation = actions.ToArray();
            s.Level = s.CurrentRecipe.ClassJobLevel;
            s.Level = engine.Level;
            if (findMinLevel)
            {
                int minLevelFromActionsLevel = actions.Max(CraftingAction.GetLevel);
                int minLevelFromSuccess = s.Level;

                int rotationQuality = engine.CurrentQuality;

                bool craftFailed = false;

                while (!craftFailed)
                {
                    s.RemoveActions();
                    minLevelFromSuccess--;
                    s.Level = minLevelFromSuccess;
                    s.AddActions(true, actions);
                    craftFailed = s.CurrentProgress < s.CurrentRecipe.MaxProgress || s.CurrentQuality < rotationQuality || s.Level < s.CurrentRecipe.ClassJobLevel;
                }

                minLevelFromSuccess++;

                result.MinLevel = Math.Max(minLevelFromSuccess, minLevelFromActionsLevel);
            }
            else
                result.MinLevel = engine.Level;

            s.Level = result.MinLevel;

            int recipeProgress = s.CurrentRecipe.MaxProgress;
            int recipeQuality = s.CurrentRecipe.MaxQuality;

            int oldCraftsmanshipBuff = s.CraftsmanshipBuff;
            int oldControlBuff = s.ControlBuff;
            s.AddActions(true, actions);
            while (s.CurrentProgress > recipeProgress)
            {
                s.CraftsmanshipBuff--;
                s.ExecuteActions();
            }
            s.CraftsmanshipBuff++;
            s.RemoveActions();
            s.AddActions(true, actions);
            result.MinCraftsmanship = Math.Max(s.Craftsmanship, engine.CurrentRecipe.RequiredCraftsmanship);


            while (s.CurrentQuality >= recipeQuality && s.Control > 0)
            {
                s.ControlBuff--;
                s.ExecuteActions();
            }
            result.MinControl = Math.Max(s.Control + 1, engine.CurrentRecipe.RequiredControl);

            s.CraftsmanshipBuff = oldCraftsmanshipBuff;
            s.ControlBuff = oldControlBuff;

            int oldActionsLength = actions.Length;
            int newActionsLength = oldActionsLength;
            s.RemoveActions();
            s.AddActions(true, actions);

            while (newActionsLength >= oldActionsLength)
            {

                s.CraftsmanshipBuff++;
                s.ExecuteActions();
                newActionsLength = s.CraftingActionsLength;
                if (s.Craftsmanship > 10000)
                    break;
            }
            result.MaxCraftsmanship = s.Craftsmanship - 1;
            return result;
        }
    }
}
