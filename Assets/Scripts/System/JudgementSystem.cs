using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using ArcCore.Tags;
using Unity.Rendering;
using ArcCore.Utility;
using Unity.Mathematics;
using ArcCore.MonoBehaviours.EntityCreation;
using ArcCore;

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
    public Entity entity;
    public float3 position;

    public JudgeOccurance(JudgeType type, Entity entity, float3 position)
    {
        this.type = type;
        this.entity = entity;
        this.position = position;
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
    public BeginInitializationEntityCommandBufferSystem bufferSystem;

    public const float arcLeniencyGeneral = 2f;
    protected override void OnCreate()
    {
        Instance = this;
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        globalEntityManager = defaultWorld.EntityManager;
        bufferSystem = defaultWorld.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        
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
    protected override void OnUpdate()
    {
        //Only execute after full initialization
        if (!globalCurrentArcFingers.IsCreated)
            return;

        //Get data from statics
        NativeArray<TouchPoint> touchPoints = InputManager.Instance.touchPoints;
        NativeArray<NativeList<Entity>> noteForTouch = new NativeArray<NativeList<Entity>>(touchPoints.Length, Allocator.TempJob);
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Create copies of instances
        EntityManager entityManager = globalEntityManager;
        NativeArray<int> currentArcFingers = globalCurrentArcFingers;
        NativeArray<AABB2D> laneAABB2Ds = globalLaneAABB2Ds;

        NativeList<JudgeOccurance> judgeBacklog = new NativeList<JudgeOccurance>(Allocator.TempJob);

        EntityCommandBuffer buffer = bufferSystem.CreateCommandBuffer();

        for(int i = 0; i < noteForTouch.Length; i++)
        {
            noteForTouch[i] = new NativeList<Entity>(Allocator.TempJob);
        }



        // Handle all arcs //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in EntityReference entityRef, in LinearPosGroup linearPosGroup, in ColorID colorID, in StrictArcJudge strictArcJudge) 
            
                =>

            {

                ArcIsHit hit;
                ArcIsRed red;

                // Kill all points that have passed
                if (linearPosGroup.endTime < currentTime)
                {

                    hit = entityManager.GetComponentData<ArcIsHit>(entityRef.Value);
                    red = entityManager.GetComponentData<ArcIsRed>(entityRef.Value);

                    JudgeOccurance judgeOcc = 
                        new JudgeOccurance(
                            red.Value || !hit.Value ? JudgeOccurance.JudgeType.LOST : JudgeOccurance.JudgeType.MAX_PURE, 
                            entity,
                            new float3(linearPosGroup.startPosition, 0)
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
                        hit = new ArcIsHit()
                        {
                            Value = true
                        };
                        entityManager.SetComponentData(entityRef.Value, hit);

                        // Set red based on current finger id
                        red = new ArcIsRed()
                        {
                            Value = touchPoints[i].fingerId == currentArcFingers[colorID.Value] || currentArcFingers[colorID.Value] == -1 || !strictArcJudge.Value
                        };
                        entityManager.SetComponentData(entityRef.Value, red);

                        //If the point is strict, remove the current finger id to allow for switching
                        if (!strictArcJudge.Value)
                        {
                            currentArcFingers[colorID.Value] = -1;
                        }
                        //If there is no finger currently, allow there to be a new one permitted that the arc is not hit
                        else if (currentArcFingers[colorID.Value] != touchPoints[i].fingerId && !hit.Value)
                        {
                            currentArcFingers[colorID.Value] = touchPoints[i].fingerId;
                        }

                    }
                }

            }

        )
            .WithName("HandleArcs")
            .ScheduleParallel();

        
        // Handle all holds //
        Entities.WithAll<WithinJudgeRange, JudgeHoldPoint>().ForEach(

            (Entity entity, in EntityReference entityRef, in ChartTime time, in Track track) 

                =>

            {

                //Kill old entities and make lost
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    JudgeOccurance judgeOcc =
                        new JudgeOccurance(
                            JudgeOccurance.JudgeType.LOST,
                            entity,
                            new float3(Convert.TrackToX(track.Value), float2.zero)
                        );

                    judgeBacklog.Add(judgeOcc);

                    return;

                }

                //Reset hold value
                entityManager.SetComponentData<HoldIsHeld>(entityRef.Value, new HoldIsHeld()
                {
                    Value = false
                });

                HoldIsHeld held = entityManager.GetComponentData<HoldIsHeld>(entityRef.Value);

                //If the hold has not been broken
                if (held.Value)
                {
                    for (int i = 0; i < touchPoints.Length; i++)
                    {
                        if (
                            touchPoints[i].status != TouchPoint.Status.RELEASED &&
                            time.Value - touchPoints[i].time <= Constants.FarWindow &&
                            touchPoints[i].time - time.Value >= Constants.FarWindow &&
                            touchPoints[i].trackPlaneValid &&
                            touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                        )
                        {

                            entityManager.SetComponentData<HoldIsHeld>(entityRef.Value, new HoldIsHeld()
                            {
                                Value = true
                            });

                            JudgeOccurance judgeOcc =
                                new JudgeOccurance(
                                    JudgeOccurance.JudgeType.LOST,
                                    entity,
                                    new float3(Convert.TrackToX(track.Value), float2.zero)
                                );

                            judgeBacklog.Add(judgeOcc);

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
                            touchPoints[i].status == TouchPoint.Status.TAPPED &&
                            time.Value - touchPoints[i].time <= Constants.FarWindow &&
                            touchPoints[i].time - time.Value >= Constants.FarWindow &&
                            touchPoints[i].trackPlaneValid &&
                            touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                        )
                        {
                            //Check for judge later
                            noteForTouch[i].Add(entity);
                            return;
                        }
                    }
                }

            }

        )
            .WithName("HandleHolds")
            .ScheduleParallel();


        // Handle all arctaps //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in SinglePosition pos)

                =>

            {


                //Kill all remaining entities if they are overaged
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    JudgeOccurance judgeOcc =
                                new JudgeOccurance(
                                    JudgeOccurance.JudgeType.LOST,
                                    entity,
                                    new float3(pos.Value, 0)
                                );

                    judgeBacklog.Add(judgeOcc);

                    return;

                }

                for (int i = 0; i < touchPoints.Length; i++)
                {
                    if (
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
                        noteForTouch[i].Add(entity);
                        return;
                    }
                }

            }

        )
            .WithName("HandleArctaps")
            .ScheduleParallel();


        // Handle all taps //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in Track track)

                =>

            {

                for (int i = 0; i < touchPoints.Length; i++)
                {
                    if (
                        touchPoints[i].status == TouchPoint.Status.TAPPED &&
                        touchPoints[i].trackPlaneValid &&
                        touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                    )
                    {
                        //Check for judge later
                        noteForTouch[i].Add(entity);
                        return;
                    }
                }

            }

        )
            .WithName("HandleTaps")
            .ScheduleParallel();

        // Complete code and find mins //
        Dependency.Complete();
        Job.WithCode(

            () =>

            {

                for (int t = 0; t < noteForTouch.Length; t++)
                {

                    if (noteForTouch[t].Length == 0)
                    {
                        continue;
                    }

                    Entity minEntity = noteForTouch[t][0];
                    int minTime = entityManager.GetComponentData<ChartTime>(minEntity).Value;

                    for (int i = 0; i < noteForTouch[t].Length; i++)
                    {
                        int newTime = entityManager.GetComponentData<ChartTime>(noteForTouch[t][i]).Value;
                        if (newTime < minTime)
                        {
                            minEntity = noteForTouch[t][i];
                            minTime = newTime;
                        }
                    }

                    if(entityManager.HasComponent(minEntity, ComponentType.ReadOnly<Track>()))
                    {

                        Track track = entityManager.GetComponentData<Track>(minEntity);

                        JudgeOccurance.JudgeType type;

                        if (entityManager.HasComponent(minEntity, ComponentType.ReadOnly<JudgeHoldPoint>()))
                        {
                            type = JudgeOccurance.JudgeType.MAX_PURE;
                        }
                        else
                        {
                            ChartTime time = entityManager.GetComponentData<ChartTime>(minEntity);
                            type = JudgeOccurance.GetType(currentTime - time.Value);
                        }

                        JudgeOccurance judgeOcc =
                                    new JudgeOccurance(
                                        JudgeOccurance.JudgeType.LOST,
                                        minEntity,
                                        new float3(Convert.TrackToX(track.Value), float2.zero)
                                    );

                        judgeBacklog.Add(judgeOcc);

                    } 
                    else
                    {

                        SinglePosition pos = entityManager.GetComponentData<SinglePosition>(minEntity);
                        ChartTime time = entityManager.GetComponentData<ChartTime>(minEntity);

                        JudgeOccurance judgeOcc =
                                new JudgeOccurance(
                                    JudgeOccurance.GetType(currentTime - time.Value),
                                    minEntity,
                                    new float3(pos.Value, 0)
                                );

                        judgeBacklog.Add(judgeOcc);

                    }

                }

            }

        )
            .WithName("FinalizeMinimums")
            .Schedule();

        // Complete code and manage backlog //
        Dependency.Complete();
        for (int i = 0; i < judgeBacklog.Length; i++)
        {

            entityManager.AddComponent<Disabled>(judgeBacklog[i].entity);

            switch(judgeBacklog[i].type)
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

    }
}
