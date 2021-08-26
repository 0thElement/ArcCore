using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using ArcCore.Gameplay.Data;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateBefore(typeof(ArcCollisionCheckSystem))]
    public class ArcGraceCheckSystem : SystemBase
    {
        EntityQuery arcJudgeQuery;
        protected override void OnCreate()
        {
            arcJudgeQuery = GetEntityQuery(
                ComponentType.ReadOnly<WithinJudgeRange>(),
                ComponentType.ReadOnly<ArcData>(),
                ComponentType.ReadOnly<ArcGroupID>(),
                ComponentType.ReadOnly<ArcColorID>(),
                ComponentType.ReadOnly<ChartIncrTime>()
            );
        }
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.ReceptorTime;
            int maxArcColor = PlayManager.MaxArcColor;

            List<NativeArray<ArcData>> arcDataByColor = new List<NativeArray<ArcData>>();

            for (int c=0; c <= maxArcColor; c++)
            {
                arcJudgeQuery.SetSharedComponentFilter(new ArcColorID(c));
                arcDataByColor.Add(arcJudgeQuery.ToComponentDataArray<ArcData>(Allocator.Temp));
            }

            for (int c1 = 0; c1 <= maxArcColor - 1; c1++)
            {
                for (int c2 = c1 + 1; c2 <= maxArcColor; c2++)
                {
                    for (int i = 0; i < arcDataByColor[c1].Length; i++)
                    {
                        for (int j = 0; j < arcDataByColor[c2].Length; j++)
                        {
                            float2 posDifference = arcDataByColor[c1][i].GetPosAt(currentTime) - arcDataByColor[c2][j].GetPosAt(currentTime);

                            if (Mathf.Abs(posDifference.x) <= Constants.ArcBoxExtents.x
                            &&  Mathf.Abs(posDifference.y) <= Constants.ArcBoxExtents.y)
                            {
                                ActivateGrace();

                                for (int k = c1; k <= maxArcColor; k++)
                                {
                                    arcDataByColor[k].Dispose();
                                }
                                return;
                            }
                        }
                    }
                }

                arcDataByColor[c1].Dispose();
            }
            arcDataByColor[maxArcColor].Dispose();
        }

        private void ActivateGrace()
        {
            foreach (var fsm in PlayManager.ArcColorFsm)
            {
                fsm.Execute(ArcColorFSM.Event.Grace);
            }
        }
    }

}