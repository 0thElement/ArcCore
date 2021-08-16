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
    [UpdateInGroup(typeof(CustomInitializationSystemGroup)), UpdateAfter(typeof(ChunkScopingSystem))]
    public class JudgeEntitiesScopingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.Conductor.receptorTime;
            var commandBuffer = PlayManager.CommandBuffer;

            Entities.WithNone<WithinJudgeRange, PastJudgeRange>().ForEach(

                    (Entity entity, in ChartTime chartTime)

                        =>

                    {
                        if (currentTime + Constants.LostWindow >= chartTime.value)
                        {
                            commandBuffer.AddComponent<WithinJudgeRange>(entity);
                        }
                    }

                ).Run();
        }
    }
}