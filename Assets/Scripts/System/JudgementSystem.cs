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
    public bool deleteEn;

    public JudgeOccurance(JudgeType type, Entity entity, float3 position, bool deleteEn)
    {
        this.type = type;
        this.entity = entity;
        this.position = position;
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
    public struct EntityWithLoadedChartTime
    {
        public readonly Entity en;
        public readonly ChartTime ch;

        public EntityWithLoadedChartTime(Entity en, ChartTime ch)
        {
            this.en = en;
            this.ch = ch;
        }
    }
    protected override void OnUpdate()
    {
        //Only execute after full initialization
        if (!globalCurrentArcFingers.IsCreated)
            return;

        //Get data from statics
        NativeArray<TouchPoint> touchPoints = InputManager.Instance.touchPoints;
        NativeArray<EntityWithLoadedChartTime> noteForTouch = new NativeArray<EntityWithLoadedChartTime>(touchPoints.Length, Allocator.TempJob);
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Create copies of instances
        EntityManager entityManager = globalEntityManager;
        NativeArray<int> currentArcFingers = globalCurrentArcFingers;
        NativeArray<AABB2D> laneAABB2Ds = globalLaneAABB2Ds;

        NativeList<JudgeOccurance> judgeBacklog = new NativeList<JudgeOccurance>(Allocator.TempJob);

        EntityCommandBuffer buffer = bufferSystem.CreateCommandBuffer();



        // Handle all arcs //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in EntityReference entityRef, in LinearPosGroup linearPosGroup, in ColorID colorID, in StrictArcJudge strictArcJudge) 
            
                =>

            {

                HitState hit = entityManager.GetComponentData<HitState>(entityRef.Value); ;
                ArcIsRed red;

                // Kill all points that have passed
                if (linearPosGroup.endTime < currentTime)
                {

                    red = entityManager.GetComponentData<ArcIsRed>(entityRef.Value);

                    JudgeOccurance judgeOcc = 
                        new JudgeOccurance(
                            red.Value || !hit.HitRaw ? JudgeOccurance.JudgeType.LOST : JudgeOccurance.JudgeType.MAX_PURE, 
                            entity,
                            new float3(linearPosGroup.startPosition, 0),
                            false
                        );

                    judgeBacklog.Add(judgeOcc);

                    hit = new HitState()
                    {
                        Value = hit.HitRaw ? 0f : 2f,
                        HitRaw = hit.HitRaw
                    };

                    entityManager.SetComponentData<HitState>(entityRef.Value, hit);

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

                        // Set hit.HitRaw to true
                        hit = new HitState()
                        {
                            Value = hit.Value,
                            HitRaw = true
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
                        else if (currentArcFingers[colorID.Value] != touchPoints[i].fingerId && !hit.HitRaw)
                        {
                            currentArcFingers[colorID.Value] = touchPoints[i].fingerId;
                        }

                    }
                }

            }

        )
            .WithName("HandleArcs")
            .Schedule();

        
        // Handle all holds //
        Entities.WithAll<WithinJudgeRange, JudgeHoldPoint>().ForEach(

            (Entity entity, in EntityReference entityRef, in ChartTime time, in Track track) 

                =>

            {


                HitState hit = entityManager.GetComponentData<HitState>(entityRef.Value);

                //Kill old entities and make lost
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    JudgeOccurance judgeOcc =
                        new JudgeOccurance(
                            JudgeOccurance.JudgeType.LOST,
                            entity,
                            new float3(Convert.TrackToX(track.Value), float2.zero),
                            false
                        );

                    judgeBacklog.Add(judgeOcc);

                    return;

                }

                //Reset hold value
                entityManager.SetComponentData<HitState>(entityRef.Value, new HitState()
                {
                    Value = hit.Value == 0 ? 0 : 2,
                    HitRaw = hit.HitRaw
                });

                //If the hold has not been broken
                if (hit.HitRaw)
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
                                entityManager.SetComponentData<HitState>(entityRef.Value, new HitState()
                                {
                                    Value = hit.Value,
                                    HitRaw = false
                                });

                            }
                            else
                            {

                                JudgeOccurance judgeOcc =
                                        new JudgeOccurance(
                                            JudgeOccurance.JudgeType.MAX_PURE,
                                            entity,
                                            new float3(Convert.TrackToX(track.Value), float2.zero),
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
                            noteForTouch[i] = new EntityWithLoadedChartTime(entity, time);
                            return;
                        }
                    }
                }

            }

        )
            .WithName("HandleHolds")
            .Schedule();


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
                                    new float3(pos.Value, 0),
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
                        noteForTouch[i] = new EntityWithLoadedChartTime(entity, time);
                        return;
                    }
                }

            }

        )
            .WithName("HandleArctaps")
            .Schedule();


        // Handle all taps //
        Entities.WithAll<WithinJudgeRange>().ForEach(

            (Entity entity, in ChartTime time, in Track track)

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
                        noteForTouch[i] = new EntityWithLoadedChartTime(entity, time);
                        return;
                    }
                }

            }

        )
            .WithName("HandleTaps")
            .Schedule();

        // Complete code and find types //
        Dependency.Complete();
        Job.WithCode(

            () =>

            {

                for (int t = 0; t < noteForTouch.Length; t++)
                {
                    Entity minEntity = noteForTouch[t].en;
                    ChartTime time = noteForTouch[t].ch;
                    if(entityManager.HasComponent(minEntity, ComponentType.ReadOnly<Track>()))
                    {

                        Track track = entityManager.GetComponentData<Track>(minEntity);

                        JudgeOccurance.JudgeType type;
                        bool delEn;

                        if (entityManager.HasComponent(minEntity, ComponentType.ReadOnly<JudgeHoldPoint>()))
                        {
                            type = JudgeOccurance.JudgeType.MAX_PURE;
                            delEn = false;

                            entityManager.SetComponentData<HitState>(
                                entityManager.GetComponentData<EntityReference>(noteForTouch[t].en).Value, 
                                new HitState() {
                                    Value = 1,
                                    HitRaw = true
                            });
                        }
                        else
                        {
                            type = JudgeOccurance.GetType(currentTime - time.Value);
                            delEn = true;
                        }

                        JudgeOccurance judgeOcc =
                                    new JudgeOccurance(
                                        type,
                                        minEntity,
                                        new float3(Convert.TrackToX(track.Value), float2.zero),
                                        delEn
                                    );

                        judgeBacklog.Add(judgeOcc);

                    } 
                    else
                    {

                        SinglePosition pos = entityManager.GetComponentData<SinglePosition>(minEntity);

                        JudgeOccurance judgeOcc =
                                new JudgeOccurance(
                                    JudgeOccurance.GetType(currentTime - time.Value),
                                    minEntity,
                                    new float3(pos.Value, 0),
                                    true
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

            if (judgeBacklog[i].deleteEn)
            {
                entityManager.AddComponent<Disabled>(
                    entityManager.GetComponentData<EntityReference>(judgeBacklog[i].entity).Value
                    );
            }

            entityManager.AddComponent<Disabled>(judgeBacklog[i].entity);

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

    }
}
