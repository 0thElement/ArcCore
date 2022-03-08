using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utilities;

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
                ComponentType.ReadOnly<ArcColorID>(),
                ComponentType.ReadOnly<ChartIncrTime>()
            );
        }
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.ReceptorTime;
            int maxArcColor = PlayManager.MaxArcColor;

            NativeArray<Entity> arcEntities = arcJudgeQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<ArcData> arcData = arcJudgeQuery.ToComponentDataArray<ArcData>(Allocator.Temp);

            int count = arcEntities.Length;

            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    float2 posDifference = arcData[i].GetPosAt(currentTime) - arcData[j].GetPosAt(currentTime);

                    if (Mathf.Abs(posDifference.x) <= Constants.ArcBoxExtents.x
                    &&  Mathf.Abs(posDifference.y) <= Constants.ArcBoxExtents.y
                    &&  EntityManager.GetSharedComponentData<ArcColorID>(arcEntities[i]).id
                    !=  EntityManager.GetSharedComponentData<ArcColorID>(arcEntities[j]).id)
                    {
                        ActivateGrace();
                    
                        arcEntities.Dispose();
                        arcData.Dispose();
                        return;
                    }
                }
            }

            arcEntities.Dispose();
            arcData.Dispose();
        }

        private void ActivateGrace()
        {
            foreach (var fsm in PlayManager.ArcColorState)
            {
                fsm.Grace(PlayManager.ReceptorTime);
            }
        }
    }

}