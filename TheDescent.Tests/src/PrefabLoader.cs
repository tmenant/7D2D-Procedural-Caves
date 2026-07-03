using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Runtime.Serialization;
using System.Reflection;
using System.Xml;
using System;
using PrefabVolumes;

public class PrefabLoaderTest
{
    private static readonly string userDataPath = @"C:\Users\menan\AppData\Roaming\7DaysToDie";

    private static string LocalPrefabsDir => Path.Combine(userDataPath, "LocalPrefabs");

    public static Dictionary<string, PrefabData> LoadPrefabs()
    {
        var AllPrefabDatas = new Dictionary<string, PrefabData>();

        foreach (var location in GetPrefabsLocations())
        {
            var prefabData = NewPrefabData(location);

            if (!prefabData.Tags.Test_AnySet(CaveTags.tagCave) || !CavePrefabChecker.IsValid(prefabData))
            {
                continue;
            }

            AllPrefabDatas[prefabData.Name] = prefabData;
        }

        return AllPrefabDatas;
    }

    private static List<PathAbstractions.AbstractedLocation> GetPrefabsLocations(string directory)
    {
        var paths = new List<PathAbstractions.AbstractedLocation>();

        foreach (var filename in Directory.GetFiles(directory))
        {
            string path = Path.Combine(directory, filename);

            if (Path.GetExtension(path) != ".xml")
                continue;

            var loc = new PathAbstractions.AbstractedLocation();
            var objloc = loc as object;
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            CaveUtils.SetField<PathAbstractions.AbstractedLocation>(objloc, "Type", PathAbstractions.EAbstractedLocationType.UserDataPath, bindingFlags);
            CaveUtils.SetField<PathAbstractions.AbstractedLocation>(objloc, "Name", Path.GetFileName(path), bindingFlags);
            CaveUtils.SetField<PathAbstractions.AbstractedLocation>(objloc, "Folder", Path.GetDirectoryName(path), bindingFlags);
            CaveUtils.SetField<PathAbstractions.AbstractedLocation>(objloc, "Extension", Path.GetExtension(path), bindingFlags);
            CaveUtils.SetField<PathAbstractions.AbstractedLocation>(objloc, "FileNameNoExtension", Path.GetFileNameWithoutExtension(path), bindingFlags);
            CaveUtils.SetField<PathAbstractions.AbstractedLocation>(objloc, "IsFolder", false, bindingFlags);

            paths.Add((PathAbstractions.AbstractedLocation)objloc);
        }

        foreach (var dir in Directory.GetDirectories(directory))
        {
            paths.AddRange(GetPrefabsLocations(dir));
        }

        return paths;
    }

    private static List<PathAbstractions.AbstractedLocation> GetPrefabsLocations()
    {
        return GetPrefabsLocations(LocalPrefabsDir);
    }

    private static PrefabData NewPrefabData(PathAbstractions.AbstractedLocation location)
    {
        var properties = ReadXML(location.FullPath);
        var prefabData = new PrefabData(location)
        {
            DuplicateRepeatDistance = ParsePropertyInt("DuplicateRepeatDistance", properties)
        };

        ParsePOIMarkers(prefabData, properties);

        prefabData.location = location;
        prefabData.size = ParseVector(properties["PrefabSize"]);

        if (properties.ContainsKey("YOffset"))
        {
            prefabData.yOffset = int.Parse(properties["YOffset"]);
        }

        if (properties.ContainsKey("Tags"))
        {
            prefabData.Tags = FastTags<TagGroup.Poi>.Parse(properties["Tags"].Replace(" ", ""));
        }
        else
        {
            prefabData.Tags = FastTags<TagGroup.Poi>.none;
        }

        if (properties.ContainsKey("ThemeTags"))
        {
            prefabData.ThemeTags = FastTags<TagGroup.Poi>.Parse(properties["ThemeTags"].Replace(" ", ""));
        }
        else
        {
            prefabData.ThemeTags = FastTags<TagGroup.Poi>.none;
        }

        return prefabData;
    }

    private static void SetField(PrefabData instance, string fieldName, object value)
    {
        var field = typeof(PrefabData).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    private static int ParsePropertyInt(string key, Dictionary<string, string> properties, int @default = 0)
    {
        if (properties.ContainsKey(key))
        {
            return int.Parse(properties[key]);
        }

        return @default;
    }

    private static Vector3i ParseVector(string str)
    {
        var value = str.Split(',');

        return new Vector3i(
            int.Parse(value[0].Trim()),
            int.Parse(value[1].Trim()),
            int.Parse(value[2].Trim())
        );
    }

    private static void ParsePOIMarkers(PrefabData prefab, Dictionary<string, string> properties)
    {
        if (!properties.ContainsKey("POIMarkerSize"))
            return;

        var POIMarkerSize = properties["POIMarkerSize"].Split('#');
        var POIMarkerStart = properties["POIMarkerStart"].Split('#');
        var POIMarkerGroup = properties["POIMarkerGroup"].Split('#');
        var POIMarkerTags = properties["POIMarkerTags"].Split('#');
        var POIMarkerType = properties["POIMarkerType"].Split('#');

        for (int i = 0; i < POIMarkerSize.Length; i++)
        {
            var marker = new Marker
            {
                // groupName = POIMarkerGroup[i],
                size = ParseVector(POIMarkerSize[i]),
                startPos = ParseVector(POIMarkerStart[i]),
                tags = FastTags<TagGroup.Poi>.Parse(POIMarkerTags[i].Replace(" ", ""))
            };

            if (POIMarkerType.Length == POIMarkerSize.Length && Enum.TryParse<Marker.MarkerTypes>(POIMarkerType[i], ignoreCase: true, out var result))
            {
                marker.markerType = result;
            }
            else
            {
                marker.MarkerType = Marker.MarkerTypes.None;
            }

            prefab.POIMarkers.List.Add(marker);
        }
    }

    private static Dictionary<string, string> ReadXML(string path)
    {
        var properties = new Dictionary<string, string>();

        XmlDocument doc = new XmlDocument();

        using (var reader = new StreamReader(path))
        {
            doc.LoadXml(reader.ReadToEnd());
        }

        XmlNodeList propertyNodes = doc.GetElementsByTagName("property");

        foreach (XmlNode node in propertyNodes)
        {
            string name = node.Attributes["name"]?.Value;
            string value = node.Attributes["value"]?.Value;

            if (name != null)
            {
                properties[name] = value;
            }
        }

        return properties;
    }
}

