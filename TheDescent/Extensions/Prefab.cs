using System.Collections.Generic;
using PrefabVolumes;

public static class PrefabExtensions
{
    public static IEnumerable<Marker> GetPOIMarkers(this Prefab prefab)
    {
        return prefab.MarkerVolumeList.AsIterator();
    }
}