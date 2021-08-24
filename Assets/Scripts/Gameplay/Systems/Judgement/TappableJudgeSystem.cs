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
using Unity.Rendering;
using ArcCore.Math;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ExpirableJudgeSystem))]
    public class TappableJudgeSystem : SystemBase
    {
        private enum MinType
        {
            Arctap,
            Tap,
            Hold,
            Void
        }

        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            var particleBuffer = PlayManager.ParticleBuffer;

            Entity minEntity = Entity.Null;
            int minTime;
            MinType minType;
            JudgeType minJType = JudgeType.Lost;
            var commandBuffer = PlayManager.CommandBuffer;

            var touchPoints = PlayManager.InputHandler.GetEnumerator();
            while (touchPoints.MoveNext())
            {
                minTime = int.MaxValue;
                minType = MinType.Void;

                TouchPoint touch = touchPoints.Current;
                int tapTime = touch.tapTime;
                if (touch.status != TouchPoint.Status.Tapped) continue;

                if (touch.TrackValid)
                {
                    //- TAPS -//
                    Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime, ArcTapShadowReference>().ForEach(
                        (Entity en, in ChartTime chartTime, in ChartLane cl) =>
                        {
                            if (chartTime.value < minTime && touch.track == cl.lane)
                            {
                                minTime = chartTime.value;
                                minEntity = en;
                                minType = MinType.Tap;
                                minJType = JudgeManage.GetType(tapTime - chartTime.value);
                            }
                        }
                    ).Run();

                    //- LOCKED HOLDS -//
                    Entities.WithAll<WithinJudgeRange, ChartIncrTime, HoldLocked>().ForEach(
                        (Entity en, ref ChartTime chartTime, in ChartLane cl) =>
                        {
                            if (chartTime.value < minTime && touch.track == cl.lane)
                            {
                                minTime = chartTime.value;
                                minEntity = en;
                                minType = MinType.Hold;
                                minJType = JudgeType.MaxPure;
                            }
                        }
                    ).Run();
                }

                if (touch.InputPlaneValid)
                {
                    //- ARCTAPS -//
                    Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime>().WithoutBurst().ForEach(
                        (Entity en, in ChartTime chartTime, in ChartPosition cp) =>
                        {
                            if (chartTime.value < minTime && 
                                touch.inputPlane.Value.CollidesWith(new Rect2D(cp.xy - Constants.ArctapBoxExtents, cp.xy + Constants.ArctapBoxExtents)))
                            {
                                minTime = chartTime.value;
                                minEntity = en;
                                minType = MinType.Arctap;
                                minJType = JudgeManage.GetType(tapTime - chartTime.value);
                            }
                        }
                    ).Run();
                }

                //- HANDLE MIN ENTITY -//
                switch (minType)
                {
                    case MinType.Void: break;

                    case MinType.Tap:
                        commandBuffer.DisableEntity(minEntity);
                        commandBuffer.AddComponent<PastJudgeRange>(minEntity);
                        PlayManager.ScoreHandler.tracker.AddJudge(minJType);
                        particleBuffer.PlayTapParticle(
                            new float2(Conversion.TrackToX(EntityManager.GetComponentData<ChartLane>(minEntity).lane), 0.5f),
                            minJType,
                            1f
                        );
                        break;

                    case MinType.Arctap:
                        Entity shadow = EntityManager.GetComponentData<ArcTapShadowReference>(minEntity).value;
                        commandBuffer.DisableEntity(shadow);
                        commandBuffer.DisableEntity(minEntity);
                        commandBuffer.AddComponent<PastJudgeRange>(minEntity);
                        PlayManager.ScoreHandler.tracker.AddJudge(minJType);
                        particleBuffer.PlayTapParticle(
                            EntityManager.GetComponentData<ChartPosition>(minEntity).xy,
                            minJType,
                            1f
                        );
                        break;

                    case MinType.Hold:
                        ChartIncrTime chartIncrTime = EntityManager.GetComponentData<ChartIncrTime>(minEntity);
                        int count = chartIncrTime.UpdateJudgePointCache(tapTime + Constants.FarWindow);
                        commandBuffer.SetComponent<ChartIncrTime>(minEntity, chartIncrTime);
                        commandBuffer.RemoveComponent<HoldLocked>(minEntity);
                        PlayManager.ScoreHandler.tracker.AddJudge(minJType, count);
                        particleBuffer.PlayHoldParticle(
                            EntityManager.GetComponentData<ChartLane>(minEntity).lane - 1,
                            true
                        );
                        break;
                }
            }
        }
    }
}