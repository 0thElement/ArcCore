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

public unsafe class JudgementSystem : SystemBase
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
        NativeArray<Entity> noteForTouch = new NativeArray<Entity>(touchPoints.Length, Allocator.TempJob);
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Create copies of instances
        EntityManager entityManager = globalEntityManager;
        NativeArray<int> currentArcFingers = globalCurrentArcFingers;
        NativeArray<AABB2D> laneAABB2Ds = globalLaneAABB2Ds;

        EntityCommandBuffer buffer = bufferSystem.CreateCommandBuffer();

        //Set pointers to score contents
        int* maxPureT = ScoreManager.Instance.maxPureCount;
        int* latePureT = ScoreManager.Instance.latePureCount;
        int* earlyPureT = ScoreManager.Instance.earlyPureCount;
        int* lateFarT = ScoreManager.Instance.lateFarCount;
        int* earlyFarT = ScoreManager.Instance.earlyFarCount;
        int* lostT = ScoreManager.Instance.lostCount;
        
        // Foreach loop
        JobHandle loopHandle = Entities.WithAll<WithinJudgeRange>().ForEach(
            (Entity entity, in EntityReference entityRef) =>
            {
                
                // Arcs only
                if(entityManager.HasComponent(entity, ComponentType.ReadOnly<ColorID>()))
                {
                    LinearPosGroup linearPosGroup = entityManager.GetComponentData<LinearPosGroup>(entity);
                    ColorID colorID = entityManager.GetComponentData<ColorID>(entity);
                    StrictArcJudge strictArcJudge = entityManager.GetComponentData<StrictArcJudge>(entity);

                    ArcIsHit hit = entityManager.GetComponentData<ArcIsHit>(entityRef.Value);
                    ArcIsRed red = entityManager.GetComponentData<ArcIsRed>(entityRef.Value);

                    //Dead arcs -> judge
                    if (linearPosGroup.endTime < currentTime)
                    {

                        if(red.Value || !hit.Value) 
                        {
                            (*lostT)++;
                        }
                        else
                        {
                            (*maxPureT)++;
                        }

                        buffer.AddComponent<Disabled>(entity);

                        return;

                    }

                    for (int i = 0; i < touchPoints.Length; i++)
                    {

                        //Calculate whether or not the current finger is correct (or if it matters)
                        bool correctFinger = touchPoints[i].fingerId == currentArcFingers[colorID.Value] || currentArcFingers[colorID.Value] == -1 || !strictArcJudge.Value;

                        //Arc hit by any finger
                        if (
                            linearPosGroup.startTime <= touchPoints[i].time &&
                            linearPosGroup.endTime >= touchPoints[i].time &&
                            touchPoints[i].status != TouchPoint.Status.RELEASED &&
                            touchPoints[i].inputPlaneValid &&
                            touchPoints[i].inputPlane.CollidingWith(new AABB2D(linearPosGroup.PosAt(currentTime), new float2(arcLeniencyGeneral)))
                        )
                        {

                            //Arc hit by *correct* finger -> instant pure
                            if (correctFinger)
                            {
                                hit = new ArcIsHit()
                                {
                                    Value = true
                                };
                                
                                entityManager.SetComponentData<ArcIsHit>(entity, hit);

                                red = new ArcIsRed()
                                {
                                    Value = false
                                };

                                entityManager.SetComponentData<ArcIsRed>(entity, red);

                                (*maxPureT)++;
                            }
                            //Arc hit by *incorrect* finger -> instant lost
                            else
                            {
                                hit = new ArcIsHit()
                                {
                                    Value = false
                                };

                                entityManager.SetComponentData<ArcIsHit>(entity, hit);

                                red = new ArcIsRed()
                                {
                                    Value = true
                                };

                                entityManager.SetComponentData<ArcIsRed>(entity, red);

                                (*lostT)++;
                            }
                            
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

                    return;

                }

                // All other types have this component
                ChartTime time = entityManager.GetComponentData<ChartTime>(entity);

                // Holds only
                if(entityManager.HasComponent(entity, ComponentType.ReadOnly<JudgeHoldPoint>()))
                {

                    //Kill old entities and make lost
                    if(time.Value + Constants.FarWindow < currentTime)
                    {

                        buffer.AddComponent<Disabled>(entity);

                        (*lostT)++;
                        return;

                    }

                    entityManager.SetComponentData<ShouldCutOff>(entityRef.Value, new ShouldCutOff()
                    {
                        Value = 0f
                    });

                    HoldHeldJudge heldJudge = entityManager.GetComponentData<HoldHeldJudge>(entityRef.Value);
                    Track track = entityManager.GetComponentData<Track>(entity);

                    //If the hold has been broken
                    if(!heldJudge.Value)
                    {
                        for(int i = 0; i < touchPoints.Length; i++)
                        {
                            if (
                                !(entityManager.Exists(noteForTouch[i]) || 
                                entityManager.GetComponentData<ChartTime>(noteForTouch[i]).Value < time.Value) &&
                                touchPoints[i].status == TouchPoint.Status.TAPPED &&
                                time.Value - touchPoints[i].time <= Constants.FarWindow &&
                                touchPoints[i].time - time.Value >= Constants.FarWindow &&
                                touchPoints[i].trackPlaneValid &&
                                touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                            )
                            {
                                //Check for judge later
                                noteForTouch[i] = entity;
                                return;
                            }
                        }
                    } else
                    //If the hold has not been broken
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

                                    entityManager.SetComponentData<HoldHeldJudge>(entityRef.Value, new HoldHeldJudge()
                                    {
                                        Value = false
                                    });

                                } else
                                {

                                    entityManager.SetComponentData<ShouldCutOff>(entityRef.Value, new ShouldCutOff()
                                    {
                                        Value = 1f
                                    });

                                    entityManager.SetComponentData<HoldHeldJudge>(entityRef.Value, new HoldHeldJudge()
                                    {
                                        Value = true
                                    });

                                    buffer.AddComponent<Disabled>(entity);

                                    (*maxPureT)++;
                                    return;

                                }

                            }
                        }
                    }
                    return;
                }

                //Kill all remaining entities if they are overaged
                if (time.Value + Constants.FarWindow < currentTime)
                {

                    buffer.AddComponent<Disabled>(entity);

                    (*lostT)++;
                    return;

                }

                //Handle valid arctaps
                if(entityManager.HasComponent(entity, ComponentType.ReadOnly<SinglePosition>()))
                {

                    SinglePosition pos = entityManager.GetComponentData<SinglePosition>(entity);

                    for (int i = 0; i < touchPoints.Length; i++)
                    {
                        if (
                            !(entityManager.Exists(noteForTouch[i]) ||
                            entityManager.GetComponentData<ChartTime>(noteForTouch[i]).Value < time.Value) &&
                            touchPoints[i].status == TouchPoint.Status.TAPPED &&
                            time.Value - touchPoints[i].time <= Constants.FarWindow &&
                            touchPoints[i].time - time.Value >= Constants.FarWindow &&
                            touchPoints[i].inputPlaneValid &&
                            touchPoints[i].inputPlane.CollidingWith(new AABB2D(pos.Value, new float2(arcLeniencyGeneral)))
                        )
                        {
                            //Check for judge later
                            noteForTouch[i] = entity;
                            return;
                        }
                    }

                    return;

                }

                //Handle all taps
                Track trackTap = entityManager.GetComponentData<Track>(entity);

                for (int i = 0; i < touchPoints.Length; i++)
                {
                    if (
                        !(entityManager.Exists(noteForTouch[i]) ||
                        entityManager.GetComponentData<ChartTime>(noteForTouch[i]).Value < time.Value) &&
                        touchPoints[i].status == TouchPoint.Status.TAPPED &&
                        time.Value - touchPoints[i].time <= Constants.FarWindow &&
                        touchPoints[i].time - time.Value >= Constants.FarWindow &&
                        touchPoints[i].trackPlaneValid &&
                        touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[trackTap.Value])
                    )
                    {
                        //Check for judge later
                        noteForTouch[i] = entity;
                        return;
                    }
                }

            }
        ).Schedule(new JobHandle());

        JobHandle finalizeHandle = Job.WithCode(() =>
        {

            for(int i = 0; i < noteForTouch.Length; i++)
            {
                if (entityManager.Exists(noteForTouch[i]))
                    continue;

                //HANDLE HOLDS
                if (entityManager.HasComponent<JudgeHoldPoint>(noteForTouch[i]))
                {

                    EntityReference entityRef = entityManager.GetComponentData<EntityReference>(noteForTouch[i]);

                    entityManager.SetComponentData<ShouldCutOff>(entityRef.Value, new ShouldCutOff()
                    {
                        Value = 1f
                    });

                    entityManager.SetComponentData<HoldHeldJudge>(entityRef.Value, new HoldHeldJudge()
                    {
                        Value = true
                    });

                    buffer.AddComponent<Disabled>(noteForTouch[i]);

                    (*maxPureT)++;
                    return;

                }

                ChartTime time = entityManager.GetComponentData<ChartTime>(noteForTouch[i]);

                if (currentTime - time.Value > Constants.FarWindow)
                    (*lostT)++;
                else if (currentTime - time.Value > Constants.PureWindow)
                    (*earlyFarT)++;
                else if (currentTime - time.Value > Constants.MaxPureWindow)
                    (*earlyPureT)++;
                else if (currentTime - time.Value > -Constants.MaxPureWindow)
                    (*maxPureT)++;
                else if (currentTime - time.Value > -Constants.PureWindow)
                    (*latePureT)++;
                else (*lateFarT)++;

                buffer.AddComponent<Disabled>(noteForTouch[i]);

            }

        }).Schedule(loopHandle);
    }
}
