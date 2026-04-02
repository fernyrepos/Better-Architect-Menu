using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect;

[HarmonyPatch(typeof(TerrainDefGenerator_Carpet), nameof(TerrainDefGenerator_Carpet.CarpetFromBlueprint))]
public static class CarpetFromBlueprint
{

    public static void Postfix(TerrainDef __result, TerrainTemplateDef tp)
    {
        if (tp.GetModExtension<TemplateCategoryExtension>() is { } extension)
            __result.designationCategory = extension.designationCategory;
    }

}