using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ArcColorSystem))]
    public class ArcIncrementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.ReceptorTime;
            var tracker = PlayManager.ScoreHandler.tracker;
            var particleBuffer = PlayManager.ParticleBuffer;

            var arcGroupHeldState = PlayManager.ArcGroupHeldState;
            var arcColorFsmArray  = PlayManager.ArcColorFsm;

            for (int color = 0; color <= PlayManager.MaxArcColor; color ++) 
            {
                if (arcColorFsmArray[color].IsRedArc()) continue;

                Entities.WithSharedComponentFilter<ArcColorID>(new ArcColorID(color)).ForEach(
                    (Entity en, ref ChartIncrTime chartIncrTime, in ArcGroupID groupID) =>
                    {
                        if (chartIncrTime.time < currentTime + Constants.HoldLostWindow && arcGroupHeldState[groupID.value] == GroupState.Held)
                        {
                            int count = chartIncrTime.UpdateJudgePointCache(currentTime + Constants.HoldLostWindow);
                            if (count > 0)
                            {
                                tracker.AddJudge(JudgeType.MaxPure, count);
                            }
                            particleBuffer.PlayArcParticle(groupID.value, true, count > 0);
                        }
                    }
                ).Run();
            }

            PlayManager.ScoreHandler.tracker = tracker;
        }
    }
}