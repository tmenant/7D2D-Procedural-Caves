using System.Collections.Generic;
using PrefabVolumes;

public static class PrefabVolumeListAbsExtensions
{
    public static IEnumerable<TVolume> AsIterator<TVolumeList, TVolume>(this PrefabVolumeListAbs<TVolumeList, TVolume> volumes) where TVolumeList : PrefabVolumeListAbs<TVolumeList, TVolume> where TVolume : PrefabVolumeAbs<TVolume>, new()
    {
        for (int i = 0; i < volumes.Count; i++)
        {
            yield return volumes[i];
        }
    }

    public static List<TVolume> ToList<TVolumeList, TVolume>(this PrefabVolumeListAbs<TVolumeList, TVolume> volumes) where TVolumeList : PrefabVolumeListAbs<TVolumeList, TVolume> where TVolume : PrefabVolumeAbs<TVolume>, new()
    {
        var result = new List<TVolume>(volumes.Count);

        for (int i = 0; i < volumes.Count; i++)
        {
            result[i] = volumes[i];
        }

        return result;
    }
}
