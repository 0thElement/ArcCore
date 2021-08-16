using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomInitializationSystemGroup), OrderLast = true)]
    public class InitFinalizeSystem : FinalizeSystemBase 
    {}
}