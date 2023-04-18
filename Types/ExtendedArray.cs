namespace FFXIVCraftingLib.Types
{
    public class ExtendedArray<T> : IEquatable<ExtendedArray<T>>
    {
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ExtendedArray<T>)obj);
        }

        public T[] Array { get; set; }

        public ExtendedArray(T[] array)
        {
            Array = array;
        }

        public ExtendedArray(int length)
        {
            Array = new T[length];
        }

        public bool Equals(ExtendedArray<T> other)
        {
            if (other is null)
                return false;
            if (Array.Length != other.Array.Length)
                return false;
            for (int i = 0; i < Array.Length; i++)
                if (!Array[i].Equals(other.Array[i]))
                    return false;
            return true;
        }

        public static bool operator ==(ExtendedArray<T> first, ExtendedArray<T> second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null)) return false;
            if (!ReferenceEquals(first, null)) return first.Equals(second);
            if (!ReferenceEquals(second, null)) return second.Equals(first);
            return false;
        }

        public static bool operator !=(ExtendedArray<T> first, ExtendedArray<T> second)
        {
            if (ReferenceEquals(first, second)) return false;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null)) return true;
            if (!ReferenceEquals(first, null)) return !first.Equals(second);
            if (!ReferenceEquals(second, null)) return !second.Equals(first);
            return true;
        }

        public override int GetHashCode()
        {
            int result = 13;
            for (int i = 0; i < Array.Length; i++)
            {
                result *= 29;
                result ^= Array[i].GetHashCode();
            }
            return result;
        }

        public static implicit operator ExtendedArray<T>(T[] array)
        {
            return new ExtendedArray<T>(array);
        }

        public static implicit operator T[](ExtendedArray<T> array)
        {
            return array.Array;
        }
    }
}
