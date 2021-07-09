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
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ExpirableJudgeSystem))]
    public class TappableJudgeSystem : SystemBase
    {
        public static TappableJudgeSystem Instance { get; private set; }

        public static readonly float2 arctapBoxExtents = new float2(2f, 1.75f);
        private enum MinType
        {
            Arctap,
            Tap,
            Hold,
            Void
        }
        private EndSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            Instance = this;
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (!GameState.isChartMode) return;

            var particleBuffer = ParticleJudgeSystem.particleBuffer;

            Entity minEntity = Entity.Null;
            int minTime;
            MinType minType;
            JudgeType minJType = JudgeType.Lost;
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            var touchPoints = InputManager.Instance.GetEnumerator();
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
                    Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime, EntityReference>().ForEach(
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
                    Entities.WithAll<WithinJudgeRange, ChartTime, HoldLocked>().ForEach(
                        (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane cl) =>
                        {
                            if (chartIncrTime.time < minTime && touch.track == cl.lane)
                            {
                                minTime = chartIncrTime.time;
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
                            if (chartTime.value < minTime && touch.inputPlane.Value.CollidesWith(new Rect2D(cp.xy - arctapBoxExtents, cp.xy + arctapBoxExtents)))
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
                        ScoreManager.Instance.tracker.AddJudge(minJType);
                        particleBuffer.PlayTapParticle(
                            new float2(Conversion.TrackToX(EntityManager.GetComponentData<ChartLane>(minEntity).lane), 0.5f),
                            minJType,
                            1f
                        );
                        break;

                    case MinType.Arctap:
                        commandBuffer.DisableEntity(EntityManager.GetComponentData<EntityReference>(minEntity).value);
                        commandBuffer.DisableEntity(minEntity);
                        commandBuffer.AddComponent<PastJudgeRange>(minEntity);
                        ScoreManager.Instance.tracker.AddJudge(minJType);
                        particleBuffer.PlayTapParticle(
                            EntityManager.GetComponentData<ChartPosition>(minEntity).xy,
                            minJType,
                            1f
                        );
                        break;

                    case MinType.Hold:
                        ChartIncrTime chartIncrTime = EntityManager.GetComponentData<ChartIncrTime>(minEntity);
                        chartIncrTime.UpdateJudgePointCache(tapTime, out int count);
                        commandBuffer.SetComponent<ChartIncrTime>(minEntity, chartIncrTime);
                        commandBuffer.RemoveComponent<HoldLocked>(minEntity);
                        ScoreManager.Instance.tracker.AddJudge(minJType, count);
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