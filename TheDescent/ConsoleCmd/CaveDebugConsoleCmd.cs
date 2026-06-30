using System.Collections.Generic;
using System.Linq;


public class CaveDebugConsoleCmd : ConsoleCmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<CaveDebugConsoleCmd>();

    public override string[] getCommands()
    {
        return new string[] { "cavedebug", "cd" };
    }

    public override string getDescription()
    {
        return "cavedebug cd => command line tools for cave debugging";
    }

    public override string getHelp()
    {
        return @"Cave debug commands:
            - sgms [value]: set god mode speed, from the given float value
        ";
    }

    private static void ClusterCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(playerPos);

        if (prefabInstance == null)
        {
            logger.Warning($"[Cluster] no prefab found at position [{playerPos}]");
            return;
        }

        var clusters = BlockClusterizer.Clusterize(prefabInstance);

        logger.Info($"[Cluster] player: [{playerPos}], prefab: [{prefabInstance.boundingBoxPosition}], rotation: {prefabInstance.rotation}, name: '{prefabInstance.name}'");

        if (clusters.Count == 0)
        {
            logger.Warning($"[Cluster] No cluster found.");
            return;
        }

        for (int i = 0; i < clusters.Count; i++)
        {
            clusters[i] = clusters[i].Transform(prefabInstance);
            logger.Info($"[Cluster] {clusters[i].start,18} | {clusters[i].size}");
        }
        logger.Info($"[Cluster] {clusters.Count} clusters found.");

        BlockSelectionUtils.SelectBoxes(clusters);
    }

    private static void PrefabCommand(List<string> _params)
    {
        var playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
        var prefabInstance = GameManager.Instance.World.GetPOIAtPosition(playerPos);

        if (prefabInstance == null)
        {
            logger.Warning($"[Prefab] no prefab found at position [{playerPos}]");
            return;
        }

        var bb = new BoundingBox(prefabInstance.boundingBoxPosition, prefabInstance.boundingBoxSize);

        logger.Info($"[Prefab] '{prefabInstance.name}', start: [{bb.start}], size: [{bb.size}], rotation: {prefabInstance.rotation}");

        BlockSelectionUtils.SelectBox(bb);
    }

    private static void DecorateCommand(List<string> _params)
    {
        var worldPos = BlockSelectionUtils.GetSelectionPosition();

        if (worldPos.Equals(Vector3i.zero))
        {
            logger.Warning("empty selection.");
            return;
        }

        string blockName = GameManager.Instance.World.GetBlock(worldPos).Block.blockName;
        bool isChild = GameManager.Instance.World.GetBlock(worldPos).ischild;
        bool isMultiBlock = GameManager.Instance.World.GetBlock(worldPos).Block.isMultiBlock;

        logger.Info($"'{worldPos}' : isChild: {isChild}, isMulti: {isMultiBlock}, name: {blockName}");

        if (_params.Count == 1)
            return;

        int.TryParse(_params[2], out int rotation);

        var blockID = int.Parse(_params[1]);
        var blockValue = Block.GetBlockValue(blockID);
        var chunk = GameManager.Instance.World.GetChunkFromWorldPos(worldPos) as Chunk;
        var localChunkPos = World.toBlock(worldPos);

        blockValue.rotation = (byte)rotation;

        chunk.SetBlock(
            GameManager.Instance.World,
            localChunkPos.x,
            localChunkPos.y,
            localChunkPos.z,
            blockValue,
            _notifyAddChange: true
        );
    }

    private static void SpawnCommand(List<string> _params)
    {
        CaveConfig.enableCaveSpawn = !CaveConfig.enableCaveSpawn;

        var enabled = CaveConfig.enableCaveSpawn ? "enabled" : "disabled";

        logger.Info($"cave spawn {enabled}");
    }

    private static void SetGodModeSpeed(List<string> _params)
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();

        player.GodModeSpeedModifier = float.Parse(_params[1]);
    }

    private static void MoonScaleCommand(List<string> _params)
    {
        if (_params.Count == 0)
        {
            logger.Error($"Missing argument: 'scale' (float)");
            return;
        }

        if (!float.TryParse(_params[1], out var scale))
        {
            logger.Error($"Invalid argument: '{_params[1]}'");
            return;
        }

        CaveConfig.moonLightScale = scale;
    }

    private static void MarkerCommand(List<string> _params)
    {
        var prefabInstance = GameManager.Instance.World.GetPrimaryPlayer().prefab;

        if (prefabInstance == null)
        {
            logger.Warning($"Player is not inside a prefb");
            return;
        }

        var markers = CaveUtils.GetCaveMarkers(prefabInstance).ToArray();

        if (markers.Length == 0)
            logger.Warning($"No cave marker found in prefab '{prefabInstance.name}'");

        foreach (var bb in markers)
        {
            BlockSelectionUtils.SelectBox(bb);
        }
    }

    private void CmdGetHeight()
    {
        var world = GameManager.Instance.World;
        var playerPos = world.GetPrimaryPlayer().position;

        int px = (int)playerPos.x;
        int pz = (int)playerPos.z;

        logger.Info($"GetHeight: {world.GetHeight(px, pz)}, GetHeightAt: {world.GetHeightAt(px, pz)}");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count == 0)
        {
            logger.Info(getDescription());
            return;
        }

        switch (_params[0].ToLower())
        {
            case "cluster":
                ClusterCommand(_params);
                break;

            case "prefab":
                PrefabCommand(_params);
                break;

            case "deco":
                DecorateCommand(_params);
                break;

            case "spawn":
                SpawnCommand(_params);
                break;

            case "sgms":
                SetGodModeSpeed(_params);
                break;

            case "moon":
                MoonScaleCommand(_params);
                break;

            case "marker":
                MarkerCommand(_params);
                break;

            case "getheight":
            case "height":
            case "gh":
                CmdGetHeight();
                break;

            default:
                logger.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}