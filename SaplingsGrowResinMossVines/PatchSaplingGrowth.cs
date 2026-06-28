using HarmonyLib;
using Vintagestory.GameContent;

namespace SaplingsGrowResinMossVines;

[HarmonyPatch(typeof (BlockEntitySapling))]
public class PatchSaplingGrowth
{
    // todo: patch BlockEntitySapling.CheckGrow
}