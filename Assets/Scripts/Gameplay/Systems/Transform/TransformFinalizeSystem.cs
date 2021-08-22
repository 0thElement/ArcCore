using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomTransformSystemGroup)), UpdateAfter(typeof(ScaleAlongTrackSystem))]
    public class TransformFinalizeSystem : FinalizeSystemBase 
    {}
}