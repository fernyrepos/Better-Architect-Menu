using System.Collections.Generic;
using System.Text.RegularExpressions;
using Verse;

namespace BetterArchitect
{

    public static class ExtensionMethods
    {
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

        // Overridden by patch
       public static float uiWidth { get { return UI.screenWidth; } }
       public static float leftUIEdge { get { return 0f; } }
    }
}
