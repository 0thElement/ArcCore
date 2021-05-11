using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore;
using ArcCore.Components;
using ArcCore.Behaviours;
using ArcCore.Components.Tags;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(ChunkScopingSystem))]
public class JudgeEntitiesScopingSystem : SystemBase
{
    EndInitializationEntityCommandBufferSystem commandBufferSystem;
    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        int currentTime = Conductor.Instance.receptorTime;
        var commandBuffer = commandBufferSystem.CreateCommandBuffer();

        Entities.WithNone<WithinJudgeRange, PastJudgeRange>().ForEach(

                (Entity entity, in ChartTime chartTime) 

                    => 

                {
                    if (currentTime >= chartTime.value - Constants.LostWindow)
                    {
                        commandBuffer.AddComponent<WithinJudgeRange>(entity);
                    }
                }

            ).Run();
    }
}
//For testing later
// public class JudgeEntitiesScopingSystem : SystemBase
// {
//     EndInitializationEntityCommandBufferSystem commandBufferSystem;
//     protected override void OnCreate()
//     {
//         commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
//     }
//     protected override void OnUpdate()
//     {
//         int currentTime = Conductor.Instance.receptorTime;
//         var commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();

//         Entities.WithNone<WithinJudgeRangeTag>()
//             .ForEach((Entity entity, int entityInQueryIndex, in ChartTime chartTime, in AppearTime appearTime) => 
//             {
//                 if (currentTime >= appearTime.Value)
//                 {
//                     commandBuffer.AddComponent<WithinJudgeRangeTag>(entityInQueryIndex, entity);
//                 }
//             }).ScheduleParallel();

//         commandBufferSystem.AddJobHandleForProducer(this.Dependency);
//     }
// }