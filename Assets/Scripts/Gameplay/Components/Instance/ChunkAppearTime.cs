using Unity.Entities;

public struct ChunkAppearTime : ISharedComponentData
{
    public int value;

    public ChunkAppearTime(int value)
    {
        this.value = value;
    }
}
