using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomTransformSystemGroup), OrderLast = true)]
    public class TransformFinalizeSystem : FinalizeSystemBase 
    {}
}