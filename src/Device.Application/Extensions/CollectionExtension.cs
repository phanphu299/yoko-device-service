using System;
using System.Collections.Generic;
using System.Linq;

namespace Device.ApplicationExtension.Extension
{
    public static class CollectionExtension
    {
        public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            T element;

            for (int i = 0; i < collection.Count; i++)
            {
                element = collection.ElementAt(i);
                if (predicate(element))
                {
                    collection.Remove(element);
                    i--;
                }
            }
        }
    }
}
