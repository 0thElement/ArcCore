using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Behaviours.EntityCreation;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using ArcCore.Gameplay.Data;
using ArcCore.Math;
using UnityEngine;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Systems.Judgement
{

    public class ArcColorTouchData
    {
        /// <summary>
        /// The touchId associated with this arc. Used for judgement.
        /// Id = TouchPoint.NullId means no touch is associated with this color and judgement system should assign a new touch as soon as one collides with an arc
        /// of this color
        /// </summary>
        public int fingerId;

        /// <summary>
        /// Timing at which this arc color stop being red.
        /// </summary>
        public int endRedArcSchedule;

        /// <summary>
        /// Timing after which <see cref="touchId"> is allowed to be reset.
        /// After an arc color is released (the current <see cref="touchId"> no longer exists), the color keep the same <see cref="touchId"> for an extended
        /// period of time before a new one can be assigned.
        /// </summary>
        public int resetFingerIdSchedule;

        public bool isFingerUnassigned => fingerId == TouchPoint.NullId;
        public bool isRedArc(int timing) => timing < endRedArcSchedule;
        public bool shouldResetTouchId(int timing) => timing >= resetFingerIdSchedule;
        public void resetFingerId() => fingerId = TouchPoint.NullId;
    }


    [UpdateInGroup(typeof(JudgementSystemGroup))]

    public class ArcCollisionCheckSystem : SystemBase
    {

        /// <summary>
        /// Keep track of whether a group was held or not this frame.
        /// </summary>
        public static NativeArray<bool> arcGroupHeldStateMap;

        /// <summary>
        /// Keep track of whether or not an arc color should be red.
        /// </summary>
        private static List<ArcColorTouchData> arcColorTouchDataArray;

        private EntityQuery arcColorQuery;

        protected override void OnStartRunning()
        {
            arcGroupHeldStateMap = new NativeArray<bool>(ArcEntityCreator.GroupCount, Allocator.Persistent);
            arcColorTouchDataArray = new List<ArcColorTouchData>(ArcEntityCreator.ColorCount);
            arcColorQuery = GetEntityQuery(typeof(ArcColorID));

            for (int i=0; i < ArcEntityCreator.ColorCount; i++)
            {
                arcColorTouchDataArray.Add(new ArcColorTouchData{fingerId = TouchPoint.NullId, endRedArcSchedule = 0, resetFingerIdSchedule = 0});
            }
        }

        protected override void OnUpdate()
        {
            int currentTiming = Conductor.Instance.receptorTime;

            //Build fingerid-touchpoint hashmap
            NativeHashMap<int, Rect2D> fingerIdToTouchPointMap = new NativeHashMap<int, Rect2D>(10, Allocator.TempJob);
            var touchPoints = InputManager.Instance.GetEnumerator();
            while (touchPoints.MoveNext())
            {
                TouchPoint current = touchPoints.Current;
                if (current.InputPlaneValid)
                    fingerIdToTouchPointMap.TryAdd(current.fingerId, current.inputPlane.Value);
            }
            NativeArray<int> allFingerId = fingerIdToTouchPointMap.GetKeyArray(Allocator.TempJob);

            //Check from color to color
            for (int color=0; color < ArcEntityCreator.ColorCount; color++)
            {
                arcColorQuery.SetSharedComponentFilter(new ArcColorID(color));

                ArcColorTouchData correctTouch = arcColorTouchDataArray[color];

                if (correctTouch.isFingerUnassigned)
                {
                    //Try to assign new finger to unassigned colors
                    int newFingerId = TouchPoint.NullId; 

                    Entities
                        .WithStoreEntityQueryInField(ref arcColorQuery)
                        .WithAll<WithinJudgeRange>()
                        .ForEach(
                            (in ArcData arcData) =>
                            {
                                for (int i=0; i < allFingerId.Length; i++)
                                {
                                    Rect2D fingerTouchPoint = fingerIdToTouchPointMap[allFingerId[i]];
                                    if (arcData.CollideWith(currentTiming, fingerTouchPoint))
                                    {
                                        newFingerId = allFingerId[i];
                                    }
                                }
                            }
                        ).Run();

                    if (newFingerId != TouchPoint.NullId)
                    {
                        correctTouch.fingerId = newFingerId;
                        correctTouch.resetFingerIdSchedule = currentTiming + Constants.ArcResetTouchWindow;
                    }
                    continue;
                }

                bool fingerExist = fingerIdToTouchPointMap.TryGetValue(correctTouch.fingerId, out Rect2D correctTouchRect2D);

                //Check the correct finger first for colors with assigned finger. If fails check for red arc
                Entities
                    .WithStoreEntityQueryInField(ref arcColorQuery)
                    .WithAll<WithinJudgeRange>()
                    .ForEach(
                        (in ArcData arcData, in ArcGroupID groupId) =>
                        {
                            if (fingerExist && arcData.CollideWith(currentTiming, correctTouchRect2D))
                            {
                                //Highlight
                                correctTouch.resetFingerIdSchedule = currentTiming + Constants.ArcResetTouchWindow;
                                arcGroupHeldStateMap[groupId.value] = true;
                            }
                            else
                            {
                                //yes. this is code duplication. no i do not care
                                for (int i=0; i < allFingerId.Length; i++)
                                {
                                    Rect2D fingerTouchPoint = fingerIdToTouchPointMap[allFingerId[i]];
                                    if (arcData.CollideWith(currentTiming, fingerTouchPoint))
                                    {
                                        //Red arc
                                        correctTouch.endRedArcSchedule = currentTiming + Constants.ArcRedArcWindow;
                                        break;
                                    }
                                }
                                //Gray arc
                                arcGroupHeldStateMap[groupId.value] = false;
                            }
                        }
                    ).WithoutBurst().Run();

                //Cooldown until new finger can be assigned
                if (!fingerExist && correctTouch.shouldResetTouchId(currentTiming))
                {
                    Debug.Log($"RESET FINGERID OF COLOR {color}. {currentTiming} -> {correctTouch.resetFingerIdSchedule}");
                    correctTouch.resetFingerId();
                    correctTouch.endRedArcSchedule = currentTiming - 1; //Force end red arc
                }
            }

            fingerIdToTouchPointMap.Dispose();
            allFingerId.Dispose();
        }

        protected override void OnDestroy()
        {
            arcGroupHeldStateMap.Dispose();
            // arcColorTouchDataArray.Dispose();
        }
    }

}