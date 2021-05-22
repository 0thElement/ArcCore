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
            currentCombo = ScoreManager.Instance.currentCombo;
        NTrackArray<int> tracksHeld = InputManager.Instance.tracksHeld;

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithNone<HoldLocked>().ForEach(
            (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane lane) =>
            { 
                if(chartIncrTime.time > currentTime - Constants.FarWindow && tracksHeld[lane.lane] > 0)
                {
                    if(!chartIncrTime.UpdateJudgePointCachePure(currentTime, out int count))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        //this ideology might be problematic! (doubling chunk count again)
                    }

                    maxPureCount += count;
                    currentCombo += count;
                }
            }
        ).Run();

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
        ScoreManager.Instance.maxPureCount = maxPureCount;
        ScoreManager.Instance.currentCombo = currentCombo;
    }
}