using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;

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
                    if (chartIncrTime.time < currentTime + Constants.HoldLostWindow && tracksHeld[lane.lane] > 0)
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