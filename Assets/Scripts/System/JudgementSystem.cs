//COMMENT THIS OUT IN ORDER TO HIDE CONTENTS OF THIS FILE
#define JDG_ACTIVE
//#define ARC_ACTIVE

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Components;
using ArcCore.Behaviours;
using Unity.Rendering;
using ArcCore.Utility;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.Behaviours.EntityCreation;
using ArcCore;
using ArcCore.Parsing;
using ArcCore.Components.Tags;
using ArcCore.Math;
using static ArcCore.EntityManagement;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class JudgementSystem : SystemBase
{
    public static JudgementSystem Instance { get; private set; }
    public EntityManager entityManager;

    public bool IsReady =>
#if ARC_ACTIVE 
    arcFingers.IsCreated 
#else 
    true 
#endif
    ;

    public const float arcLeniencyGeneral = 2f;
    public static readonly float2 arctapBoxExtents = new float2(4f, 1f); //DUMMY VALUES

#if ARC_ACTIVE
    public NativeRefArray<ArcCompleteState> globalArcStates;
    public NativeArray<int> arcFingers;
#endif

    BeginSimulationEntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;

    private enum JudgeEnType
    {
        None,
        Arctap,
        Tap,
        Hold
    }

    protected override void OnCreate()
    {
        Instance = this;
        entityManager = EManager;
        beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    public void SetupColors()
    {
#if ARC_ACTIVE
        arcFingers = new NativeArray<int>(utils.newarr_fill_aclen(-1), Allocator.Persistent);
        globalArcStates = new NativeRefArray<ArcCompleteState>(utils.newarr_fill_aclen(new ArcCompleteState(ArcState.Normal)), Allocator.Persistent);
#endif
    }
    protected override void OnDestroy()
    {
#if ARC_ACTIVE
        arcFingers.Dispose();
        globalArcStates.Dispose();
#endif
    }


    protected override unsafe void OnUpdate()
    {

#if JDG_ACTIVE
        //Only execute after full initialization
        if (!IsReady)
            return;

        //Get data from statics
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Command buffering
        var commandBuffer = beginSimulationEntityCommandBufferSystem.CreateCommandBuffer();

        //Particles
        NativeList<SkyParticleAction>   skyParticleActions   = new NativeList<SkyParticleAction>  (Allocator.TempJob);
        NativeList<ComboParticleAction> comboParticleActions = new NativeList<ComboParticleAction>(Allocator.TempJob);
        NativeList<TrackParticleAction> trackParticleActions = new NativeList<TrackParticleAction>(Allocator.TempJob);

        //Score management data
        int maxPureCount = ScoreManager.Instance.maxPureCount,
            latePureCount = ScoreManager.Instance.latePureCount,
            earlyPureCount = ScoreManager.Instance.earlyPureCount,
            lateFarCount = ScoreManager.Instance.lateFarCount,
            earlyFarCount = ScoreManager.Instance.earlyFarCount,
            lostCount = ScoreManager.Instance.lostCount,
            combo = ScoreManager.Instance.currentCombo;
        
        void Judge(JudgeType type, int count = 1)
        {
            switch (type) 
            {
                case JudgeType.Lost:
                    lostCount += count;
                    combo = 0;
                    break;

                case JudgeType.EarlyFar:
                    earlyFarCount += count;
                    combo += count;
                    break;

                case JudgeType.EarlyPure:
                    earlyPureCount += count;
                    combo += count;
                    break;

                case JudgeType.MaxPure:
                    maxPureCount += count;
                    combo += count;
                    break;

                case JudgeType.LatePure:
                    latePureCount += count;
                    combo += count;
                    break;

                case JudgeType.LateFar:
                    lateFarCount += count;
                    combo += count;
                    break;
            }
        }

        Entity minEntity = Entity.Null;
        JudgeEnType minEnType;
        JudgeType judgeType;

        //Handle all unlocked holds
        QuadArr<int> trackHits = InputManager.Instance.tracksHeld;

        Entities.WithNone<HoldLocked>().ForEach(

            (Entity entity, ref ChartIncrTime iTime, in ChartLane track)

                    =>

            {

                //Invalidate holds out of time range
                if (iTime.time - Constants.FarWindow < currentTime) return;

                //Increment or kill holds out of time for judging
                if (iTime.time + Constants.FarWindow < currentTime)
                {
                    if (!iTime.UpdateJudgePointCache(currentTime, out int comboCount))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(entity);
                        commandBuffer.AddComponent<PastJudgeRange>(entity);
                    }
                    else Judge(JudgeType.Lost, comboCount);
                }

                //Invalidate holds not in range; should also rule out all invalid data, i.e. positions with a lane of -1
                if (trackHits[track.lane] > 0)
                {
                    if (!iTime.UpdateJudgePointCache(currentTime, out int comboCount))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(entity);
                        commandBuffer.AddComponent<PastJudgeRange>(entity);
                    }
                    else Judge(JudgeType.MaxPure, comboCount);
                }

            }

        ).Run();

        //Reset all isReds
#if ARC_ACTIVE
        for (int i = 0; i < globalArcStates.Length; i++)
        {
            //IMPLEMENT SHIZ
            globalArcStates[i]->state = ArcState.Unheld;
            globalArcStates[i]->isRed = true;
        }
#endif

        //Clear old items
        //Holds:
        Entities.WithAll<WithinJudgeRange, HoldLocked>().ForEach(

            (Entity entity, ref ChartIncrTime iTime, in ChartLane track)

                =>

            {

                //Invalidate holds out of time range
                if (iTime.time - Constants.FarWindow < currentTime) return;

                //Increment or kill holds out of time for judging
                if (iTime.time + Constants.FarWindow < currentTime)
                {
                    if (!iTime.UpdateJudgePointCache(currentTime, out int comboCount))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(entity);
                        commandBuffer.AddComponent<PastJudgeRange>(entity);
                    }
                    else Judge(JudgeType.Lost, comboCount);
                }

            }

        ).Run();

        //Tap notes; no EntityReference, those only exist on arctaps
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in ChartLane track)

                =>

            {
                //Items out of time for judging
                if (time.value + Constants.FarWindow < currentTime)
                {
                    Judge(JudgeType.Lost);
                    commandBuffer.DestroyEntity(entity);
                }

            }

        ).Run();

        //Arctaps:
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in ChartPosition position, in EntityReference enRef)

                =>

            {
                //Items out of time for judging
                if (time.value + Constants.FarWindow < currentTime)
                {
                    Judge(JudgeType.Lost);
                    commandBuffer.DestroyEntity(entity);
                    commandBuffer.DestroyEntity(enRef.value);
                }
            }

        ).Run();

        //KILL OFF BEFORE CONTINUING
        commandBuffer.Playback(entityManager);

        //Execute for each touch
        var touches = InputManager.GetEnumerator();
        while (touches.MoveNext())
        {
            TouchPoint touch = touches.Current;
            minEnType = JudgeEnType.None;
            judgeType = JudgeType.Lost;

            //Track taps
            if (touch.TrackValid)
            {

                //Hold notes
                Entities.WithAll<WithinJudgeRange, HoldLocked>().ForEach(

                    (Entity entity, ref ChartIncrTime iTime, in ChartLane track)

                        =>

                    {

                        //Invalidate holds out of time range
                        if (iTime.time - Constants.FarWindow < currentTime) return;

                        //Increment or kill holds out of time for judging
                        if (iTime.time + Constants.FarWindow < currentTime)
                        {
                            if (!iTime.UpdateJudgePointCache(currentTime, out int comboCount))
                            {
                                commandBuffer.RemoveComponent<WithinJudgeRange>(entity);
                                commandBuffer.AddComponent<PastJudgeRange>(entity);
                            }
                            else Judge(JudgeType.Lost, comboCount);
                        }

                        //Invalidate holds not in range; should also rule out all invalid data, i.e. positions with a lane of -1
                        if (touch.track != track.lane) return;

                        if (touch.status == TouchPoint.Status.Tapped)
                        {
                            minEntity = entity;
                            minEnType = JudgeEnType.Hold;
                        }

                    }

                ).Run();

                //Tap notes; no EntityReference, those only exist on arctaps
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    (Entity entity, in ChartTime time, in ChartLane track)

                        =>

                    {
                        //Items out of time for judging
                        if (time.value + Constants.FarWindow < currentTime)
                        {
                            Judge(JudgeType.Lost);
                            commandBuffer.DestroyEntity(entity);
                        }

                        //Invalidate if not in range of a tap; should also rule out all invalid data, i.e. positions with a lane of -1
                        if (touch.track != track.lane) return;

                        //Register
                        if (touch.status == TouchPoint.Status.Tapped)
                        {
                            minEntity = entity;
                            minEnType = JudgeEnType.Tap;
                            judgeType = JudgeManage.GetType(currentTime - time.value);
                        }
                    }

                ).Run();

            }

            if (touch.InputPlaneValid)
            {
                //Arctap notes
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    (Entity entity, in ChartTime time, in ChartPosition position, in EntityReference enRef)

                        =>

                    {
                        //Items out of time for judging
                        if (time.value + Constants.FarWindow < currentTime)
                        {
                            Judge(JudgeType.Lost);
                            commandBuffer.DestroyEntity(entity);
                            commandBuffer.DestroyEntity(enRef.value);
                        }

                        //Invalidate if not in range of a tap
                        if (touch.InputPlane.CollidesWith(new Rect2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents)))
                        {
                            minEntity = entity;
                            minEnType = JudgeEnType.Arctap;
                            judgeType = JudgeManage.GetType(currentTime - time.value);
                        }
                    }

                ).Run();

