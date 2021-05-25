using ArcCore;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.Gameplay.Utility;
using ArcCore.Math;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(TappableJudgeSystem))]
    public class UnlockedHoldJudgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!GameState.isChartMode) return;

            int currentTime = Conductor.Instance.receptorTime;
            int maxPureCount = ScoreManager.Instance.maxPureCount,
            currentCombo = ScoreManager.Instance.currentCombo;
            NTrackArray<int> tracksHeld = InputManager.Instance.tracksHeld;

            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var particleBuffer = ParticleJudgeSystem.particleBuffer;

            Entities.WithNone<HoldLocked, PastJudgeRange>().ForEach(
                (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane lane) =>
                {
                    if (chartIncrTime.time > currentTime - Constants.FarWindow && tracksHeld[lane.lane] > 0)
                    {
                        chartIncrTime.UpdateJudgePointCachePure(currentTime, out int count);
                        maxPureCount += count;
                        currentCombo += count;
                        particleBuffer.PlayTapParticle(Conversion.TrackToXYParticle(lane.lane), ParticleCreator.JudgeType.Pure, ParticleCreator.JudgeDetail.None);
                    }
                }
            ).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
            ScoreManager.Instance.maxPureCount = maxPureCount;
            ScoreManager.Instance.currentCombo = currentCombo;
        }
    }
}