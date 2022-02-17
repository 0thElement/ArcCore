using Unity.Entities;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Utilities;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomInitializationSystemGroup)), UpdateAfter(typeof(InitSetupSystem))]
    public class ScopingSystem : SystemBase
    {
        private EntityQuery chunkQuery;
        private int chunkIndexCache = 0;

        protected override void OnCreate()
        {
            chunkQuery = GetEntityQuery(typeof(ChunkAppearTime), typeof(Disabled));
            chunkIndexCache = 0;
        }
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.Conductor.receptorTime;
            var commandBuffer = PlayManager.CommandBuffer;

            //Cheating ECS and forcing this system to update every frame
            Entities
                .WithAll<Prefab, Disabled>()
                .ForEach((in ChartLane _)=>{return;}).Run();

            Entities.WithNone<WithinJudgeRange, PastJudgeRange, BypassJudgeScoping>().ForEach(

                    (Entity entity, in ChartTime chartTime) =>

                    {
                        if (Mathf.Abs(currentTime - chartTime.value) <= Constants.LostWindow)
                        {
                            commandBuffer.AddComponent<WithinJudgeRange>(entity);
                        }
                    }

                ).Run();

            Entities.WithAll<Disabled, ChunkAppeared>().WithNone<PastJudgeRange>().ForEach(

                    (Entity entity, in AppearTime appearTime) =>

                    {
                        if (currentTime > appearTime.value)
                            commandBuffer.RemoveComponent<Disabled>(entity);
                    }

                ).Run();

            List<int> chunkAppearTimes = ScopingChunk.AllChunkAppearTimes;

            while (chunkAppearTimes[chunkIndexCache] <= currentTime)
            {
                chunkQuery.SetSharedComponentFilter(new ChunkAppearTime(chunkAppearTimes[chunkIndexCache]));
                EntityManager.AddComponent<ChunkAppeared>(chunkQuery);
                chunkIndexCache++;
            }
        }
    }
}