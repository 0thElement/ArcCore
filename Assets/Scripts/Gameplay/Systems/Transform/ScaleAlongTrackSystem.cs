using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;
using ArcCore.Gameplay.Components;
using UnityEngine;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomTransformSystemGroup)), UpdateAfter(typeof(MovingNotesSystem))]
    public class ScaleAlongTrackSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            Entities.ForEach(

                (ref NonUniformScale scale, in Translation translation, in BaseZScale baseScale) =>
                {
                    scale.Value.z = baseScale.value - translation.Value.z / 100;
                }

            ).Schedule();
        }
    }
}