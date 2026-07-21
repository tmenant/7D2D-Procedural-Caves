using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class WorldData
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<WorldData>();

    public readonly PathAbstractions.AbstractedLocation location;

    public readonly GameUtils.WorldInfo worldInfo;

    public readonly List<PrefabDataInstance> prefabs;

    public readonly IRawHeightMap heightMap;

    public readonly string name;

    public readonly int size;

    public readonly int seed;

    public readonly bool[] roadMap;

    public readonly bool[] waterMap;

    public string dtmPath => Path.Combine(location.FullPath, "dtm.raw");

    public string prefabsPath => Path.Combine(location.FullPath, "prefabs.xml");

    public string splat3Path => Path.Combine(location.FullPath, "splat3.png");

    public string splat4Path => Path.Combine(location.FullPath, "splat4.png");

    public WorldData(string worldName)
    {
        this.name = worldName;
        this.location = PathAbstractions.WorldsSearchPaths.GetLocation(worldName);
        this.worldInfo = GameUtils.WorldInfo.LoadWorldInfo(location);
        this.size = worldInfo.WorldSize.x;
        this.seed = GetWorldSeed();
        this.roadMap = ReadTexture(splat3Path);
        this.waterMap = ReadTexture(splat4Path);
        this.prefabs = PrefabLoader.LoadPrefabs(prefabsPath).ToList();
        this.heightMap = new RawHeightMap(dtmPath, size);
    }

    public WorldData(PathAbstractions.AbstractedLocation worldLocation)
    {
        this.name = worldLocation.Name;
        this.location = worldLocation;
        this.worldInfo = GameUtils.WorldInfo.LoadWorldInfo(location);
        this.size = worldInfo.WorldSize.x;
        this.seed = GetWorldSeed();
        this.roadMap = ReadTexture(splat3Path);
        this.waterMap = ReadTexture(splat4Path);
        this.prefabs = PrefabLoader.LoadPrefabs(prefabsPath).ToList();
        this.heightMap = new RawHeightMap(dtmPath, size);
    }

    private bool[] ReadTexture(string path)
    {
        var texture = PNGFile.Load(path);
        int totalPixels = size * size;
        var result = new bool[totalPixels];

        int pixelCount = 0;

        for (int i = 0; i < totalPixels; i++)
        {
            var pixel = texture.pixels[i];

            bool hasColor = (pixel.r | pixel.g | pixel.b) > 0;

            result[i] = hasColor;

            if (hasColor) pixelCount++;
        }

        logger.Debug($"{pixelCount} pixels for '{path}'");

        return result;
    }

    private int GetWorldSeed()
    {
        if (!worldInfo.DynamicProperties.Contains("Seed"))
        {
            return 0;
        }

        return worldInfo.DynamicProperties.GetInt("Seed");
    }

    public void Debug()
    {
        logger.Debug($"name: {name}");
        logger.Debug($"size: {size}");
        logger.Debug($"seed: {seed}");
    }

    public List<StreetTileData> GetStreetTiles()
    {
        var streetTiles = InitStreetTiles().ToList();
        var StreetTileMapSize = size / 150;

        foreach (var st in streetTiles)
        {
            for (int dx = 0; dx < 150; dx++)
            {
                for (int dy = 0; dy < 150; dy++)
                {
                    int x = st.worldPosition.x + dx;
                    int y = st.worldPosition.y + dy;

                    if (roadMap[x + y * size])
                    {
                        st.ContainsRoad = true;
                    }
                }
            }
        }



        return streetTiles;
    }

    public IEnumerable<StreetTileData> InitStreetTiles()
    {
        var StreetTileMapSize = size / 150;

        for (int x = 0; x < StreetTileMapSize; x++)
        {
            for (int y = 0; y < StreetTileMapSize; y++)
            {
                yield return new StreetTileData(new Vector2i(x, y), StreetTileMapSize);
            }
        }
    }

    public float GetHeight(int x, int z)
    {
        return heightMap.GetHeight(x, z);
    }

    public int GetHeightCeil(int x, int z)
    {
        return Mathf.CeilToInt(GetHeight(x, z));
    }

    public bool IsWater(int x, int z)
    {
        return waterMap[x + z * size];
    }
}

public class StreetTileData
{
    public Vector2i gridPosition;

    public Vector2i worldPosition;

    public Vector2i worldPositionCenter => worldPosition + Vector2i.one * 75;

    public bool overlapsRadiation = false;

    public bool ContainsRoad = false;

    public StreetTileData(Vector2i position, int streetTileMapSize)
    {
        this.gridPosition = position;
        this.worldPosition = gridPosition * 150;
        this.overlapsRadiation = position.x < 1 || position.x >= streetTileMapSize - 1 || position.y < 1 || position.y >= streetTileMapSize - 1;
    }

    public bool HasPrefabs(IEnumerable<PrefabDataInstance> prefabs)
    {
        var worldPos = new Vector3i(worldPosition.x, 0, worldPosition.y);
        var tileSize = new Vector3i(150, 0, 150);

        foreach (var prefab in prefabs)
        {
            if (CaveUtils.OverLaps2D(worldPos, tileSize, prefab.boundingBoxPosition, prefab.boundingBoxSize))
            {
                return true;
            }
        }

        return false;
    }
}