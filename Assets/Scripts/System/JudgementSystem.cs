//COMMENT THIS OUT IN ORDER TO HIDE CONTENTS OF THIS FILE
//#define FlooferWroteThisHahahahahaSexUwu


#if FlooferWroteThisHahahahahaSexUwu
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

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class JudgementSystem : SystemBase
{
    public static JudgementSystem Instance { get; private set; }
    public EntityManager entityManager;

    public bool IsReady => arcFingers.IsCreated;
    public EntityQuery tapQuery, arcQuery, arctapQuery, holdQuery;

    public const float arcLeniencyGeneral = 2f;
    public static readonly float2 arctapBoxExtents = new float2(4f, 1f); //DUMMY VALUES

    public NativeMatrIterator<ArcJudge> arcJudges;
    public NativeArray<ArcCompleteState> arcStates;
    public NativeArray<int> arcFingers;
    public NativeArray<AffArc> rawArcs;

    BeginSimulationEntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;

    private enum JudgeEnType
    {
        NONE,
        ARCTAP,
        TAP,
        HOLD
    }

    protected override void OnCreate()
    {
        Instance = this;
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;

        beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    public void SetupColors()
    {
        arcFingers = new NativeArray<int>(utils.newarr_fill_aclen(-1), Allocator.Persistent);
        arcStates = new NativeArray<ArcCompleteState>(utils.newarr_fill_aclen(new ArcCompleteState(ArcState.Normal)), Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        arcJudges.Dispose();
        arcFingers.Dispose();
        arcStates.Dispose();
        rawArcs.Dispose();
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

        //Clean up red arcs
        for (int c = 0; c < ArcEntityCreator.ColorCount; c++)
        {
            if (arcJudges.PeekAhead(c, 1).time > currentTime + Constants.FarWindow)
            {
                arcFingers[c] = -1;
                if (arcStates[c].state == ArcState.Red) 
                    arcStates[c] = new ArcCompleteState(arcStates[c], ArcState.Unheld);
            }
        }

        //Execute for each touch
        Job.WithBurst(FloatMode.)
        for (int i = 0; i < InputManager.MaxTouches; i++)
        {
            TouchPoint touch = InputManager.Get(i);


            //Track taps
            if (touch.TrackValid) {

                //Hold notes
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    (Entity entity, ref HoldIsTapped held, ref ChartIncrTime holdTime, in ChartTimeSpan span, in ChartLane position)

                        =>

                    {
                        //Invalidate holds if they require a tap and this touch has been parsed as a tap already
                        if (!held.State && tapped) return;

                        //Invalidate holds out of time range
                        if (!holdTime.CheckStart(Constants.FarWindow)) return;

                        //Increment or kill holds out of time for judging
                        if (holdTime.CheckOutOfRange(currentTime))
                        {
                            JudgeLost();
                            held.value = false;

                            if (!holdTime.Increment(span)) 
                                Disable();
                        }

                        //Invalidate holds not in range; should also rule out all invalid data, i.e. positions with a lane of -1
                        if (touch.track != position.lane) return;

                        //Holds not requiring a tap
                        if(held.value)
                        {
                            //If valid:
                            if (touch.status != TouchPoint.Status.Released)
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
                        else if(touch.status == TouchPoint.Status.Tapped)
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

                        (Entity entity, in ChartTime time, in ChartLane position)

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
                            if (touch.track != position.lane) return;

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

                    ((Entity entity, in ChartTime time, in ChartPosition position, in EntityReference enRef)

                        =>

                    {
                        //Invalidate if already tapped
                        if (tapped) return;

                        //Increment or kill holds out of time for judging
                        if (time.CheckOutOfRange(currentTime))
                        {
                            JudgeLost();
                            entityManager.DestroyEntity(entity);
                            entityManager.DestroyEntity(enRef.value);
                        }

                        //Invalidate if not in range of a tap; should also rule out all invalid data, i.e. positions with a lane of -1

/* Unmerged change from project 'Assembly-CSharp.Player'
Before:
                        if (!touch.inputPlane.CollidesWith(new AABB2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
After:
                        if (!touch.inputPlane.CollidesWith(new ArcCore.Utility.AABB2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
*/
                        if (!touch.inputPlane.CollidesWith((Rect2D)new Rect2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
                            return;

                        //Register tap lul
                        Judge(time.value);
                        tapped = true;

                        //Destroy tap
                        entityManager.DestroyEntity(entity);
                    })

                );
            }
        }

        NativeArray<TouchPoint> touchpoints = InputManager.Instance.touchPoints;

        // Handle all arcs //
        Job.WithBurst().WithCode(

            delegate ()
            {
                for (int c = 0; c < arcJudges.RowCount; c++)
                {
                    //Label to go to start of arc logic again
                    START_LOOP:

                    if (!arcJudges.HasCurrent(c)) continue;
                    ArcJudge cjudge = arcJudges.Current(c);

                    if (cjudge.time <= currentTime) //NO LEAD-IN WIGGLE ROOM
                    {
                        //if an arc judge has expired:
                        lostCount++;
                        combo = 0;

                        //move to next if applicable
                        if (arcJudges.MoveNext(c)) goto START_LOOP;
                    }

                    if (cjudge.time <= currentTime + Constants.FarWindow)
                    {
                        //if judge is in range:
                        Circle2D cCollider = rawArcs[cjudge.rawArcIdx].ColliderAt(currentTime);

                        bool isCorrectTouch = false; //will this touch make arc red
                        int touchIdx = -1; //which touch?
                        for (int t = 0; t < touchpoints.Length; t++)
                        {
                            if (cCollider.CollidesWith(touchpoints[t].inputPlane))
                            {
                                if (touchpoints[t].fingerId == -1 || touchpoints[t].fingerId == arcFingers[c])
                                {
                                    touchIdx = t;
                                    isCorrectTouch = true;
                                    break;
                                }
                                else
                                {
                                    touchIdx = t;
                                }
                            }
                        }

                        if (touchIdx != -1)
                        {
                            //IF TOUCH WAS CORRECT FINGER
                            TouchPoint touch = touchpoints[touchIdx];

                            if (isCorrectTouch && arcStates[c].state != ArcState.Red)
                            {
                                //IS VALID TOUCH
                                if (touch.status == TouchPoint.Status.Released)
                                {
                                    //RELEASED MID-ARC
                                    arcStates[c] = new ArcCompleteState(arcStates[c], ArcState.Red);
                                    arcFingers[c] = -1;

                                    lostCount++;
                                    combo = 0;
                                }
                                else
                                {
                                    //CORRECT JUDGE
                                    maxPureCount++;
                                    combo = 0;

                                    arcJudges.MoveNext(c);
                                }
                            }
                            else
                            {
                                //IS INVALID TOUCH
                                arcStates[c] = new ArcCompleteState(arcStates[c], ArcState.Red);

                                lostCount++;
                                combo = 0;
                            }

                            //skip cleanup, above code handles it (?)
                            continue;
                        }
                    }

                    //check for dead fingers
                    bool arcFingerFound = false;

                    for(int t = 0; t < touchpoints.Length; t++)
                    {
                        if(touchpoints[t].fingerId == arcFingers[c])
                        {
                            arcFingerFound = (touchpoints[t].status != TouchPoint.Status.Released);
                            break;
                        }
                    }

                    if(!arcFingerFound)
                    {
                        arcFingers[c] = -1;
                    }
                }
            }

        ).Run();

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
#endif