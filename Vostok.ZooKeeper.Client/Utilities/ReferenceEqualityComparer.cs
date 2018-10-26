using System.Collections.Generic;

namespace Vostok.Zookeeper.Client.Utilities
{
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}