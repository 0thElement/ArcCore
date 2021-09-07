using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomInitializationSystemGroup), OrderFirst = true)]
    public class InitSetupSystem : SetupSystemBase
    {}
}