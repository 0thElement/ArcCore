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
using ArcCore.Math;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(TappableJudgeSystem))]
    public class UnlockedHoldJudgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!GameState.isChartMode) return;

            int currentTime = Conductor.Instance.receptorTime;
            var tracker = ScoreManager.Instance.tracker;
            var tracksHeld = InputManager.Instance.tracksHeld;

            var particleBuffer = ParticleJudgeSystem.particleBuffer;

            Entities.WithNone<HoldLocked, PastJudgeRange>().ForEach(
                (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane lane) =>
                {
                    if (chartIncrTime.time < currentTime + Constants.FarWindow && (bool)tracksHeld[lane.lane])
                    {
                        int count = chartIncrTime.UpdateJudgePointCache(currentTime + Constants.FarWindow);
                        if (count > 0)
                        {
                            tracker.AddJudge(JudgeType.MaxPure, count);
                            particleBuffer.PlayHoldParticle(lane.lane - 1, true);
                        }
                    }
                }
            ).Run();

            ScoreManager.Instance.tracker = tracker;
        }
    }
}