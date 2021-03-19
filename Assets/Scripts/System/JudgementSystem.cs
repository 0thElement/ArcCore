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

public struct JudgeEntityRef
{
    public enum EntityType
    {
        HOLD,
        ARCTAP,
        TAP
    }

    public Entity entity;
    public int time;
    public EntityType type;

    public JudgeEntityRef(Entity entity, int time, EntityType type)
    {
        this.entity = entity;
        this.time = time;
        this.type = type;
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
        NativeArray<JudgeEntityRef> noteForTouch = new NativeArray<JudgeEntityRef>(touchPoints.Length, Allocator.TempJob);
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Setup noteForTouch 
        for (int i = 0; i < noteForTouch.Length; i++)
        {
            noteForTouch[i] = new JudgeEntityRef(new Entity(), int.MaxValue, JudgeEntityRef.EntityType.TAP);
        }
        //Create copies of instances
        EntityManager entityManager = globalEntityManager;
        NativeArray<int> currentArcFingers = globalCurrentArcFingers;
        NativeArray<bool> arcFingersAreUsed = new NativeArray<bool>(currentArcFingers.Length, Allocator.TempJob);
        NativeArray<AABB2D> laneAABB2Ds = globalLaneAABB2Ds;

        // Handle arc fingers once they are released //
        for(int i = 0; i < currentArcFingers.Length; i++)
        {

            if(currentArcFingers[i] != -1)
            {
                bool remove = true;
                for(int j = 0; j < touchPoints.Length; j++)
                {
                    bool statusIsReleased = touchPoints[j].status == TouchPoint.Status.RELEASED;
                    if (touchPoints[j].fingerId == currentArcFingers[i])
                    {
                        if (!statusIsReleased)
                        {
                            remove = false;
                        }
                        break;
                    }
                }

                if(remove)
                {
                    currentArcFingers[i] = -1;
                }
            }

        }


        // Handle all arcs //
        Entities.WithAll<WithinJudgeRange>().WithoutBurst().WithStructuralChanges().ForEach(

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

                    ScoreManager.Instance.AddJudge(JudgeManage.JudgeType.LOST);
                    //PARTICLE MANAGEMENT HERE OR IN SCORE MANAGER

                    entityManager.DestroyEntity(entity);

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
                           (touchPoints[i].fingerId != currentArcFingers[colorID.Value] && 
                            currentArcFingers[colorID.Value] != -1) || !strictArcJudge.Value;

                        // If the point not is strict, remove the current finger id to allow for switching
                        if (!strictArcJudge.Value)
                        {
                            currentArcFingers[colorID.Value] = -1;
                        }
                        // If there is no finger currently, allow there to be a new one permitted that the arc is not hit
                        else if (currentArcFingers[colorID.Value] != touchPoints[i].fingerId && !arcFunnelPtrD->isHit)
                        {
                            currentArcFingers[colorID.Value] = touchPoints[i].fingerId;
                        }

                        // Kill arc judger
                        if (arcFunnelPtrD->isRed)
                        {
                            ScoreManager.Instance.AddJudge(JudgeManage.JudgeType.LOST);
                        }
                        else
                        {
                            ScoreManager.Instance.AddJudge(JudgeManage.JudgeType.MAX_PURE);
                        }

                        entityManager.DestroyEntity(entity);

                    }
                }

            }

        )
            .WithName("HandleArcs")
            .Run();

        
        // Handle all holds //
        Entities.WithAll<WithinJudgeRange, JudgeHoldPoint>().WithoutBurst().WithStructuralChanges().ForEach(

            (Entity entity, in HoldFunnelPtr holdFunnelPtr, in ChartTime time, in Track track) 

                =>

            {

                HoldFunnel* holdFunnelPtrD = holdFunnelPtr.Value;

                //Kill old entities and make lost
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    ScoreManager.Instance.AddJudge(JudgeManage.JudgeType.LOST);

                    entityManager.DestroyEntity(entity);

                    return;

                }

                //Reset hold value
                if (currentTime > time.Value)
                {
                    holdFunnelPtrD->visualState = LongnoteVisualState.JUDGED_LOST;
                }

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

                                ScoreManager.Instance.AddJudge(JudgeManage.JudgeType.MAX_PURE);

                                entityManager.DestroyEntity(entity);

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
                           !entityManager.Exists(noteForTouch[i].entity) &&
                            noteForTouch[i].time < time.Value &&
                            touchPoints[i].status == TouchPoint.Status.TAPPED &&
                            time.Value - touchPoints[i].time <= Constants.FarWindow &&
                            touchPoints[i].time - time.Value >= Constants.FarWindow &&
                            touchPoints[i].trackPlaneValid &&
                            touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                        )
                        {
                            //Check for judge later
                            noteForTouch[i] = new JudgeEntityRef(entity, time.Value, JudgeEntityRef.EntityType.HOLD);
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

                    JudgeManage judgeOcc =
                                new JudgeOccurance(
                                    JudgeManage.JudgeType.LOST,
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


                    JudgeManage.JudgeType type;
                    bool delEn;

                    if (noteForTouch[t].isHold)
                    {
                        type = JudgeManage.JudgeType.MAX_PURE;
                        delEn = false;

                        noteForTouch[t].holdFunnelPtr.Value->visualState = LongnoteVisualState.JUDGED_PURE;
                    }
                    else
                    {
                        type = JudgeManage.GetType(currentTime - time.Value);
                        delEn = true;
                    }

                    JudgeManage judgeOcc =
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
                case JudgeManage.JudgeType.LOST:
                    ScoreManager.Instance.lostCount++;
                    break;
                case JudgeManage.JudgeType.EARLY_FAR:
                    ScoreManager.Instance.earlyFarCount++;
                    break;
                case JudgeManage.JudgeType.EARLY_PURE:
                    ScoreManager.Instance.earlyPureCount++;
                    break;
                case JudgeManage.JudgeType.MAX_PURE:
                    ScoreManager.Instance.maxPureCount++;
                    break;
                case JudgeManage.JudgeType.LATE_PURE:
                    ScoreManager.Instance.latePureCount++;
                    break;
                case JudgeManage.JudgeType.LATE_FAR:
                    ScoreManager.Instance.lateFarCount++;
                    break;
            }

        }

        noteForTouch.Dispose();
        judgeBacklog.Dispose();

    }
}
