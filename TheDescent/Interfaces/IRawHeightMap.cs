public interface IRawHeightMap
{
    int WorldSize { get; }

    float GetHeight(Vector3i vector);

    float GetHeight(int x, int z);
}