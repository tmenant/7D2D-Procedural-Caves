using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;


public class CmdWorldGen : CmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<CmdWorldGen>();

    private const string worldPath = "../ignore/Pregen06k01";

    private DynamicPrefabDecorator dynamicPrefabDecorator;

    private PrefabCache prefabCache;

    public List<PrefabInstance> AllPrefabs => dynamicPrefabDecorator.allPrefabs;

    public override string[] GetCommands()
    {
        return new string[] { "worldgen" };
    }

    public override void Execute(List<string> args)
    {
        prefabCache = new PrefabCache();
        dynamicPrefabDecorator = new DynamicPrefabDecorator(prefabCache);

        string xmlPath = Path.Combine(worldPath, "prefabs.xml");

        if (!File.Exists(xmlPath))
        {
            logger.Warning($"prefabs.xml not found: '{xmlPath}'");
            return;
        }

        var document = XDocument.Parse(File.ReadAllText(xmlPath));

        foreach (XElement item in document.XPathSelectElements("//decoration"))
        {
            ParsePrefab(item);
            try
            {
            }
            catch (Exception ex2)
            {
                Log.Error("Loading prefabs xml file for level '" + Path.GetFileName(worldPath) + "': " + ex2.Message);
            }
        }

        dynamicPrefabDecorator.SortPrefabs();

        logger.Info($"prefabs loaded: {AllPrefabs.Count}");
    }

    private void ParsePrefab(XElement item)
    {
        if (!item.HasAttribute("name"))
            return;

        string name = item.GetAttribute("name");

        Vector3i vector3i = ParseVector(item.GetAttribute("position"));
        StringParsers.TryParseBool(item.GetAttribute("y_is_groundlevel"), out var y_is_groundlevel);

        byte rotation = 0;
        if (item.HasAttribute("rotation"))
        {
            rotation = byte.Parse(item.GetAttribute("rotation"));
        }

        // Prefab prefabRotated = PrefabLoaderTest.LoadPrefabs(name, rotation);
        // if (prefabRotated == null)
        // {
        //     Log.Warning("Could not load prefab '" + name + "'. Skipping it");
        //     return;
        // }

        // if (y_is_groundlevel)
        // {
        //     vector3i.y += prefabRotated.yOffset;
        // }

        // if (prefabRotated.bTraderArea && SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        // {
        //     dynamicPrefabDecorator.AddTrader(new TraderArea(vector3i, prefabRotated.size, prefabRotated.TraderAreaProtect, prefabRotated.TeleportVolumeList));
        // }

        // PrefabInstance prefabInstance = new PrefabInstance(AllPrefabs.Count, prefabRotated.location, vector3i, rotation, prefabRotated, 0);
        // dynamicPrefabDecorator.AddWorldPrefab(prefabInstance, prefabInstance.prefab.HasQuestTag());
    }

    // public Prefab GetPrefabRotated(string _name, int _rotation, DynamicPrefabDecorator dpd)
    // {
    //     _rotation &= 3;
    //     lock (prefabCache)
    //     {
    //         if (dpd.prefabCache.prefabCacheRotations.TryGetValue(_name, out var value))
    //         {
    //             if (value[_rotation] != null)
    //             {
    //                 return value[_rotation];
    //             }
    //         }
    //         else
    //         {
    //             value = new Prefab[4];
    //             prefabCacheRotations[_name] = value;
    //         }

    //         Prefab prefab = GetPrefab(_name, _applyMapping, _fixChildblocks && _rotation == 0, _allowMissingBlocks, _skipBlockData);
    //         if (prefab == null)
    //         {
    //             return null;
    //         }

    //         if (_rotation > 0)
    //         {
    //             prefab = prefab.Clone(_sharedData: true);
    //             prefab.RotateY(_bLeft: true, _rotation);
    //         }

    //         value[_rotation] = prefab;
    //         return prefab;
    //     }
    // }

    private static Vector3i ParseVector(string str)
    {
        var value = str.Split(',');

        return new Vector3i(
            int.Parse(value[0].Trim()),
            int.Parse(value[1].Trim()),
            int.Parse(value[2].Trim())
        );
    }
}