#if ARC_ACTIVE
                //Arcs!
                Entities.ForEach(

                    (Entity entity, ref ChartIncrTime iTime, in ChartTime cTime, in ArcData arcData, in ColorID colorID)

                        =>

                    {

                        ///Modified HoldBaseLogic
                        //FUCK I DONT REMEMBER WHAT IT DOES BUT IT WORKS AAAA
                        if (iTime.time < currentTime || !globalArcStates[colorID.value].isRed) return;

                        //Increment or kill arcs out of time for judging
                        if (iTime.time + Constants.FarWindow < currentTime)
                        {
                            JudgeFromIncr(entity, ref iTime, JudgeType.Lost);
                        }

                        Circle2D arcCollider = AffArc.ColliderAt(currentTime, cTime.value, iTime.endTime, arcData.start, arcData.end, arcData.easing);
                        if (touch.InputPlane.CollidesWith(arcCollider))
                        {
                            if (touch.fingerId == -1 || touch.fingerId == arcFingers[colorID.value])
                            {
                                globalArcStates.Ele = globalArcStates[colorID.value].Copy(withState: ArcState.Normal, withRed: false);
                                JudgeFromIncr(entity, ref iTime, JudgeType.MaxPure);
                            }
                            
                            globalArcStates[colorID.value] = globalArcStates[colorID.value].Copy(withState: ArcState.Normal);
                        }
    
                    }

                ).Run();

