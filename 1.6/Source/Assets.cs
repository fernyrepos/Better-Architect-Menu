using UnityEngine;
using Verse;

namespace BetterArchitect
{
    [StaticConstructorOnStartup]
    public static class Assets
    {
        public static readonly Texture2D EditIcon = ContentFinder<Texture2D>.Get("Edit");
        public static readonly Texture2D EditIconHighlighted = ContentFinder<Texture2D>.Get("EditHighlighted");
        public static readonly Texture2D RestoreIcon = ContentFinder<Texture2D>.Get("Restore");
        public static readonly Texture2D GroupingIcon = ContentFinder<Texture2D>.Get("GroupType");
        public static readonly Texture2D SortType = ContentFinder<Texture2D>.Get("SortType");
        public static readonly Texture2D AscendingIcon = ContentFinder<Texture2D>.Get("SortAscend");
        public static readonly Texture2D DescendingIcon = ContentFinder<Texture2D>.Get("SortDescend");
        public static readonly Texture2D FreeIcon = ContentFinder<Texture2D>.Get("UI/Free");
    }
}

