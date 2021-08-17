namespace ArcCore.Gameplay.Behaviours
{
    public interface IIndicator
    {
        int endTime {get; set;}
        void Enable();
        void Disable();
        void Update(Unity.Mathematics.float3 position);
        void Destroy();
    }
}