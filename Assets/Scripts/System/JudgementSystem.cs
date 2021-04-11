using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using Unity.Rendering;
using ArcCore.Utility;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.MonoBehaviours.EntityCreation;
using ArcCore;
using ArcCore.Tags;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class JudgementSystem : SystemBase
{
    public static JudgementSystem Instance { get; private set; }
    public EntityManager entityManager;
    public NativeArray<int> currentArcFingers;
    public NativeArray<AABB2D> laneAABB2Ds;

    public bool IsReady => currentArcFingers.IsCreated;
    public EntityQuery tapQuery, arcQuery, arctapQuery, holdQuery;

    public const float arcLeniencyGeneral = 2f;
    public static readonly float2 arctapBoxExtents = new float2(4f, 1f); //DUMMY VALUES

    BeginSimulationEntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        Instance = this;
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;

        beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        laneAABB2Ds = new NativeArray<AABB2D>(
            new AABB2D[] {
                new AABB2D(new float2(ArccoreConvert.TrackToX(1), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(ArccoreConvert.TrackToX(2), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(ArccoreConvert.TrackToX(3), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new AABB2D(new float2(ArccoreConvert.TrackToX(4), 0), new float2(Constants.LaneWidth, float.PositiveInfinity))
                    },
            Allocator.Persistent
            );

        holdQuery = GetEntityQuery(
                typeof(HoldFunnelPtr),
                typeof(ChartTime),
                typeof(Track),
                typeof(WithinJudgeRange),
                typeof(JudgeHoldPoint)
            );

        arctapQuery = GetEntityQuery(
                typeof(EntityReference),
                typeof(ChartTime),
                typeof(ChartPosition),
                typeof(WithinJudgeRange)
            );


        tapQuery = GetEntityQuery(
                typeof(EntityReference),
                typeof(ChartTime),
                typeof(Track),
                typeof(WithinJudgeRange)
            );

        arcQuery = GetEntityQuery(
                typeof(ArcFunnelPtr),
                typeof(LinearPosGroup),
                typeof(ColorID),
                typeof(StrictArcJudge),
                typeof(WithinJudgeRange)
            );
    }
    public void SetupColors()
    {
        currentArcFingers = new NativeArray<int>(ArcEntityCreator.Instance.arcColors.Length, Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        currentArcFingers.Dispose();
        laneAABB2Ds.Dispose();
    }
    protected override unsafe void OnUpdate()
    {
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

        void JudgeLost()
        {
            lostCount++;
            combo = 0;
        }
        void JudgeMaxPure()
        {
            maxPureCount++;
            combo++;
        }
        void Judge(int time)
        {
            int timeDiff = time - currentTime;
            if (timeDiff > Constants.FarWindow)
            {
                lostCount++;
                combo = 0;
            }
            else if (timeDiff > Constants.PureWindow)
            {
                earlyFarCount++;
                combo++;
            }
            else if (timeDiff > Constants.MaxPureWindow)
            {
                earlyPureCount++;
                combo++;
            }
            else if (timeDiff > -Constants.MaxPureWindow)
            {
                JudgeMaxPure();
            }
            else if (timeDiff > -Constants.PureWindow)
            {
                latePureCount++;
                combo++;
            }
            else
            {
                lateFarCount++;
                combo++;
            }
        } 

        //Execute for each touch
        for (int i = 0; i < InputManager.MaxTouches; i++)
        {
            TouchPoint touch = InputManager.Get(i);
            bool tapped = false;

            //Track taps
            if (touch.TrackRangeValid) {

                //Hold notes
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    (Entity entity, ref HoldLastJudge held, ref ChartHoldTime holdTime, ref HoldLastJudge lastJudge, in ChartTimeSpan span, in ChartPosition position)

                        =>

                    {
                        //Invalidate holds if they require a tap and this touch has been parsed as a tap already
                        if (!held.value && tapped) return;

                        //Invalidate holds out of time range
                        if (!holdTime.CheckStart(Constants.FarWindow)) return;

                        //Disable judgenotes
                        void Disable()
                        {
                            commandBuffer.RemoveComponent<WithinJudgeRange>(entity);
                            commandBuffer.AddComponent<PastJudgeRange>(entity);
                        }

                        //Increment or kill holds out of time for judging
                        if (holdTime.CheckOutOfRange(currentTime))
                        {
                            JudgeLost();
                            lastJudge.value = false;

                            if (!holdTime.Increment(span)) 
                                Disable();
                        }

                        //Invalidate holds not in range; should also rule out all invalid data, i.e. positions with a lane of -1
                        if (!touch.trackRange.Contains(position.lane)) return;

                        //Holds not requiring a tap
                        if(held.value)
                        {
                            //If valid:
                            if (touch.status != TouchPoint.Status.RELEASED)
                            {
                                JudgeMaxPure();
                                lastJudge.value = true;

                                if (!holdTime.Increment(span)) 
                                    Disable();
                            }
                            //If invalid:
                            else
                            {
                                held.value = false;
                            }
                        }
                        //Holds requiring a tap
                        else if(touch.status == TouchPoint.Status.TAPPED)
                        {
                            JudgeMaxPure();
                            lastJudge.value = true;

                            if (!holdTime.Increment(span)) 
                                Disable();

                            tapped = true;
                        }
                    }

                );

                if (!tapped) {
                    //Tap notes; no EntityReference, those only exist on arctaps
                    Entities.WithAll<WithinJudgeRange>().WithNone<EntityReference>().ForEach(

                        (Entity entity, in ChartTime time, in ChartPosition position)

                            =>

                        {
                            //Invalidate if already tapped
                            if (tapped) return;

                            //Increment or kill holds out of time for judging
                            if (time.CheckOutOfRange(currentTime))
                            {
                                JudgeLost();
                                entityManager.DestroyEntity(entity);
                            }

                            //Invalidate if not in range of a tap; should also rule out all invalid data, i.e. positions with a lane of -1
                            if (!touch.trackRange.Contains(position.lane)) return;

                            //Register tap lul
                            Judge(time.value);
                            tapped = true;

                            //Destroy tap
                            entityManager.DestroyEntity(entity);
                        }

                    );
                }

            }

            //Refuse to judge arctaps if above checks have found a tap already
            if (!tapped)
            {
                //Tap notes; no EntityReference, those only exist on arctaps
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    (Entity entity, in ChartTime time, in ChartPosition position, in EntityReference enRef)

                        =>

                    {
                        //Invalidate if already tapped
                        if (tapped) return;

                        //Increment or kill holds out of time for judging
                        if (time.CheckOutOfRange(currentTime))
                        {
                            JudgeLost();
                            entityManager.DestroyEntity(entity);
                            entityManager.DestroyEntity(enRef.Value);
                        }

                        //Invalidate if not in range of a tap; should also rule out all invalid data, i.e. positions with a lane of -1
                        if (!touch.inputPlane.CollidesWith(new AABB2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
                            return;

                        //Register tap lul
                        Judge(time.value);
                        tapped = true;

                        //Destroy tap
                        entityManager.DestroyEntity(entity);
                    }

                );
            }

        }

        // Handle arc fingers once they are released //
        for(int i = 0; i < currentArcFingers.Length; i++)
        {
            if(currentArcFingers[i] != -1)
            {
                bool remove = true;
                for(int j = 0; j < InputManager.Instance.touchPoints.Length; j++)
                {
                    bool statusIsReleased = InputManager.Instance.touchPoints[j].status == TouchPoint.Status.RELEASED;
                    if (InputManager.Instance.touchPoints[j].fingerId == currentArcFingers[i])
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
        NativeArray<Entity> arcEns = arcQuery.ToEntityArray(Allocator.TempJob);
        for (int en = 0; en < arcEns.Length; en++)
        {
            Entity entity = arcEns[en];

            // Get entity components
            ArcFunnelPtr arcFunnelPtr     = entityManager.GetComponentData<ArcFunnelPtr>  (entity);
            ColorID colorID               = entityManager.GetComponentData<ColorID>       (entity);
            LinearPosGroup linearPosGroup = entityManager.GetComponentData<LinearPosGroup>(entity);
            StrictArcJudge strictArcJudge = entityManager.GetComponentData<StrictArcJudge>(entity);

            // Get arc funnel pointer to allow indirect struct access
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

                entityManager.AddComponent(entity, typeof(Disabled));

                return;

            }

            // Loop through all touch points
            for (int i = 0; i < InputManager.Instance.touchPoints.Length; i++)
            {

                // Arc hit by finger
                if (linearPosGroup.startTime <= InputManager.Instance.touchPoints[i].time &&
                    linearPosGroup.endTime >= InputManager.Instance.touchPoints[i].time &&
                    InputManager.Instance.touchPoints[i].status != TouchPoint.Status.RELEASED &&
                    InputManager.Instance.touchPoints[i].InputPlaneValid &&
                    InputManager.Instance.touchPoints[i].inputPlane.CollidesWith(
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
                       (InputManager.Instance.touchPoints[i].fingerId != currentArcFingers[colorID.Value] &&
                        currentArcFingers[colorID.Value] != -1) || !strictArcJudge.Value;

                    // If the point not is strict, remove the current finger id to allow for switching
                    if (!strictArcJudge.Value)
                    {
                        currentArcFingers[colorID.Value] = -1;
                    }
                    // If there is no finger currently, allow there to be a new one permitted that the arc is not hit
                    else if (currentArcFingers[colorID.Value] != InputManager.Instance.touchPoints[i].fingerId && !arcFunnelPtrD->isHit)
                    {
                        currentArcFingers[colorID.Value] = InputManager.Instance.touchPoints[i].fingerId;
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

                    entityManager.AddComponent(entity, typeof(Disabled));

                }
            }
        }

        // Destroy array after use
        arcEns.Dispose();

        // Repopulate managed data
        ScoreManager.Instance.currentCombo = combo;
        ScoreManager.Instance.maxPureCount = maxPureCount;
        ScoreManager.Instance.latePureCount = latePureCount;
        ScoreManager.Instance.lateFarCount = lateFarCount;
        ScoreManager.Instance.earlyPureCount = earlyPureCount;
        ScoreManager.Instance.earlyFarCount = earlyFarCount;
        ScoreManager.Instance.lostCount = lostCount;


    }
}
