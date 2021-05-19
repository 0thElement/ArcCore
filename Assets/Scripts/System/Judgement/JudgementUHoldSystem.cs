using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.Utility;
using ArcCore.Math;

[UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(JudgementMinSystem))]
public class JudgementUHoldSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        int currentTime = Conductor.Instance.receptorTime;
        int maxPureCount = ScoreManager.Instance.maxPureCount,
            currentCount = ScoreManager.Instance.currentCombo;
        NativeQuadArr<int> tracksHeld = InputManager.Instance.tracksHeld;

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithNone<HoldLocked>().ForEach(
            (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane lane) =>
            { 
                if(chartIncrTime.time > currentTime - Constants.FarWindow && tracksHeld[lane.lane] > 0)
                {
                    if(!chartIncrTime.UpdateJudgePointCache(currentTime, out int count))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        //this ideology might be problematic! (doubling chunk count again)
                    }

                    maxPureCount += count;
                    currentCount += count;
                }
            }
        ).Run();

        commandBuffer.Playback(EntityManager);
        ScoreManager.Instance.maxPureCount = maxPureCount;
    }
}