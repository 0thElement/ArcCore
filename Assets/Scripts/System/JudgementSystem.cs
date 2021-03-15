using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using Unity.Rendering;
using ArcCore.Utility;
using Unity.Mathematics;
using ArcCore.MonoBehaviours.EntityCreation;
using ArcCore;
using ArcCore.Tags;

public struct EntityWithLoadedNoteData
{
    public readonly Entity en;
    public readonly ChartTime ch;

    public readonly float3 pos;

    public readonly bool isHold;
    public readonly HoldFunnelPtr holdFunnelPtr;
    public readonly ArctapFunnelPtr arctapFunnelPtr;

    public EntityWithLoadedNoteData(Entity en, ChartTime ch, float3 pos, bool isHold, HoldFunnelPtr holdFunnelPtr, ArctapFunnelPtr arctapFunnelPtr)
    {
        this.en = en;
        this.ch = ch;
        this.pos = pos;
        this.isHold = isHold;
        this.holdFunnelPtr = holdFunnelPtr;
        this.arctapFunnelPtr = arctapFunnelPtr;
    }
}

public struct JudgeOccurance
{
    public enum JudgeType
    {
        MAX_PURE,
        LATE_PURE,
        EARLY_PURE,
        LATE_FAR,
        EARLY_FAR,
        LOST
    }

    public JudgeType type;
    public EntityWithLoadedNoteData entity;
    public bool deleteEn;

    public JudgeOccurance(JudgeType type, EntityWithLoadedNoteData entity, bool deleteEn)
    {
        this.type = type;
        this.entity = entity;
        this.deleteEn = deleteEn;
    }

    public static JudgeType GetType(int timeDifference)
    {
        if (timeDifference > Constants.FarWindow)
            return JudgeType.LOST;
        else if (timeDifference > Constants.PureWindow)
            return JudgeType.EARLY_FAR;
        else if (timeDifference > Constants.MaxPureWindow)
            return JudgeType.EARLY_PURE;
        else if (timeDifference > -Constants.MaxPureWindow)
            return JudgeType.MAX_PURE;
        else if (timeDifference > -Constants.PureWindow)
            return JudgeType.LATE_PURE;
        else if (timeDifference > -Constants.FarWindow)
            return JudgeType.LATE_FAR;
        else return JudgeType.LOST;
    }
}

public class JudgementSystem : SystemBase
{
    public static JudgementSystem Instance { get; private set; }
    public EntityManager globalEntityManager;
    public NativeArray<int> globalCurrentArcFingers;
    public NativeArray<AABB2D> globalLaneAABB2Ds;

