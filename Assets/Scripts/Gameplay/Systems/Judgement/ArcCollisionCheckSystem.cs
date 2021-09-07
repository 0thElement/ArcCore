#define DEBUG
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;
using ArcCore.Gameplay.Data;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(UnlockedHoldJudgeSystem))]
    public class ArcCollisionCheckSystem : SystemBase
    {
#if DEBUG
        string[] statesString = new string[] {"Await", "AwaitLift", "Listening", "Lifted", "LiftedRed", "Red", "Grace"};
#endif
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            //Cheating ECS and forcing this system to update every frame
            Entities
                .WithAll<Prefab, Disabled>()
                .ForEach((in ArcGroupID _)=>{return;}).Run();

            var touchArray = PlayManager.InputHandler.touchPoints;
            int currentTime = PlayManager.ReceptorTime;
            var arcGroupHeldState = PlayManager.ArcGroupHeldState;
            float accumulativeArcX = 0;

#if DEBUG
            string s = "";

            for (int i=0; i < touchArray.Length; i++)
            {
                if (touchArray[i].InputPlaneValid)
                    s+= $"touch: {touchArray[i].fingerId} {touchArray[i].inputPosition.Value}\n";
            }
#endif

            for (int color = 0; color <= PlayManager.MaxArcColor; color++)
            {
                ArcColorFSM colorState = PlayManager.ArcColorFsm[color];
                colorState.CheckSchedule();

                float colorCumulativeX = 0;

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
                    colorState.Execute(ArcColorFSM.Event.Lift);
                
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
                                    for (int c=0; c <= PlayManager.MaxArcColor; c++)
                                    {
                                        if (c!=color && PlayManager.ArcColorFsm[c].FingerId == currentTouch.fingerId)
                                        {
                                            canAssign = false;
                                            break;    
                                        }
                                    }
                                    if (canAssign) 
                                        colorState.Execute(ArcColorFSM.Event.Collide, currentTouch.fingerId);
                                }

                                collided = true;
                                if (colorState.IsValidId(currentTouch.fingerId))
                                    groupHeld = true;
                                else
                                    wrongFinger = true;
                            }
                        }

                        if (groupHeld)
                        {
                            arcGroupHeldState[groupID.value] = GroupState.Held;
                            colorCumulativeX += arcData.GetPosAt(currentTime).x;
                        }
                        else if (arcGroupHeldState[groupID.value] == GroupState.Held)
                            arcGroupHeldState[groupID.value] = GroupState.Lifted;
                        else
                            arcGroupHeldState[groupID.value] = GroupState.Missed;
                    }

                ).WithoutBurst().Run();

                if (existEntities)
                    colorState.Execute(ArcColorFSM.Event.Unrest);
                else
                    colorState.Execute(ArcColorFSM.Event.Rest);

                if (collided)
                    colorState.Execute(ArcColorFSM.Event.Collide);
                if (!collided && wrongFinger)
                    colorState.Execute(ArcColorFSM.Event.WrongFinger);

                accumulativeArcX += colorCumulativeX;

#if DEBUG
                s += $"Color: {color} -> finger: {colorState.FingerId} & state: {statesString[(int)colorState._state]} \n";
#endif
            }
#if DEBUG
            PlayManager.DebugText.text = s;
#endif
            PlayManager.GameplayCamera.AccumulativeArcX = accumulativeArcX;
        }
    }

}