using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods.NoObf;
using ITreeGenerator = Vintagestory.API.Server.ITreeGenerator;

namespace SaplingsGrowResinMossVines;

[HarmonyPatch(typeof(BlockEntitySapling), "CheckGrow")]
public static class PatchSaplingGrowth
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        Console.WriteLine("[PatchSaplingGrowth][Transpiler] Start");
        CodeMatch matchTreeGenparamsFieldInit = CodeMatch.StoresField(typeof(TreeGenParams).Field("mossGrowthChance"));
        CodeMatch matchStoreTreeGenParams = CodeMatch.StoresLocal("treeGenParams");
        
        CodeInstruction[] codeToInsert = [
            CodeInstruction.LoadArgument(0),
            CodeInstruction.LoadLocal(5),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSaplingGrowth), nameof(SetTreeGenParams))),
        ];
        Console.WriteLine("[PatchSaplingGrowth][Transpiler][Code] {0}", codeToInsert);

        CodeMatcher codeMatcher = new CodeMatcher(instructions);
        Console.WriteLine("[PatchSaplingGrowth][Transpiler] Matching against {0} instructions", codeMatcher.Length);

        codeMatcher.MatchStartForward(matchTreeGenparamsFieldInit);
        Console.WriteLine("[PatchSaplingGrowth][Transpiler] Matched init at {0}", codeMatcher.Pos);
        codeMatcher.MatchStartForward(matchStoreTreeGenParams);
        Console.WriteLine("[PatchSaplingGrowth][Transpiler] Matched store at {0}", codeMatcher.Pos);

        {
            int range = 5; 
            int indexStart = Math.Max(codeMatcher.Pos - range, 0);
            int indexEnd = Math.Min(codeMatcher.Pos + range, codeMatcher.Length - 1);
            int sliceSize = indexEnd - indexStart;
            codeMatcher.Instructions()
                .Slice(indexStart, sliceSize)
                .ForEach(instruction => Console.WriteLine("[PatchSaplingGrowth][Transpiler][CodePreInsert] {0}", instruction));
        }
        
        codeMatcher.InsertAfter(codeToInsert);
        Console.WriteLine("[PatchSaplingGrowth][Transpiler] Now there are {0} instructions", codeMatcher.Length);

        {
            int range = 5; 
            int indexStart = Math.Max(codeMatcher.Pos - range, 0);
            int indexEnd = Math.Min(codeMatcher.Pos + range, codeMatcher.Length - 1);
            int sliceSize = indexEnd - indexStart;
            codeMatcher.Instructions()
                .Slice(indexStart, sliceSize)
                .ForEach(instruction => Console.WriteLine("[PatchSaplingGrowth][Transpiler][CodePostInsert] {0}", instruction));
        }
        
        return codeMatcher.Instructions();
    }

    public static void DoNothing()
    {
        Console.WriteLine("[PatchSaplingGrowth] DoNothing");
    }
    
    public static void Dummy1(BlockEntitySapling saplingEntity)
    {
        saplingEntity.Api.Logger.Debug("Dummy1 called");
    }
    
    public static void Dummy2(BlockEntitySapling saplingEntity, TreeGenParams treeGenParams)
    {
        saplingEntity.Api.Logger.Debug("Dummy2 called");
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