    public const float arcLeniencyGeneral = 2f;
    protected override void OnCreate()
    {
        Instance = this;
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        globalEntityManager = defaultWorld.EntityManager;
        
        globalLaneAABB2Ds = new NativeArray<AABB2D>(
            new AABB2D[] {
                new AABB2D(new float2(Convert.TrackToX(1), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(Convert.TrackToX(2), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(Convert.TrackToX(3), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(Convert.TrackToX(4), 0), new float2(Constants.LaneWidth, float.PositiveInfinity))
                    },
            Allocator.Persistent
            );
    }
    public void SetupColors()
    {
        globalCurrentArcFingers = new NativeArray<int>(ArcEntityCreator.Instance.arcColors.Length, Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        globalCurrentArcFingers.Dispose();
        globalLaneAABB2Ds.Dispose();
    }
    protected override unsafe void OnUpdate()
    {
        //Only execute after full initialization
        if (!globalCurrentArcFingers.IsCreated)
            return;

        //Get data from statics
        NativeArray<TouchPoint> touchPoints = InputManager.Instance.touchPoints;
        NativeArray<EntityWithLoadedNoteData> noteForTouch = new NativeArray<EntityWithLoadedNoteData>(touchPoints.Length, Allocator.TempJob);
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Create copies of instances
        EntityManager entityManager = globalEntityManager;
        NativeArray<int> currentArcFingers = globalCurrentArcFingers;
        NativeArray<AABB2D> laneAABB2Ds = globalLaneAABB2Ds;

        NativeList<JudgeOccurance> judgeBacklog = new NativeList<JudgeOccurance>(Allocator.TempJob);


        // Handle all arcs //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ArcFunnelPtr arcFunnelPtr, in LinearPosGroup linearPosGroup, in ColorID colorID, in StrictArcJudge strictArcJudge) 
            
                =>

            {

                ArcFunnel* arcFunnelPtrD = arcFunnelPtr.Value;

                // Kill all points that have passed
                if (linearPosGroup.endTime < currentTime)
                {

                    arcFunnelPtrD->visualState = 
                        arcFunnelPtrD->isHit ? 
                        LongnoteVisualState.JUDGED_PURE : 
                        LongnoteVisualState.JUDGED_LOST;

                    JudgeOccurance judgeOcc =
                        new JudgeOccurance(
                            arcFunnelPtrD->isRed || !arcFunnelPtrD->isHit ? 
                                JudgeOccurance.JudgeType.LOST : 
                                JudgeOccurance.JudgeType.MAX_PURE,
                            new EntityWithLoadedNoteData(
                                entity, new ChartTime(), 
                                new float3(linearPosGroup.startPosition, 0),
                                false,
                                new HoldFunnelPtr(), new ArctapFunnelPtr()
                            ),
                            false
                        );

                    judgeBacklog.Add(judgeOcc);

                    return;

                }

                // Loop through all touch points
                for (int i = 0; i < touchPoints.Length; i++)
                {

                    // Arc hit by finger
                    if (linearPosGroup.startTime <= touchPoints[i].time &&
                        linearPosGroup.endTime >= touchPoints[i].time &&
                        touchPoints[i].status != TouchPoint.Status.RELEASED &&
                        touchPoints[i].inputPlaneValid &&
                        touchPoints[i].inputPlane.CollidingWith(
                            new AABB2D(
                                linearPosGroup.PosAt(currentTime), 
                                new float2(arcLeniencyGeneral)
                                )
                            )) 
                    {

                        // Set hit to true
                        arcFunnelPtrD->isHit = true;

                        // Set red based on current finger id
                        arcFunnelPtrD->isRed =
                            touchPoints[i].fingerId == currentArcFingers[colorID.Value] || 
                            currentArcFingers[colorID.Value] == -1 || !strictArcJudge.Value;

                        //If the point is strict, remove the current finger id to allow for switching
                        if (!strictArcJudge.Value)
                        {
                            currentArcFingers[colorID.Value] = -1;
                        }
                        //If there is no finger currently, allow there to be a new one permitted that the arc is not hit
                        else if (currentArcFingers[colorID.Value] != touchPoints[i].fingerId && !arcFunnelPtrD->isHit)
                        {
                            currentArcFingers[colorID.Value] = touchPoints[i].fingerId;
                        }

                    }
                }

            }

        )
            .WithName("HandleArcs")
            .Schedule(Dependency)
            .Complete();

        
        // Handle all holds //
        Entities.WithAll<WithinJudgeRange, JudgeHoldPoint>().ForEach(

            (Entity entity, in HoldFunnelPtr holdFunnelPtr, in ChartTime time, in Track track) 

                =>

            {

                HoldFunnel* holdFunnelPtrD = holdFunnelPtr.Value;

                //Kill old entities and make lost
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    JudgeOccurance judgeOcc =
                        new JudgeOccurance(
                            JudgeOccurance.JudgeType.LOST,
                            new EntityWithLoadedNoteData(
                                entity, time,
                                new float3(Convert.TrackToX(track.Value), 0, 0),
                                true, holdFunnelPtr, new ArctapFunnelPtr()
                            ),
                            false
                        );

                    judgeBacklog.Add(judgeOcc);

                    return;

                }

                //Reset hold value
                holdFunnelPtrD->visualState = holdFunnelPtrD->isHit ? LongnoteVisualState.JUDGED_PURE : LongnoteVisualState.JUDGED_LOST;

                //If the hold has not been broken
                if (holdFunnelPtrD->isHit)
                {
                    for (int i = 0; i < touchPoints.Length; i++)
                    {
                        if (
                            time.Value - touchPoints[i].time <= Constants.FarWindow &&
                            touchPoints[i].time - time.Value >= Constants.FarWindow &&
                            touchPoints[i].trackPlaneValid &&
                            touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                        )
                        {

                            if (touchPoints[i].status == TouchPoint.Status.RELEASED)
                            {
                                holdFunnelPtrD->isHit = false;
                            }
                            else
                            {

                                JudgeOccurance judgeOcc =
                                        new JudgeOccurance(
                                            JudgeOccurance.JudgeType.MAX_PURE,
                                            new EntityWithLoadedNoteData(
                                                entity, time,
                                                new float3(Convert.TrackToX(track.Value), 0, 0),
                                                true, holdFunnelPtr, new ArctapFunnelPtr()
                                            ),
                                            false
                                        );

                                judgeBacklog.Add(judgeOcc);

                            }

                            return;

                        }
                    }
                }
                //If the hold has been broken
                else
                {
                    for (int i = 0; i < touchPoints.Length; i++)
                    {
                        if (
                            noteForTouch[i].ch.Value < time.Value &&
                            touchPoints[i].status == TouchPoint.Status.TAPPED &&
                            time.Value - touchPoints[i].time <= Constants.FarWindow &&
                            touchPoints[i].time - time.Value >= Constants.FarWindow &&
                            touchPoints[i].trackPlaneValid &&
                            touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                        )
                        {
                            //Check for judge later
                            noteForTouch[i] = new EntityWithLoadedNoteData(
                                entity, time,
                                new float3(Convert.TrackToX(track.Value), 0, 0),
                                true, holdFunnelPtr, new ArctapFunnelPtr()
                                );
                            return;
                        }
                    }
                }

            }

        )
            .WithName("HandleHolds")
            .Schedule(Dependency)
            .Complete();


        // Handle all arctaps //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in SinglePosition pos, in ArctapFunnelPtr funnel)

                =>

            {


                //Kill all remaining entities if they are overaged
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    JudgeOccurance judgeOcc =
                                new JudgeOccurance(
                                    JudgeOccurance.JudgeType.LOST,
                                    new EntityWithLoadedNoteData(
                                        entity, time,
                                        new float3(pos.Value, 0),
                                        false, new HoldFunnelPtr(), funnel
                                    ),
                                    true
                                );

                    judgeBacklog.Add(judgeOcc);

                    return;

                }

                for (int i = 0; i < touchPoints.Length; i++)
                {
                    if (
                        noteForTouch[i].ch.Value < time.Value &&
                        touchPoints[i].status == TouchPoint.Status.TAPPED &&
                        touchPoints[i].inputPlaneValid &&
                        touchPoints[i].inputPlane.CollidingWith(
                            new AABB2D(
                                pos.Value, 
                                new float2(arcLeniencyGeneral)
                                )
                            ))
                    {
                        //Check for judge later
                        noteForTouch[i] = new EntityWithLoadedNoteData(
                            entity, time,
                            new float3(pos.Value, 0),
                            false, new HoldFunnelPtr(), funnel
                            );
                        return;
                    }
                }

            }

        )
            .WithName("HandleArctaps")
            .Schedule(Dependency)
            .Complete();


        // Handle all taps //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in Track track, in ArctapFunnelPtr funnel)

                =>

            {

                for (int i = 0; i < touchPoints.Length; i++)
                {
                    if (
                        noteForTouch[i].ch.Value < time.Value &&
                        touchPoints[i].status == TouchPoint.Status.TAPPED &&
                        touchPoints[i].trackPlaneValid &&
                        touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                    )
                    {
                        //Check for judge later
                        noteForTouch[i] = new EntityWithLoadedNoteData(
                            entity, time,
                            new float3(Convert.TrackToX(track.Value), 0, 0),
                            false, new HoldFunnelPtr(), funnel
                            );
                        return;
                    }
                }

            }

        )
            .WithName("HandleTaps")
            .Schedule(Dependency)
            .Complete();

