public class HeightMapConstant : IRawHeightMap
{
    private readonly int worldSize;

    private readonly int height;

    public int WorldSize => worldSize;

    public HeightMapConstant(int height, int worldSize)
    {
        this.worldSize = worldSize;
        this.height = height;
    }

    public float GetHeight(Vector3i vector) => height;

    public float GetHeight(int x, int z) => height;
}