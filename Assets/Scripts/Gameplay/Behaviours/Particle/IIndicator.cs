namespace ArcCore.Gameplay.Behaviours
{
    public interface IIndicator
    {
        int startTime {get; set;}
        int endTime {get; set;}
        void Enable();
        void Disable();
        void Update(Unity.Mathematics.float3 position);
    }
}