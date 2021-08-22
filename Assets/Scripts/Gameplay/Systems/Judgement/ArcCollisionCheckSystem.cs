using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.EntityCreation;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using ArcCore.Gameplay.Data;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Systems
{
    public enum GroupState
    {
        //Default state
        Initial,
        //Went past judge line but missed
        Missed,
        //Is being held
        Held,
        //Was held before
        Lifted
    }

    [UpdateInGroup(typeof(JudgementSystemGroup))]
    public class ArcCollisionCheckSystem : SystemBase
    {

        //TODO: REMOVE STATICS

        /// <summary>
        /// Keep track of whether a group was held or not this frame.
        /// </summary>
        public static NativeArray<GroupState> arcGroupHeldState;

        /// <summary>
        /// Keep track of whether or not an arc color should be red.
        /// </summary>
        public static List<ArcColorFSM> arcColorFsmArray;

        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            NativeArray<TouchPoint> touchArray = PlayManager.InputHandler.touchPoints;
            int currentTime = PlayManager.ReceptorTime;

            for (int color = 0; color < ArcEntityCreator.ColorCount; color++)
            {
                ArcColorFSM colorState = arcColorFsmArray[color];
                colorState.CheckSchedule();

                bool collided = false;
                bool wrongFinger = false;
                bool touchLifted = true;
                bool existEntities = false;

                for (int i=0; i < touchArray.Length; i++)
                {
                    if (touchArray[i].fingerId == colorState.FingerId)
                    {
                        touchLifted = false;
                        break;
                    }
                }
                if (touchLifted) 
                {
                    colorState.Execute(ArcColorFSM.Event.Lift);
                }
                
                Entities.WithSharedComponentFilter<ArcColorID>(new ArcColorID(color)).WithAll<ChartIncrTime,WithinJudgeRange>().ForEach(
                    (in ArcData arcData, in ArcGroupID groupID) =>
                    {
                        existEntities = true;
                        bool groupHeld = false;

                        for (int i=0; i < touchArray.Length; i++)
                        {
                            TouchPoint currentTouch = touchArray[i];
                            if (!currentTouch.InputPlaneValid) continue;

                            if (arcData.CollideWith(currentTime, currentTouch.InputPlane))
                            {
                                //only assign this touch if it's not already holding another color
                                //i can't think of a better way to do this
                                if (colorState.IsAwaiting())
                                {
                                    bool canAssign = true;
                                    for (int c=0; c < ArcEntityCreator.ColorCount; c++)
                                    {
                                        if (c!=color && arcColorFsmArray[c].FingerId == currentTouch.fingerId)
                                        {
                                            canAssign = false;
                                            break;    
                                        }
                                    }
                                    if (canAssign) 
                                    {
                                        colorState.Execute(ArcColorFSM.Event.Collide, currentTouch.fingerId);
                                    }
                                }

                                collided = true;
                                if (colorState.IsValidId(currentTouch.fingerId))
                                {
                                    groupHeld = true;
                                }
                                else
                                {
                                    wrongFinger = true;
                                }
                            }
                        }

                        if (groupHeld)
                            arcGroupHeldState[groupID.value] = GroupState.Held;
                        else if (arcGroupHeldState[groupID.value] == GroupState.Held)
                            arcGroupHeldState[groupID.value] = GroupState.Lifted;
                        else
                            arcGroupHeldState[groupID.value] = GroupState.Missed;
                    }
                ).WithoutBurst().Run();

                if (!existEntities) colorState.Execute(ArcColorFSM.Event.Rest);
                if (collided)
                    colorState.Execute(ArcColorFSM.Event.Collide);
                if (!collided && wrongFinger)
                    colorState.Execute(ArcColorFSM.Event.WrongFinger);
            }
            touchArray.Dispose();
        }

        protected override void OnDestroy()
        {
            arcGroupHeldState.Dispose();
        }

        public static void SetUpArray(int GroupCount, int ColorCount)
        {
            if (arcGroupHeldState.IsCreated) arcGroupHeldState.Dispose();
            arcGroupHeldState = new NativeArray<GroupState>(GroupCount, Allocator.Persistent);
            arcColorFsmArray = new List<ArcColorFSM>(ColorCount);
            for (int i=0; i<ColorCount; i++)
            {
                ArcCollisionCheckSystem.arcColorFsmArray.Add(new ArcColorFSM(i));
            }
        }
    }

}