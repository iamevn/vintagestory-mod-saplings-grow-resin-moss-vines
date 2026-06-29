using HarmonyLib;
using Vintagestory.API.Common;

namespace SaplingsGrowResinMossVines;

public class SaplingsGrowResinMossVines : ModSystem
{
    public static Harmony harmony;

    public override void Start(ICoreAPI api)
    {
        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();
    }

    public override void Dispose() => harmony.UnpatchAll(Mod.Info.ModID);
}