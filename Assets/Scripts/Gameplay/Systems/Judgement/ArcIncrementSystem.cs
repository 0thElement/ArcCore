using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.EntityCreation;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ArcCollisionCheckSystem))]
    public class ArcIncrementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.ReceptorTime;
            var tracker = PlayManager.ScoreHandler.tracker;
            var particleBuffer = PlayManager.ParticleBuffer;

            NativeArray<GroupState> arcGroupHeldState = ArcCollisionCheckSystem.arcGroupHeldState;
            List<ArcColorFSM> arcColorFsmArray = ArcCollisionCheckSystem.arcColorFsmArray;

            for (int color = 0; color < ArcEntityCreator.ColorCount; color ++) 
            {
                if (arcColorFsmArray[color].IsRedArc()) continue;

                Entities.WithSharedComponentFilter<ArcColorID>(new ArcColorID(color)).WithAll<WithinJudgeRange>().ForEach(
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