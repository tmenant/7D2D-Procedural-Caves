using PrefabVolumes;

public static class MarkerExtensions
{
    public static bool IsCaveMarker(this Marker marker)
    {
        return marker.tags.Test_AnySet(CaveTags.tagCaveMarker);
    }
}