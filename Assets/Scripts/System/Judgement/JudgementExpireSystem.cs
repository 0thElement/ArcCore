﻿using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.Utility;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class JudgementExpireSystem : SystemBase
{
    public static ParticleBuffer particleBuffer;

    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        int lostCount = ScoreManager.Instance.lostCount,
            currentCombo = ScoreManager.Instance.currentCombo,
            currentTime = Conductor.Instance.receptorTime;

        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        var particleBuffer = new ParticleBuffer(Allocator.Persistent);

        //- TAPS -//
        Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime, EntityReference>().ForEach(
            (Entity en, in ChartTime chartTime, in ChartLane cl) => {
                if(currentTime - Constants.FarWindow > chartTime.value)
                {
                    commandBuffer.DisableEntity(en);
                    commandBuffer.AddComponent<PastJudgeRange>(en);
                    lostCount++;
                    currentCombo = 0;

                    particleBuffer.PlayTapParticle(
                        new float2(Conversion.TrackToX(cl.lane), 1),
                        ParticleCreator.JudgeType.Lost,
                        ParticleCreator.JudgeDetail.None
                    );
                }
            }
        ).Run();

        //- ARCTAPS -//
        Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime>().ForEach(
            (Entity en, in ChartTime chartTime, in EntityReference enRef, in ChartPosition cp) => {
                if (currentTime - Constants.FarWindow > chartTime.value)
                {
                    commandBuffer.DisableEntity(enRef.value);
                    commandBuffer.DisableEntity(en);
                    commandBuffer.AddComponent<PastJudgeRange>(en);
                    lostCount++;
                    currentCombo = 0;

                    particleBuffer.PlayTapParticle(
                        cp.xy,
                        ParticleCreator.JudgeType.Lost,
                        ParticleCreator.JudgeDetail.None
                    );
                }
            }
        ).Run();

        //- HOLDS -//
        Entities.WithAll<WithinJudgeRange, ChartTime>().ForEach(
            (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane cl) => {
                if (currentTime - Constants.FarWindow > chartIncrTime.time)
                {
                    if (!chartIncrTime.UpdateJudgePointCache(currentTime, out int count))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en); 
                        //this ideology might be problematic! (doubling chunk count again)
                    }
                    lostCount += count;
                    currentCombo = 0;
                    // particleBuffer.PlayHoldParticle(cl.lane - 1, false);
                }
            }
        ).Run();

        Entities.WithAll<ChartTime>().ForEach(
            (Entity en, in ChartIncrTime chartIncrTime, in ChartLane cl) => {
                if (currentTime - Constants.FarWindow > chartIncrTime.endTime)
                {
                    commandBuffer.AddComponent<Disabled>(en);
                    particleBuffer.DisableLaneParticle(cl.lane - 1);
                }
            }
        ).Run();

        //- ARCS -//
        //...

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();

        ScoreManager.Instance.lostCount = lostCount;
        ScoreManager.Instance.currentCombo = currentCombo;

        JudgementExpireSystem.particleBuffer = particleBuffer;
    }
}