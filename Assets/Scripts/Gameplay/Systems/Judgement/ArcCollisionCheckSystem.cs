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

    public struct ArcColorTouchData
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
        public static NativeArray<int> arcGroupHeldState;

        /// <summary>
        /// Keep track of whether or not an arc color should be red.
        /// </summary>
        public static NativeArray<ArcColorTouchData> arcColorTouchDataArray;

        private EntityQuery arcColorQuery;

        protected override void OnCreate()
        {
            arcColorQuery = GetEntityQuery(typeof(ArcColorID));
        }

        protected override void OnUpdate()
        {
            for (int i=0; i < arcGroupHeldState.Length; i++)
            {
                if ((Conductor.Instance.receptorTime / 1000) % 2 == 0) 
                {
                    arcGroupHeldState[i] = 1;
                }
                else
                {
                    arcGroupHeldState[i] = -1;
                }
            }
        }

        protected override void OnDestroy()
        {
            arcGroupHeldState.Dispose();
            arcColorTouchDataArray.Dispose();
        }
    }

}