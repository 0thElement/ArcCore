using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(TransformSystemGroup))]
    public class CustomTransformSystemGroup : ComponentSystemGroup
    {
    }
}