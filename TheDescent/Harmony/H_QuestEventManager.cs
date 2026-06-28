using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(QuestEventManager), "SetupTraderPrefabList")]
public static class QuestEventManager_SetupTraderPrefabList
{
    public static bool Prefix(QuestEventManager __instance, TraderArea area)
    {
        var logger = Logging.CreateLogger("TheDescent.H_QuestEventManager");

        if (__instance.TraderPrefabList.ContainsKey(area))
        {
            return false;
        }

        Vector3 a = area.Position.ToVector3();
        List<PrefabInstance> prefabInstances = new List<PrefabInstance>();
        GameManager.Instance.GetDynamicPrefabDecorator().GetPOIPrefabs(prefabInstances);
        List<QuestEventManager.PrefabListData> prefabData = new List<QuestEventManager.PrefabListData>();

        var prefabListData1 = new QuestEventManager.PrefabListData();
        var prefabListData2 = new QuestEventManager.PrefabListData();
        var prefabListData3 = new QuestEventManager.PrefabListData();

        prefabData.Add(prefabListData1);
        prefabData.Add(prefabListData2);
        prefabData.Add(prefabListData3);

        for (int i = 0; i < prefabInstances.Count; i++)
        {
            var prefabInstance = prefabInstances[i];
            var distance = Vector3.Distance(a, prefabInstance.boundingBoxPosition);

            if (prefabInstance.prefab.tags.Test_AnySet(CaveTags.tagUnderground))
            {
                logger.Warning($"Skip quest for underground poi: '{prefabInstance.name}'");
                continue;
            }

            if (distance <= 500f)
            {
                prefabListData1.AddPOI(prefabInstances[i]);
            }
            else if (distance <= 1500f)
            {
                prefabListData2.AddPOI(prefabInstances[i]);
            }
            else
            {
                prefabListData3.AddPOI(prefabInstances[i]);
            }
        }

        __instance.TraderPrefabList.Add(area, prefabData);
        return false;
    }
}