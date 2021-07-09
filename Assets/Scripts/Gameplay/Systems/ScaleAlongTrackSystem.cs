using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;
using ArcCore.Gameplay.Components;

namespace ArcCore.Gameplay.Systems
{
    public class ScaleAlongTrackSystem : SystemBase
    {
        public static ScaleAlongTrackSystem Instance { get; private set; }
        protected override void OnCreate()
        {
            Instance = this;
        }
        protected override void OnUpdate()
        {
            Entities.ForEach(

                (ref NonUniformScale scale, in Translation translation, in BaseZScale baseScale) =>
                {
                    scale.Value.z = baseScale.value - translation.Value.z / 100;
                }
            ).Schedule();
        }
    }
}