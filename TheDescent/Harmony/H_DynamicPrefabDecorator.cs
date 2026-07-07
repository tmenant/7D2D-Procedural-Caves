using System.Collections;
using System.IO;
using HarmonyLib;


/// <summary>
/// run cave generation after prefabs spawn, to allow caves digging into prefabs
/// </summary>
[HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
public class H_DynamicPrefabDecorator_DecorateChunk
{
    public static void Postfix(Chunk _chunk)
    {
        if (CaveGenerator.isEnabled && !GameUtils.IsPlaytesting())
        {
            CaveGenerator.GenerateCaveChunk(_chunk);
        }
    }
}


/// <summary>
/// Prevents underground prefabs from writing to the world heightmap.
/// Fixes a bug where surface terrain would snap or flatten to the roof of underground structures.
/// </summary>
[HarmonyPatch(typeof(DynamicPrefabDecorator), "copyPrefabsIntoHeightMap")]
public class H_DynamicPrefabDecorator_copyPrefabsIntoHeightMap
{
    public static bool Prefix(PrefabInstance _pi, int _heightMapWidth, int _heightMapHeight, IBackedArray<ushort> _heightData, int _heightMapScale, ushort[] _topTextures = null)
    {
        if (_pi.prefab.tags.Test_AnySet(CaveTags.tagCaveUnderground))
        {
            return false;
        }

        return true;
    }
}


/// <summary>
/// Intercepts the prefabs loading process to load additional cave prefabs
/// (registered in the cavemap folder) after the default world prefabs are loaded.
/// </summary>
[HarmonyPatch(typeof(DynamicPrefabDecorator))]
[HarmonyPatch(nameof(DynamicPrefabDecorator.Load))]
public class H_DynamicPrefabDecorator_Load
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<H_DynamicPrefabDecorator_Load>();

    private static bool isPrefixRunning = false;

    [HarmonyPrefix]
    public static bool Prefix(string _path, bool _skipBlockData, DynamicPrefabDecorator __instance, ref IEnumerator __result)
    {
        if (isPrefixRunning)
        {
            return true;
        }

        __result = LoadAllPrefabs(_path, _skipBlockData, __instance);

        return false;
    }

    public static IEnumerator LoadAllPrefabs(string worldPath, bool skipBlockData, DynamicPrefabDecorator prefabDecorator)
    {
        isPrefixRunning = true;

        try
        {
            yield return prefabDecorator.Load(worldPath, skipBlockData);

            string cavemapFolder = Path.Combine(worldPath, "cavemap");
            string prefabsFilePath = Path.Combine(cavemapFolder, "prefabs.xml");

            if (!File.Exists(prefabsFilePath))
            {
                logger.Warning($"file not found: '{prefabsFilePath}'");
                yield break;
            }

            int prefabsCountBefore = prefabDecorator.allPrefabs.Count;

            yield return prefabDecorator.Load(cavemapFolder, skipBlockData);

            // Incrément prefabs ids to avoid conflits
            for (int i = prefabsCountBefore; i < prefabDecorator.allPrefabs.Count; i++)
            {
                prefabDecorator.allPrefabs[i].id += prefabsCountBefore;
            }

            logger.Info($"{prefabDecorator.allPrefabs.Count - prefabsCountBefore} cave prefabs registered.");
        }
        finally
        {
            isPrefixRunning = false;
        }
    }
}
