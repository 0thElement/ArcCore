namespace ArcCore.Gameplay.Objects.Particle
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