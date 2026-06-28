using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(PrefabManagerData), "LoadPrefabs")]
public static class H_PrefabManagerData
{
    // prefix to be runned when caveGeneration is disabled, to filter underground prefabs
    public static bool Prefix(PrefabManagerData __instance)
    {
        LoadPrefabs(__instance);
        return false;
    }

    public static void LoadPrefabs(PrefabManagerData PrefabManagerData, CavePrefabManager cavePrefabManager = null)
    {
        if (PrefabManagerData.AllPrefabDatas.Count != 0)
        {
            return;
        }
        MicroStopwatch microStopwatch = new MicroStopwatch(_bStart: true);
        List<PathAbstractions.AbstractedLocation> availablePathsList = PathAbstractions.PrefabsSearchPaths.GetAvailablePathsList(null, _ignoreDuplicateNames: true);

        // PATCH: add underground prefab filter + create tag to prevent the vanilla rwg from selecting wilderness cave entrances
        FastTags<TagGroup.Poi> tagFilter = FastTags<TagGroup.Poi>.Parse("navonly,devonly,testonly,biomeonly,underground");

        for (int i = 0; i < availablePathsList.Count; i++)
        {
            PathAbstractions.AbstractedLocation location = availablePathsList[i];
            int num = location.Folder.LastIndexOf("/Prefabs/");
            if (num >= 0 && location.Folder.Substring(num + 8, 5).EqualsCaseInsensitive("/test"))
            {
                continue;
            }
            PrefabData prefabData = PrefabData.LoadPrefabData(location);
            try
            {
                // PATCH START //
                if (prefabData.Tags.Test_AllSet(CaveTags.tagCaveTrader))
                {
                    Logging.Warning($"Skip underground trader '{prefabData.Name}'");
                    continue;
                }

                cavePrefabManager?.TryCacheCavePrefab(prefabData);

                if (!prefabData.Tags.Test_AnySet(tagFilter))
                {
                    PrefabManagerData.AllPrefabDatas[location.Name.ToLower()] = prefabData;
                }
                // PATCH END //
            }
            catch (System.Exception)
            {
                Log.Error("Could not load prefab data for " + location.Name);
            }
        }
        Log.Out("LoadPrefabs {0} of {1} in {2} s", PrefabManagerData.AllPrefabDatas.Count, availablePathsList.Count, (float)microStopwatch.ElapsedMilliseconds * 0.001f);
    }

}


[HarmonyPatch(typeof(PrefabManager), "getScoreForPrefab")]
public static class H_PrefabManager_getScoreForPrefab
{
    public static void Postfix(PrefabData prefab, Vector2i center, ref float __result)
    {
        if (prefab.Tags.Test_AllSet(CaveTags.tagCave))
        {
            __result *= CaveConfig.prefabScoreMultiplier;
        }
    }
}