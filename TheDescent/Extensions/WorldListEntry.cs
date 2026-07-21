using System.IO;

public static class WorldListEntryExtensions
{
    public static bool HasCave(this XUiC_WorldList.WorldListEntry worldEntry)
    {
        if (worldEntry == null)
            return false;

        var worldPath = worldEntry.Location.FullPath;
        var caveMapPath = Path.Combine(worldPath, "cavemap");

        return Directory.Exists(caveMapPath);
    }
}