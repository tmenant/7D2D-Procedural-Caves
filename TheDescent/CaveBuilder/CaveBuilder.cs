using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

using Random = System.Random;
using System.Threading.Tasks;

public class CaveBuilder
{
    public class Settings
    {
        public bool GenerateWater => caveWater != WorldBuilder.GenerationSelections.None;

        public bool GenerateCaves => caveNetworks != WorldBuilder.GenerationSelections.None;

        public float terrainOffset = 50;

        public WorldBuilder.GenerationSelections caveNetworks = WorldBuilder.GenerationSelections.Default;

        public WorldBuilder.GenerationSelections caveEntrances = WorldBuilder.GenerationSelections.Default;

        public WorldBuilder.GenerationSelections caveWater = WorldBuilder.GenerationSelections.Default;
    }

    private static readonly Logging.Logger logger = Logging.CreateLogger<CaveBuilder>();

    private CaveMap cavemap;

    private WorldBuilder worldBuilder;

    public CavePrefabManager cavePrefabManager;

    public CaveEntrancesPlanner caveEntrancesPlanner;

    private IRawHeightMap heightMap;

    private PrefabManager PrefabManager => worldBuilder.PrefabManager;

    private int worldSize;

    public readonly Settings settings;

    public readonly string caveTempDir = $"{GameIO.GetUserGameDataDir()}/temp";

    public CaveBuilder() { }

    public CaveBuilder(WorldBuilder worldBuilder, Settings settings)
    {
        this.worldBuilder = worldBuilder;
        this.worldSize = worldBuilder.WorldSize;
        this.settings = settings;

        cavemap = new CaveMap(worldSize);
        cavePrefabManager = new CavePrefabManager(worldBuilder);
        caveEntrancesPlanner = new CaveEntrancesPlanner(cavePrefabManager);
        heightMap = new RawHeightMap(worldBuilder);
    }

    public void Cleanup()
    {
        cavemap.Cleanup();
        cavePrefabManager.Cleanup();

        cavemap = null;
        cavePrefabManager = null;
        caveEntrancesPlanner = null;
        heightMap = null;
    }

    private Task StartRoomsTask(CavePrefabManager cavePrefabManager)
    {
        var roomBlock = new CaveBlock()
        {
            isRoom = true,
        };

        var task = new Task(() =>
        {
            foreach (var caveRoom in cavePrefabManager.CaveRooms)
            {
                cavemap.AddBlocks(caveRoom.GetBlocks(), roomBlock.rawData);
            }
        });

        logger.Info($"Start cave rooms thread");

        task.Start();

        return task;
    }

    private void SpawnNaturalEntrances()
    {
        foreach (var prefab in cavePrefabManager.Prefabs.Where(prefab => prefab.isNaturalEntrance))
        {
            var center = prefab.position + Vector3i.one;
            var blocks = CaveTunnel.CreateNaturalEntrance(center, heightMap);

            cavemap.AddBlocks(blocks);
            cavemap.SetRope(center);
        }
    }