        // Complete code and find types //
        Job.WithCode(

            () =>

            {

                for (int t = 0; t < noteForTouch.Length; t++)
                {

                    if (!entityManager.Exists(noteForTouch[t].en)) continue;

                    Entity minEntity = noteForTouch[t].en;
                    ChartTime time = noteForTouch[t].ch;


                    JudgeOccurance.JudgeType type;
                    bool delEn;

                    if (noteForTouch[t].isHold)
                    {
                        type = JudgeOccurance.JudgeType.MAX_PURE;
                        delEn = false;

                        noteForTouch[t].holdFunnelPtr.Value->visualState = LongnoteVisualState.JUDGED_PURE;
                    }
                    else
                    {
                        type = JudgeOccurance.GetType(currentTime - time.Value);
                        delEn = true;
                    }

                    JudgeOccurance judgeOcc =
                                new JudgeOccurance(
                                    type,
                                    noteForTouch[t],
                                    delEn
                                );

                    judgeBacklog.Add(judgeOcc);

                }

            }

        )
            .WithName("FinalizeMinimums")
            .Schedule();

        // Complete code and manage backlog //
        Dependency.Complete();
        for (int i = 0; i < judgeBacklog.Length; i++)
        {

            if (judgeBacklog[i].deleteEn)
            {
                entityManager.AddComponent<Disabled>(judgeBacklog[i].entity.en);
                if(!judgeBacklog[i].entity.isHold)
                {
                    judgeBacklog[i].entity.arctapFunnelPtr.Value->isExistant = false;
                }
            }

            switch (judgeBacklog[i].type)
            {
                case JudgeOccurance.JudgeType.LOST:
                    ScoreManager.Instance.lostCount++;
                    break;
                case JudgeOccurance.JudgeType.EARLY_FAR:
                    ScoreManager.Instance.earlyFarCount++;
                    break;
                case JudgeOccurance.JudgeType.EARLY_PURE:
                    ScoreManager.Instance.earlyPureCount++;
                    break;
                case JudgeOccurance.JudgeType.MAX_PURE:
                    ScoreManager.Instance.maxPureCount++;
                    break;
                case JudgeOccurance.JudgeType.LATE_PURE:
                    ScoreManager.Instance.latePureCount++;
                    break;
                case JudgeOccurance.JudgeType.LATE_FAR:
                    ScoreManager.Instance.lateFarCount++;
                    break;
            }

        }

        noteForTouch.Dispose();
        judgeBacklog.Dispose();

    }
}
