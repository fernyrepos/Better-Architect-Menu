using System.Collections.Generic;
using System.Text.RegularExpressions;
using Verse;

namespace BetterArchitect
{

    public static class ExtensionMethods
    {
        // Can be overridden by patches to change the width of the UI
        public static float UIWidth { get { return UI.screenWidth; } }
        public static float LeftUIEdge { get { return 0f; } }

        public static void SortBy<T, K>(this List<T> list, System.Func<T, K> keySelector, bool ascending) where K : System.IComparable
        {
            list.Sort((a, b) =>
            {
                var keyA = keySelector(a);
                var keyB = keySelector(b);
                int comparison = keyA.CompareTo(keyB);
                return ascending ? comparison : -comparison;
            });
        }

        public static string ToStringTranslated(this SortBy sortBy)
        {
            return ("BA." + sortBy.ToString()).Translate();
        }

    }
}