    public void GenerateCaveMap()
    {
        if (worldBuilder.IsCanceled)
            return;

        var timer = ProfilingUtils.StartTimer();
        var memoryBefore = GC.GetTotalMemory(true);

        caveEntrancesPlanner.SpawnNaturalEntrances(worldBuilder);

        worldBuilder.SetTaskMessage("Spawning cave prefabs...");

        Random random = new Random(worldBuilder.Seed + worldSize);

        cavePrefabManager.AddUsedCavePrefabs(PrefabManager.UsedPrefabsWorld, worldSize);
        cavePrefabManager.SpawnUnderGroundPrefabs(worldSize / 5, random, heightMap);
        cavePrefabManager.SpawnCaveRooms(1000, random, heightMap);

        worldBuilder.SetTaskMessage("Culterize surface prefabs...");

        cavePrefabManager.AddSurfacePrefabs(PrefabManager.UsedPrefabsWorld);

        logger.Debug($"Prefab timer: {timer.ElapsedMilliseconds / 1000:F1}s");

        worldBuilder.SetTaskMessage("Setup cave network...");

        var caveGraph = new Graph(cavePrefabManager.Prefabs, worldSize);
        var subLists = CaveUtils.SplitList(caveGraph.Edges.ToList(), 6);
        var localMinimas = new HashSet<CaveBlock>();
        var lockObject = new object();
        int index = 0;

        logger.Debug($"Graph timer: {timer.ElapsedMilliseconds / 1000:F1}ms");

        worldBuilder.SetTaskMessage("Start tunneling threads...");

        var tasks = new List<Task>()
        {
            StartRoomsTask(cavePrefabManager),
        };

        foreach (var edgeList in subLists)
        {
            var thread = new Task(() =>
            {
                foreach (var edge in edgeList)
                {
                    string message = $"Cave tunneling: {100f * index++ / caveGraph.Edges.Count:F0}% ({index} / {caveGraph.Edges.Count})";

                    if (worldBuilder.IsCanceled)
                        return;

                    var start = edge.node1;
                    var target = edge.node2;

                    var tunnel = new CaveTunnel(edge, cavePrefabManager, heightMap, worldSize, worldBuilder.Seed);

                    cavemap.AddTunnel(tunnel);

                    lock (lockObject)
                    {
                        localMinimas.UnionWith(tunnel.LocalMinimas);
                    }
                }
            });

            thread.Start();
            tasks.Add(thread);
        }

        while (true)
        {
            bool isThreadAlive = false;
            foreach (var th in tasks)
            {
                if (!th.IsCompleted)
                {
                    isThreadAlive = true;
                    break;
                }
            }

            if (isThreadAlive)
            {
                worldBuilder.SetTaskMessage($"Cave tunneling {100f * cavemap.TunnelsCount / caveGraph.Edges.Count:F0}%");
            }
            else
            {
                break;
            }
        }

        if (settings.GenerateWater)
        {
            cavemap.GenerateWater(cavePrefabManager, worldBuilder, localMinimas, settings.caveWater);
        }

        if (worldBuilder.IsCanceled)
            return;

        SpawnNaturalEntrances();

        // yield return GenerateCavePreview(cavemap);

        logger.Info($"{cavemap.BlocksCount:N0} cave blocks generated, timer: {timer.ElapsedMilliseconds / 1000:F1}s, memory used: {(GC.GetTotalMemory(true) - memoryBefore) / 1_048_576:N1}MB");
    }

    public IEnumerator GenerateCaveFromWorld(PathAbstractions.AbstractedLocation worldLocation)
    {
        logger.Info($"cave generation started for world '{worldLocation.Name}'");

        Task task = new Task(() =>
        {
            GenerateTask(worldLocation);
        });

        task.Start();

        while (!task.IsCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (task.IsFaulted)
        {
            throw task.Exception;
        }

        logger.Info($"task complete");
    }

    private void GenerateTask(PathAbstractions.AbstractedLocation worldLocation)
    {
        var timer = ProfilingUtils.StartTimer();
        var memoryBefore = GC.GetTotalMemory(true);
        var worldDatas = new WorldData(worldLocation);

        worldSize = worldDatas.size;
        cavemap = new CaveMap(worldDatas.size);
        cavePrefabManager = new CavePrefabManager(worldDatas);
        caveEntrancesPlanner = new CaveEntrancesPlanner(cavePrefabManager);
        heightMap = worldDatas.heightMap;

        logger.Info("SpawnNatural entrances...");

        caveEntrancesPlanner.SpawnNaturalEntrances(worldDatas);

        Random random = new Random(worldDatas.seed + worldDatas.size);

        logger.Info("Add prefabs...");
        cavePrefabManager.AddUsedCavePrefabs(worldDatas.prefabs, worldDatas.size);
        cavePrefabManager.SpawnUnderGroundPrefabs(worldDatas.size / 5, random, heightMap);
        cavePrefabManager.SpawnCaveRooms(1000, random, heightMap);
        cavePrefabManager.AddSurfacePrefabs(worldDatas.prefabs);

        CaveUtils.Assert(cavePrefabManager.Prefabs.Count > 0, "No cave prefab was added to the world");

        logger.Debug($"{cavePrefabManager.Prefabs.Count} cave prefabs added to the world.");
        logger.Debug($"Prefab timer: {timer.ElapsedMilliseconds / 1000:F1}s");
        logger.Debug("Setup cave network...");

        var caveGraph = new Graph(cavePrefabManager.Prefabs, worldSize);
        var localMinimas = new HashSet<CaveBlock>();
        var subLists = CaveUtils.SplitList(caveGraph.Edges.ToList(), 6);
        var lockObject = new object();

        logger.Debug($"Graph timer: {timer.ElapsedMilliseconds / 1000:F1}ms");
        logger.Debug("Start tunneling threads...");

        var tasks = new List<Task>()
        {
            StartRoomsTask(cavePrefabManager),
        };

        Parallel.ForEach(caveGraph.Edges, edge =>
        {
            GraphNode startNode = edge.node1;
            GraphNode targetNode = edge.node2;

            var tunnel = new CaveTunnel(edge, cavePrefabManager, heightMap, worldSize, worldDatas.seed);

            cavemap.AddTunnel(tunnel);

            lock (lockObject)
            {
                localMinimas.UnionWith(tunnel.LocalMinimas);
            }
        });

        Task.WaitAll(tasks.ToArray());

        if (settings.GenerateWater)
        {
            cavemap.GenerateWater(cavePrefabManager, worldBuilder, localMinimas, settings.caveWater);
        }

        SpawnNaturalEntrances();
        SaveCaveMap(worldDatas);

        logger.Info($"{cavemap.BlocksCount:N0} cave blocks generated, timer: {timer.ElapsedMilliseconds / 1000:F1}s, memory used: {(GC.GetTotalMemory(true) - memoryBefore) / 1_048_576:N1}MB");
    }

    private IEnumerator GenerateCavePreview(CaveMap caveMap)
    {
        yield return worldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        Color32 regularPrefabColor = new Color32(255, 255, 255, 32);
        Color32 cavePrefabsColor = new Color32(0, 255, 0, 128);
        Color32 caveEntrancesColor = new Color32(255, 255, 0, 255);
        Color32 caveTunnelColor = new Color32(255, 0, 0, 64);

        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), worldSize * worldSize).ToArray();
        var HalfWorldSize = CaveUtils.HalfWorldSize(worldSize);

