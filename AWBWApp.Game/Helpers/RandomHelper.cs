using System;
using System.Collections.Generic;
using System.Linq;

namespace AWBWApp.Game.Helpers
{
    public static class RandomHelper
    {
        public static T Pick<T>(this Random random, IList<T> list)
        {
            return list[random.Next(list.Count)];
        }

        public static T Pick<T>(this Random random, HashSet<T> set)
        {
            var element = random.Next(set.Count);
            return set.ElementAt(element);
        }

        public static U Pick<T, U>(this Random random, Dictionary<T, U> set)
        {
            var element = random.Next(set.Count);
            return set.ElementAt(element).Value;
        }
    }
}
