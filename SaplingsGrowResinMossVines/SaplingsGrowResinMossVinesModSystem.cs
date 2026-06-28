using HarmonyLib;
using Vintagestory.API.Common;

namespace SaplingsGrowResinMossVines;

public class SaplingsGrowResinMossVines : ModSystem
{
    public static Harmony harmony;

    public virtual void Start(ICoreAPI api)
    {
        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();
    }

    public virtual void Dispose() => harmony.UnpatchAll(Mod.Info.ModID);
}