using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomInitializationSystemGroup)), UpdateAfter(typeof(JudgeEntitiesScopingSystem))]
    public class InitFinalizeSystem : FinalizeSystemBase 
    {}
}