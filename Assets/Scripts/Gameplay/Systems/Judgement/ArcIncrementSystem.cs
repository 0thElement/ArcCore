using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Behaviours.EntityCreation;
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
            int currentTime = Conductor.Instance.receptorTime;
            var tracker = ScoreManager.Instance.tracker;
            var particleBuffer = ParticleJudgeSystem.particleBuffer;

            NativeArray<GroupState> arcGroupHeldState = ArcCollisionCheckSystem.arcGroupHeldState;
            List<ArcColorFSM> arcColorFsmArray = ArcCollisionCheckSystem.arcColorFsmArray;

            for (int color = 0; color < ArcEntityCreator.ColorCount; color ++) 
            {
                if (arcColorFsmArray[color].IsRedArc()) continue;

                Entities.WithSharedComponentFilter<ArcColorID>(new ArcColorID(color)).WithAll<WithinJudgeRange>().ForEach(
                    (Entity en, ref ChartIncrTime chartIncrTime, in ArcGroupID groupID, in ArcData arcData) =>
                    {
                        if (chartIncrTime.time < currentTime + Constants.HoldLostWindow && arcGroupHeldState[groupID.value] == GroupState.Held)
                        {
                            int count = chartIncrTime.UpdateJudgePointCache(currentTime + Constants.HoldLostWindow);
                            if (count > 0)
                            {
                                tracker.AddJudge(JudgeType.MaxPure, count);
                                particleBuffer.PlayArcParticle(groupID.value, arcData.GetPosAt(currentTime), true);
                            }
                        }
                    }
                ).Run();
            }

            ScoreManager.Instance.tracker = tracker;
        }
    }
}