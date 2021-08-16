using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomTransformSystemGroup), OrderFirst = true)]
    public class TransformSetupSystem : SetupSystemBase
    {}
}