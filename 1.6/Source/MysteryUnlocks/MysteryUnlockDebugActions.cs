using LudeonTK;
using Verse;

namespace BetterArchitect
{
    public static class MysteryUnlockDebugActions
    {
        [DebugAction("Better Architect", "Mystery-box all buildings", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void MysteryBoxAllBuildings()
        {
            MysteryUnlockTracker.MarkAllAvailable();
        }

        [DebugAction("Better Architect", "Clear mystery boxes", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearMysteryBoxes()
        {
            MysteryUnlockTracker.ClearAll();
        }
    }
}
