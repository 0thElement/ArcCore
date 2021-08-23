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
    [UpdateInGroup(typeof(JudgementSystemGroup))]
    public class ArcCollisionCheckSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            var touchArray = PlayManager.InputHandler.touchPoints;
            int currentTime = PlayManager.ReceptorTime;

            for (int color = 0; color <= PlayManager.MaxArcColor; color++)
            {
                ArcColorFSM colorState = PlayManager.ArcColorFsm[color];
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
                
                Entities.WithSharedComponentFilter(new ArcColorID(color)).WithAll<ChartIncrTime,WithinJudgeRange>().ForEach(
                    
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
                                    for (int c=0; c < PlayManager.MaxArcColor; c++)
                                    {
                                        if (c!=color && PlayManager.ArcColorFsm[c].FingerId == currentTouch.fingerId)
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

                        var arcGroupHelpState = PlayManager.ArcGroupHeldState;

                        if (groupHeld)
                        {
                            arcGroupHelpState[groupID.value] = GroupState.Held;
                        }
                        else if (arcGroupHelpState[groupID.value] == GroupState.Held)
                        {
                            arcGroupHelpState[groupID.value] = GroupState.Lifted;
                        }
                        else
                        {
                            arcGroupHelpState[groupID.value] = GroupState.Missed;
                        }
                    }

                ).WithoutBurst().Run();

                if (!existEntities) colorState.Execute(ArcColorFSM.Event.Rest);

                if (collided)
                    colorState.Execute(ArcColorFSM.Event.Collide);
                if (!collided && wrongFinger)
                    colorState.Execute(ArcColorFSM.Event.WrongFinger);
            }
        }
    }

}