        foreach (PrefabDataInstance pdi in PrefabManager.UsedPrefabsWorld)
        {
            var prefabColor = regularPrefabColor;

            if (pdi.prefab.Tags.Test_AnySet(CaveTags.tagCaveEntrance))
            {
                prefabColor = caveEntrancesColor;
            }
            else if (pdi.prefab.Tags.Test_AnySet(CaveTags.tagCave))
            {
                prefabColor = cavePrefabsColor;
            }

            var position = pdi.boundingBoxPosition + HalfWorldSize;
            var size = new Vector3i(pdi.boundingBoxSize);

            if (pdi.rotation == 1 || pdi.rotation == 3)
            {
                size = new Vector3i(pdi.boundingBoxSize.z, pdi.boundingBoxSize.y, pdi.boundingBoxSize.x);
            }

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * worldSize;
                pixels[index] = prefabColor;
            }
        }

        var usedTiles = (
            from StreetTile st in worldBuilder.StreetTileMap
            where st.Used
            select st
        ).ToList();

        foreach (var st in usedTiles)
        {
            var position = new Vector3i(st.WorldPosition.x, 0, st.WorldPosition.y);
            var size = new Vector3i(150, 0, 150);

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * worldSize;
                pixels[index] = regularPrefabColor;
            }
        }

        foreach (CaveBlock caveblock in caveMap.GetBlocks())
        {
            var position = caveblock;
            int index = position.x + position.z * worldSize;
            try
            {
                caveTunnelColor.a = (byte)position.y;
                pixels[index] = caveTunnelColor;
            }
            catch (IndexOutOfRangeException)
            {
                logger.Error($"IndexOutOfRangeException: index={index}, position={caveblock}, worldSize={worldSize}");
            }
        }

        var image = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)worldSize, (uint)worldSize, (uint)worldSize * 4);
        var filename = $"{caveTempDir}/cavemap.png";

        if (!Directory.Exists(caveTempDir))
            Directory.CreateDirectory(caveTempDir);

        File.WriteAllBytes(filename, image);

        yield return null;
    }

    public void SaveCaveMap(WorldBuilder worldBuilder)
    {
        cavemap.Save($"{worldBuilder.WorldPath}/cavemap", worldBuilder.WorldSize);
        SaveCavePrefabs(worldBuilder.WorldPath);
    }

    public void SaveCaveMap(WorldData worldDatas)
    {
        cavemap.Save($"{worldDatas.location.FullPath}/cavemap", worldDatas.size);
        SaveCavePrefabs(worldDatas.location.FullPath);
    }

    public void SaveCavePrefabs(string worldPath)
    {
        var document = new XmlDocument();
        var root = document.AddXmlElement("prefabs");

        foreach (var cavePrefab in cavePrefabManager.Prefabs)
        {
            if (cavePrefab.IsUndergroundPrefab())
            {
                var xmlPrefab = root.AddXmlElement("decoration");
                var worldPos = cavePrefab.position - CaveUtils.HalfWorldSize(worldSize);

                xmlPrefab.SetAttribute("type", "model");
                xmlPrefab.SetAttribute("name", cavePrefab.PrefabName);
                xmlPrefab.SetAttribute("position", worldPos.ToString());
                xmlPrefab.SetAttribute("rotation", cavePrefab.rotation.ToString());
            }
        }

        document.Save($"{worldPath}/cavemap/prefabs.xml");

        logger.Info($"{root.ChildNodes.Count} cave prefabs saved");
    }
}
