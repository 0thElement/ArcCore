using ArcCore;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Gameplay.Data;
using Unity.Mathematics;
using ArcCore.Gameplay.Utility;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(ParticleJudgeSystem))]
    public class ExpirableJudgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!GameState.isChartMode) return;

            int lostCount = ScoreManager.Instance.lostCount,
            currentCombo = ScoreManager.Instance.currentCombo,
            currentTime = Conductor.Instance.receptorTime;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var particleBuffer = ParticleJudgeSystem.particleBuffer;

            //- TAPS -//
            Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime, EntityReference>().ForEach(
                (Entity en, in ChartTime chartTime, in ChartLane cl) =>
                {
                    if (currentTime - Constants.FarWindow > chartTime.value)
                    {
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        lostCount++;
                        currentCombo = 0;

                        particleBuffer.PlayTapParticle(
                            new float2(Conversion.TrackToX(cl.lane), 1),
                            ParticlePool.JudgeType.Lost,
                            ParticlePool.JudgeDetail.None
                        );
                    }
                }
            ).Run();

            //- ARCTAPS -//
            Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime>().ForEach(
                (Entity en, in ChartTime chartTime, in EntityReference enRef, in ChartPosition cp) =>
                {
                    if (currentTime - Constants.FarWindow > chartTime.value)
                    {
                        commandBuffer.DisableEntity(enRef.value);
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        lostCount++;
                        currentCombo = 0;

                        particleBuffer.PlayTapParticle(
                            cp.xy,
                            ParticlePool.JudgeType.Lost,
                            ParticlePool.JudgeDetail.None
                        );
                    }
                }
            ).Run();

            //- HOLDS -//
            Entities.WithAll<WithinJudgeRange, ChartTime>().ForEach(
                (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane cl) =>
                {
                    if (currentTime - Constants.FarWindow > chartIncrTime.time)
                    {
                        chartIncrTime.UpdateJudgePointCache(currentTime, out int count);
                        lostCount += count;
                        currentCombo = 0;
                        particleBuffer.PlayHoldParticle(cl.lane - 1, false);
                    }
                }
            ).Run();

            Entities.WithAll<WithinJudgeRange, ChartTime>().ForEach(
                (Entity en, in ChartIncrTime chartIncrTime, in ChartLane cl) =>
                {
                    if (currentTime - Constants.FarWindow > chartIncrTime.endTime)
                    {
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        particleBuffer.DisableLaneParticle(cl.lane - 1);
                    }
                }
            ).Run();

            //- ARCS -//
            //...

            //- DESTROY ON TIMING -//
            //WARNING: TEMPORARY SOLUTION
            Entities.WithAll<DestroyOnTiming>().ForEach(
                (Entity en, in ChartTime charttime) =>
                {
                    if (currentTime >= charttime.value)
                    {
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                    }
                }
            ).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            ScoreManager.Instance.lostCount = lostCount;
            ScoreManager.Instance.currentCombo = currentCombo;
        }
    }
}