#endif
            }

            switch (minEnType)
            {
                case JudgeEnType.Hold:

                    ChartIncrTime t = entityManager.GetComponentData<ChartIncrTime>(minEntity);

                    if (!t.UpdateJudgePointCache(currentTime, out int comboCount))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(minEntity);
                        commandBuffer.AddComponent<PastJudgeRange>(minEntity);
                    }
                    else
                    {
                        entityManager.SetComponentData(minEntity, t);
                        Judge(JudgeType.Lost, comboCount);
                    }

                    break;

                case JudgeEnType.Arctap:

                    EntityReference eref = entityManager.GetComponentData<EntityReference>(minEntity);
                    commandBuffer.AddComponent<Disabled>(eref.value);

                    continue;

                case JudgeEnType.Tap:

                    commandBuffer.AddComponent<Disabled>(minEntity);
                    Judge(judgeType);

                    break;
            }

        }

#if ARC_ACTIVE
        //Clean up arcs
        Entities.ForEach(

            (Entity entity, ref ChartIncrTime iTime, in ChartTime cTime, in ColorID colorID)

                =>

            {

                ///Modified HoldBaseLogic
                //FUCK I DONT REMEMBER WHAT IT DOES BUT IT WORKS AAAA
                if (iTime.time < currentTime || !globalArcStates[colorID.value].isRed) return;

                JudgeFromIncr(entity, ref iTime, JudgeType.Lost);

            }

        ).Run();
#endif
        // Repopulate managed data
        ScoreManager.Instance.currentCombo = combo;
        ScoreManager.Instance.maxPureCount = maxPureCount;
        ScoreManager.Instance.latePureCount = latePureCount;
        ScoreManager.Instance.lateFarCount = lateFarCount;
        ScoreManager.Instance.earlyPureCount = earlyPureCount;
        ScoreManager.Instance.earlyFarCount = earlyFarCount;
        ScoreManager.Instance.lostCount = lostCount;


#endif
    }
}