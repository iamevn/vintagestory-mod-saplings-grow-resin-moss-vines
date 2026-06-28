using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;
using ITreeGenerator = Vintagestory.API.Server.ITreeGenerator;

namespace SaplingsGrowResinMossVines;

[HarmonyPatch]
public class PatchSaplingGrowth
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(BlockEntitySapling), "CheckGrow")]
    public static void CheckGrow()
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            CodeMatch matchGrowTreeCall = CodeMatch.Calls(typeof(ITreeGenerator).GetMethod("GrowTree")); 
            CodeMatch matchTreeGenparamsFieldInit = CodeMatch.StoresField(typeof(TreeGenParams).Field("mossGrowthChance"));
            CodeMatch matchStoreTreeGenParams = CodeMatch.StoresLocal("treeGenParams");
            CodeInstruction[] codeToInsert = [
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(TreeGenParams), "treeGenParams"),
                CodeInstruction.Call(() => SetTreeGenParams(default,default)),
            ];
            
            var codeMatcher = new CodeMatcher(instructions);
            
            codeMatcher.MatchStartForward(matchTreeGenparamsFieldInit);
            codeMatcher.MatchStartForward(matchStoreTreeGenParams);

            codeMatcher.InsertAfter(codeToInsert);
            
            return codeMatcher.Instructions();
        }
    }

    public static void SetTreeGenParams(BlockEntitySapling saplingEntity, TreeGenParams treeGenParams)
    {
        ICoreAPI api = saplingEntity.Api;
        BlockPos saplingPos = saplingEntity.Pos;

        api.Logger.Chat("Overriding TreeGenParams from [otherBlockChance={0}][hemisphere={1}][vinesGrowthChance={2}][mossGrowthChance={3}]", treeGenParams.otherBlockChance, treeGenParams.hemisphere, treeGenParams.vinesGrowthChance, treeGenParams.mossGrowthChance);
        
        // always allow alt blocks like resin 
        treeGenParams.otherBlockChance = 1.0f;
        
        // set hemisphere for moss growth
        treeGenParams.hemisphere = api.World.Calendar.GetHemisphere(saplingPos);
        
        // set vines and moss to grow like in worldgen (calculating like WgenTreeSupplier.GetRandomGenForClimate)
        TreeGenProperties treeGenProps = api.Assets.Get((AssetLocation) "worldgen/treengenproperties.json").ToObject<TreeGenProperties>();

        ClimateCondition climateAt = api.World.BlockAccessor.GetClimateAt(saplingPos);
        float rain = climateAt.Rainfall; // WorldGenRainfall?
        float temp = climateAt.Temperature; // WorldGenTemperature?
        int descaledTemp = Climate.DescaleTemperature(temp);
        
        float rainVal = Math.Max(0, (rain / 255f - treeGenProps.vinesMinRain) / (1 - treeGenProps.vinesMinRain));
        float tempVal = Math.Max(0, (descaledTemp / 255f - treeGenProps.descVineMinTempRel) - (1 / treeGenProps.descVineMinTempRel));

        float rainValMoss = rain / 255f;
        float tempValMoss = temp / 255f;
            
        treeGenParams.vinesGrowthChance =
            1.5f * rainVal * tempVal + 0.5f * rainVal * GameMath.Clamp((tempVal + 0.33f) / 1.33f, 0, 1);
        var mossGrowChance = 2.25 * rainValMoss - 0.5 + Math.Sqrt(tempValMoss) * 3 * Math.Max(-0.5, 0.5 - tempValMoss);
        treeGenParams.mossGrowthChance = GameMath.Clamp((float)mossGrowChance, 0, 1);
        
        api.Logger.Chat("Set TreeGenParams to [otherBlockChance={0}][hemisphere={1}][vinesGrowthChance={2}][mossGrowthChance={3}]", treeGenParams.otherBlockChance, treeGenParams.hemisphere, treeGenParams.vinesGrowthChance, treeGenParams.mossGrowthChance);
    }
}