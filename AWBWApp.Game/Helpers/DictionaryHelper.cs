using System.Collections.Generic;

namespace AWBWApp.Game.Helpers
{
    public static class DictionaryHelper
    {
        public static void SetTo<T, U>(this Dictionary<T, U> dict, Dictionary<T, U> other)
        {
            dict.Clear();

            foreach (var pair in other)
                dict.Add(pair.Key, pair.Value);
        }
    }
}
