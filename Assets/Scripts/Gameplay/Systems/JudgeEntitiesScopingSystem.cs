using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Gameplay;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components.Tags;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ChunkScopingSystem))]
    public class JudgeEntitiesScopingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            int currentTime = Conductor.Instance.receptorTime;
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities.WithNone<WithinJudgeRange, PastJudgeRange>().ForEach(

                    (Entity entity, in ChartTime chartTime) =>

                    {
                        if (currentTime + Constants.LostWindow >= chartTime.value)
                        {
                            commandBuffer.AddComponent<WithinJudgeRange>(entity);
                        }
                    }

                ).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}