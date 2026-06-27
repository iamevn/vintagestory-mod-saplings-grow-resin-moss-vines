# Vintage Story Mod: Saplings May Grow Resin/Moss/Vines

Patch saplings to enable alt blocks when the sapling grows into a tree.

`BlockEntitySaplings` [currently](https://github.com/anegostudios/vssurvivalmod/blob/36d9550e19c32197e8f0d8a9780a5d0b1320dc7c/BlockEntity/BESapling.cs#L145-L154) don't allow optional blocks a chance to generate:
```csharp
TreeGenParams pa = new TreeGenParams()
{
    skipForestFloor = true,
    size = size,
    otherBlockChance = 0,
    vinesGrowthChance = 0,
    mossGrowthChance = 0
};

gen.GrowTree(Api.World.BulkBlockAccessor, Pos.DownCopy(), pa, normalRandom);
```

The [Make Me Leak mod](https://mods.vintagestory.at/show/mod/21445) enables resin by patching the [`TreeGen.TriggerRandomOtherBlock` method](https://github.com/anegostudios/vsessentialsmod/blob/3319dff9e22b2f57c301a915fe5aa67e1ca349d2/Systems/WorldGen/Standard/ChunkGen/8.GenVegetationAndPatches/Treegen/TreeGen.cs#L306-L309)
to ignore `TreeGenParams.otherBlockChance` and only depend on the `otherLogChance` for the tree that is growing.
(Note: it also uses system's `Random` instead of the passed-in `lcgRandom` which isn't ideal.)

Ideally we could instead have saplings grow using the same `otherBlockChance`, `vinesGrowthChance`, `mossGrowthChance`, and maybe even `hemisphere` that would be used at world gen.
(Still would want saplings to grow with `skipForestFloor = true` so that they don't excessively modify surrounding ground.)
