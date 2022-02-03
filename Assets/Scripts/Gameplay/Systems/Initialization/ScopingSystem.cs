using Unity.Entities;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomInitializationSystemGroup)), UpdateAfter(typeof(InitSetupSystem))]
    public class ScopingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.Conductor.receptorTime;
            var commandBuffer = PlayManager.CommandBuffer;

            Entities.WithNone<WithinJudgeRange, PastJudgeRange, BypassJudgeScoping>().ForEach(

                    (Entity entity, in ChartTime chartTime) =>

                    {
                        if (Mathf.Abs(currentTime - chartTime.value) <= Constants.LostWindow)
                        {
                            commandBuffer.AddComponent<WithinJudgeRange>(entity);
                        }
                    }

                ).Run();

            Entities.WithAll<Disabled>().WithNone<PastJudgeRange>().ForEach(

                    (Entity entity, in AppearTime appearTime) =>

                    {
                        if (currentTime > appearTime.value)
                            commandBuffer.RemoveComponent<Disabled>(entity);
                    }

                ).Run();
        }
    }
}