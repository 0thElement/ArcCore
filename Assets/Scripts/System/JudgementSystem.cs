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
    public EntityManager entityMan;
    public NativeArray<int> currentArcFgs;
    public NativeArray<AABB2D> glaneAABB2Ds;
    public BeginInitializationEntityCommandBufferSystem bufferSystem;

    public int* maxPureT, latePureT, earlyPureT, lateFarT, earlyFarT, lostT;

    public const float arcLeniencyGeneral = 2f;
    protected override void OnCreate()
    {
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityMan = defaultWorld.EntityManager;
        bufferSystem = defaultWorld.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        currentArcFgs = new NativeArray<int>(ArcEntityCreator.Instance.ArcColors.Length, Allocator.Persistent);
        glaneAABB2Ds = new NativeArray<AABB2D>(
            new AABB2D[] {
                new AABB2D(new float2(Convert.TrackToX(1), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(Convert.TrackToX(2), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(Convert.TrackToX(3), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(Convert.TrackToX(4), 0), new float2(Constants.LaneWidth, float.PositiveInfinity))
                    },
            Allocator.Persistent
            );
    }
    protected override void OnUpdate()
    {
        NativeArray<TouchPoint> touchPoints = InputManager.Instance.touchPoints;
        NativeArray<Entity> tapForTouch = new NativeArray<Entity>(touchPoints.Length, Allocator.TempJob);
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        EntityManager entityManager = entityMan;
        NativeArray<int> currentArcFingers = currentArcFgs;
        NativeArray<AABB2D> laneAABB2Ds = glaneAABB2Ds;

        EntityCommandBuffer buffer = bufferSystem.CreateCommandBuffer();

        //SWAP VALUES WITH SCORE MANAGER TO MAINTAIN BURSTABILITY
        ScoreManager.Instance.maxPureCount = maxPureT;
        ScoreManager.Instance.latePureCount = latePureT;
        ScoreManager.Instance.earlyPureCount = earlyPureT;
        ScoreManager.Instance.lateFarCount = lateFarT;
        ScoreManager.Instance.earlyFarCount = earlyFarT;
        ScoreManager.Instance.lostCount = lostT;

        maxPureT = ScoreManager.Instance.maxPureCount;
        latePureT = ScoreManager.Instance.latePureCount;
        earlyPureT = ScoreManager.Instance.earlyPureCount;
        lateFarT = ScoreManager.Instance.lateFarCount;
        earlyFarT = ScoreManager.Instance.earlyFarCount;
        lostT = ScoreManager.Instance.lostCount;

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
                            lostT++;
                        }
                        else
                        {
                            maxPureT++;
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

                                maxPureT++;
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

                                lostT++;
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

                        lostT++;

                    }

                    ShouldCutOff holdSCO = entityManager.GetComponentData<ShouldCutOff>(entityRef.Value);
                    Track track = entityManager.GetComponentData<Track>(entity);

                    //If the hold has been broken
                    if(holdSCO.Value <= 0)
                    {
                        for(int i = 0; i < touchPoints.Length; i++)
                        {
                            if (
                                entityManager.Exists(tapForTouch[i]) && 
                                entityManager.GetComponentData<ChartTime>(tapForTouch[i]).Value >= time.Value &&
                                touchPoints[i].status == TouchPoint.Status.TAPPED &&
                                time.Value - touchPoints[i].time <= Constants.FarWindow &&
                                touchPoints[i].time - time.Value >= Constants.FarWindow &&
                                touchPoints[i].trackPlaneValid &&
                                touchPoints[i].trackPlane.CollidingWith(laneAABB2Ds[track.Value])
                            )
                            {
                                //Check for judge later
                                tapForTouch[i] = entity;
                            }
                        }
                    } else
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

                                    entityManager.SetComponentData<ShouldCutOff>(entityRef.Value, new ShouldCutOff()
                                    {
                                        Value = 0f
                                    });

                                } else
                                {

                                    entityManager.SetComponentData<ShouldCutOff>(entityRef.Value, new ShouldCutOff()
                                    {
                                        Value = 1f
                                    });

                                    buffer.AddComponent<Disabled>(entity);

                                    maxPureT++;

                                }

                            }
                        }
                    }
                }

            }
        ).Schedule(new JobHandle());
    }
}
