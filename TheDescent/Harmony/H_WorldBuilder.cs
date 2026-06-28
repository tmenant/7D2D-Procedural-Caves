using System.Collections;
using HarmonyLib;
using Unity.Collections;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(WorldBuilder), "GenerateTask")]
public class WorldBuilder_GenerateData
{
    private static WorldBuilder worldBuilder;

    private static CaveBuilder caveBuilder;

    public static bool Prefix(WorldBuilder __instance)
    {
        if (!CaveConfig.generateCaves)
        {
            return true;
        }

        worldBuilder = __instance;

        CaveUtils.Assert(worldBuilder != null, "null world builder");

        GenerateTask();
        return false;
    }

    public static void GenerateTask()
    {
        PatchWaterHeight();

        caveBuilder = new CaveBuilder(worldBuilder);

        worldBuilder.GenerateTerrain();
        bool hasPOIs = worldBuilder.Towns != WorldBuilder.GenerationSelections.None || worldBuilder.Wilderness != WorldBuilder.GenerationSelections.None;
        worldBuilder.PrefabManager.ClearDisplayed();
        if (hasPOIs)
        {
            H_PrefabManagerData.LoadPrefabs(worldBuilder.PrefabManager.prefabManagerData, caveBuilder.cavePrefabManager);

            worldBuilder.PrefabManager.ShufflePrefabData(worldBuilder.Seed);
            worldBuilder.PathingUtils.SetupPathingGrid();
        }

        StoreHeightMaps(out var HeightMap, out var waterDest);
        PatchHeightMaps();

        worldBuilder.InitStreetTiles();
        if (worldBuilder.Towns != WorldBuilder.GenerationSelections.None)
        {
            worldBuilder.TownPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
        }

        ResetHeightMaps(HeightMap, waterDest);

        worldBuilder.GenerateTerrainLast();

        PatchHeightMaps();

        worldBuilder.previewStepOfTask = XUiC_WorldGenerationPreview.PreviewStep.Terrain;
        worldBuilder.POISmoother.SmoothStreetTiles();
        if (worldBuilder.IsCanceled)
        {
            return;
        }

        if (worldBuilder.Wilderness != WorldBuilder.GenerationSelections.None)
        {
            worldBuilder.WildernessPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            worldBuilder.SmoothWildernessTerrain();
        }

        if (worldBuilder.IsCanceled)
        {
            return;
        }

        if (hasPOIs)
        {
            worldBuilder.CalcTownshipsHeightMask();
            worldBuilder.HighwayPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            worldBuilder.TownPlanner.SpawnPrefabs();
        }

        if (worldBuilder.IsCanceled)
        {
            return;
        }

        if (worldBuilder.Wilderness != WorldBuilder.GenerationSelections.None)
        {
            worldBuilder.WildernessPathPlanner.Plan(worldBuilder.Seed);
        }

        int num = 12 - worldBuilder.playerSpawns.Count;
        if (num > 0)
        {
            foreach (StreetTile item in worldBuilder.CalcPlayerSpawnTiles())
            {
                if (worldBuilder.CreatePlayerSpawn(item.WorldPositionCenter, _isFallback: true) && --num <= 0)
                {
                    break;
                }
            }
        }

        if (worldBuilder.IsCanceled)
        {
            return;
        }

        worldBuilder.DrawRoads(worldBuilder.roadDest);
        if (hasPOIs)
        {
            worldBuilder.SetTaskMessage(worldBuilder.messageSmoothRoadTerrain);
            worldBuilder.CalcWindernessPOIsHeightMask(worldBuilder.roadDest);
            worldBuilder.SmoothRoadTerrain(worldBuilder.roadDest, worldBuilder.data.HeightMap, worldBuilder.WorldSize, worldBuilder.Townships);
        }

        caveBuilder.GenerateCaveMap();

        foreach (Path highwayPath in worldBuilder.highwayPaths)
        {
            highwayPath.Cleanup();
        }

        worldBuilder.highwayPaths.Clear();
        foreach (Path wildernessPath in worldBuilder.wildernessPaths)
        {
            wildernessPath.Cleanup();
        }

        worldBuilder.wildernessPaths.Clear();
        worldBuilder.FinalizeWater();
    }

    private static float ClampHeight(float height)
    {
        return CaveConfig.terrainOffset + (255f - CaveConfig.terrainOffset) * height / 255f;
    }

    private static void PatchWaterHeight()
    {
        CaveUtils.SetField<WorldBuilder>(
            worldBuilder,
            "WaterHeight",
            (int)ClampHeight(worldBuilder.WaterHeight) - 1
        );
    }

    private static void PatchHeightMaps()
    {
        for (int i = 0; i < worldBuilder.data.HeightMap.Length; i++)
        {
            worldBuilder.data.HeightMap[i] = ClampHeight(worldBuilder.data.HeightMap[i]);
        }
    }

    private static void StoreHeightMaps(out NativeArray<float> HeightMap, out NativeArray<float> waterDest)
    {
        HeightMap = new NativeArray<float>(worldBuilder.data.HeightMap.Length, Allocator.Persistent);
        waterDest = new NativeArray<float>(worldBuilder.data.waterDest.Length, Allocator.Persistent);

        worldBuilder.data.HeightMap.CopyTo(HeightMap);
        worldBuilder.data.waterDest.CopyTo(waterDest);
    }

    private static void ResetHeightMaps(NativeArray<float> HeightMap, NativeArray<float> waterDest)
    {
        worldBuilder.data.HeightMap.CopyFrom(HeightMap);
        worldBuilder.data.waterDest.CopyFrom(waterDest);
    }

    public static void SaveCaveMap()
    {
        caveBuilder.SaveCaveMap(worldBuilder);
    }

    public static void Cleanup()
    {
        caveBuilder?.Cleanup();
        caveBuilder = null;
    }
}


[HarmonyPatch(typeof(WorldBuilder), "serializeRawHeightmap")]
public static class WorldBuilder_serializeRawHeightmap
{
    public static bool Prefix()
    {
        if (CaveConfig.generateCaves)
        {
            WorldBuilder_GenerateData.SaveCaveMap();
            WorldBuilder_GenerateData.Cleanup();
        }

        return true;
    }
}
