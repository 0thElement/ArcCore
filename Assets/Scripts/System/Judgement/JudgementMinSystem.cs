using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.Utility;
using ArcCore.Math;

[UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(JudgementExpireSystem))]
public class JudgementMinSystem : SystemBase
{
    public static readonly float2 arctapBoxExtents = new float2(4f, 1f);
    private enum MinType
    {
        Arctap,
        Tap,
        Hold,
        Void
    }

    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        int currentTime = Conductor.Instance.receptorTime;

        Entity minEntity = Entity.Null;
        int minTime;
        MinType minType;
        JudgeType minJType = JudgeType.Lost;

        var touchPoints = InputManager.GetEnumerator();
        while (touchPoints.MoveNext())
        {
            minTime = int.MaxValue;
            minType = MinType.Void;

            TouchPoint touch = touchPoints.Current;
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
                            minJType = JudgeManage.GetType(currentTime - chartTime.value);
                        }
                    }
                ).Run();

                //- LOCKED HOLDS -//
                Entities.WithAll<WithinJudgeRange, ChartTime, HoldLocked>().ForEach(
                    (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane cl) => {
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
                            minJType = JudgeManage.GetType(currentTime - chartTime.value);
                        }
                    }
                ).Run();
            }

            //- HANDLE MIN ENTITY -//
            switch (minType)
            {
                case MinType.Void: break;

                case MinType.Tap:
                    EntityManager.DisableEntity(minEntity);
                    ScoreManager.Instance.AddJudge(minJType);
                    break;

                case MinType.Arctap:
                    EntityManager.DisableEntity(EntityManager.GetComponentData<EntityReference>(minEntity).value);
                    EntityManager.DisableEntity(minEntity);
                    ScoreManager.Instance.AddJudge(minJType);
                    break;

                case MinType.Hold:
                    ChartIncrTime chartIncrTime = EntityManager.GetComponentData<ChartIncrTime>(minEntity);
                    if (!chartIncrTime.UpdateJudgePointCache(currentTime, out int count))
                    {
                        EntityManager.RemoveComponent<WithinJudgeRange>(minEntity);
                        EntityManager.AddComponent<PastJudgeRange>(minEntity);
                        //this ideology might be problematic! (doubling chunk count again)
                    }
                    else
                    {
                        EntityManager.SetComponentData(minEntity, chartIncrTime);
                        EntityManager.RemoveComponent<HoldLocked>(minEntity);
                    }
                    ScoreManager.Instance.AddJudge(minJType, count);
                    break;
            }
        }
    }
}