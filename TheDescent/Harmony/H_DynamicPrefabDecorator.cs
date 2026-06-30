using HarmonyLib;


[HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
public class DynamicPrefabDecorator_DecorateChunk
{
    // run cave generation after prefabs spawn, to allow caves digging into prefabs
    public static void Postfix(Chunk _chunk)
    {
        if (CaveGenerator.isEnabled && !GameUtils.IsPlaytesting())
        {
            CaveGenerator.GenerateCaveChunk(_chunk);
        }
    }
}


/// <summary>
/// Prevents to copy prefab heightmap from ungerground prefabs.
/// Prevents a bug where the heightmap near an underground prefab sticks to the prefab top
/// </summary>
[HarmonyPatch(typeof(DynamicPrefabDecorator), "copyPrefabsIntoHeightMap")]
public class DynamicPrefabDecorator_copyPrefabsIntoHeightMap
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