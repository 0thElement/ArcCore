namespace ArcCore.Gameplay.Behaviours
{
    public interface IIndicator
    {
        int EndTime {get; set;}
        void Enable();
        void Disable();
        void Update(Unity.Mathematics.float3 position);
        void Destroy();
    }
}