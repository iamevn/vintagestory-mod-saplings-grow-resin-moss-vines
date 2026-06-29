using HarmonyLib;
using Vintagestory.API.Common;

namespace SaplingsGrowResinMossVines;

public class SaplingsGrowResinMossVines : ModSystem
{
    public static Harmony harmony;

    public override void Start(ICoreAPI api)
    {
        api.Logger.Debug("About to patch for {0}", Mod.Info.ModID);
        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();
        api.Logger.Debug("Finished patching for {0}", Mod.Info.ModID);
    }

    public override void Dispose() => harmony.UnpatchAll(Mod.Info.ModID);
}