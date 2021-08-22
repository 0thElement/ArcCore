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

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(HoldHighlightSystem))]
    public class UnlockedHoldJudgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.ReceptorTime;
            var tracker = PlayManager.ScoreHandler.tracker;
            var tracksHeld = PlayManager.InputHandler.tracksHeld;

            var particleBuffer = PlayManager.ParticleBuffer;

            Entities.WithNone<HoldLocked, PastJudgeRange>().ForEach(
                (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane lane) =>
                {
                    if (chartIncrTime.time < currentTime + Constants.HoldLostWindow && (bool)tracksHeld[lane.lane])
                    {
                        int count = chartIncrTime.UpdateJudgePointCache(currentTime + Constants.HoldLostWindow);
                        if (count > 0)
                        {
                            tracker.AddJudge(JudgeType.MaxPure, count);
                            particleBuffer.PlayHoldParticle(lane.lane - 1, true);
                        }
                    }
                }
            ).Run();

            PlayManager.ScoreHandler.tracker = tracker;
        }
    }
}