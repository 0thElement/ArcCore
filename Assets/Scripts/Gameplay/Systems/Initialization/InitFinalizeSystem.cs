using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomInitializationSystemGroup)), UpdateAfter(typeof(ScopingSystem))]
    public class InitFinalizeSystem : FinalizeSystemBase 
    {}
}