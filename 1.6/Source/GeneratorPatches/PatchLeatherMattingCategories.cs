using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect;

[HarmonyPatchCategory("OskarPotocki.VFE.Medieval2")]
[HarmonyPatch]
public static class TerrainFromLeather
{

    [HarmonyTargetMethod]
    public static System.Reflection.MethodInfo TargetMethod()
    {
        return AccessTools.TypeByName("VFEMedieval.VFEMedieval_DefGenerator_GenerateImpliedDefs_PreResolve_Patch").GetMethod("TerrainFromLeather");
    }

    [HarmonyPostfix]
    public static void Postfix(TerrainDef __result, Def tp)
    {
        if (tp.GetModExtension<TemplateCategoryExtension>() is { } extension)
            __result.designationCategory = extension.designationCategory;

    }
}