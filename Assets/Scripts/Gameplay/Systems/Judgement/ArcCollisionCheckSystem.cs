#define DEBUG
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;
using ArcCore.Gameplay.Data;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(UnlockedHoldJudgeSystem))]
    public class ArcCollisionCheckSystem : SystemBase
    {
#if DEBUG
        string[] statesString = new string[] {"Await", "AwaitLift", "Listening", "Lifted", "LiftedRed", "Red", "Grace"};
        string[] debugColor = new string[] {"#92FFFA", "#FF7878", "#76FF6E"};
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
                s += touchArray[i].fingerId + "\n";
            }
#endif
            for (int color = 0; color <= PlayManager.MaxArcColor; color++)
            {
                ArcColorState colorState = PlayManager.ArcColorState[color];

                float colorCumulativeX = 0;

                bool touchLifted = true;
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
                    colorState.Lift(currentTime);
                }
                
                bool existEntities = false;
                Entities.WithSharedComponentFilter(new ArcColorID(color))
                        .WithAll<ChartIncrTime, WithinJudgeRange>()
                        .WithNone<Autoplay>().ForEach(
                    
                    (in ArcData arcData, in ArcGroupID groupID) =>
                    {
                        existEntities = true;
                        bool groupHeld = false;

                        if (colorState.CanAssignNewInput(currentTime))
                        {
                            //Find nearest input and try to assign if input actually collides
                            //(and if input is not alerady assigned to another color)
                            float minDistSqr = float.MaxValue;
                            int nearestFinger = TouchPoint.NullId;

                            for (int i = 0; i < touchArray.Length; i++)
                            {
                                TouchPoint touch = touchArray[i];
                                if (!touch.InputPlaneValid) continue;

                                float distSqr = touch.InputPlane.Center.DistanceSquared(arcData.GetPosAt(currentTime));
                                if (distSqr < minDistSqr && arcData.CollideWith(currentTime, touch.InputPlane))
                                {
                                    //Check if touch is assigned to any other color
                                    bool canAssign = ArcColorState.GetAssignedColorOfFingerId(touch.fingerId) == null;
                                    if (canAssign) 
                                    {
                                        minDistSqr = distSqr;
                                        nearestFinger = touch.fingerId;
                                    }
                                    else
                                    {
                                        colorState.RedArc(currentTime);
                                    }
                                }
                            }

                            if (nearestFinger != TouchPoint.NullId)
                            {
                                colorState.Hit(nearestFinger, currentTime);
                                groupHeld = true;
                            }
                        }
                        else
                        {
                            //Loop through each input and update colorState
                            for (int i = 0; i < touchArray.Length; i++)
                            {
                                TouchPoint touch = touchArray[i];
                                if (arcData.CollideWith(currentTime, touch.InputPlane))
                                {
                                    if (colorState.Hit(touchArray[i].fingerId, currentTime))
                                    {
                                        groupHeld = true;
                                        break;
                                    }
                                }
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
                    colorState.Unrest(currentTime);
                else
                    colorState.Rest(currentTime);

                accumulativeArcX += colorCumulativeX;
#if DEBUG
                s += $"Color: {color} -> finger: {colorState.FingerId}\n";
#endif
         }
#if DEBUG
            PlayManager.DebugText.text = s;
#endif
            PlayManager.GameplayCamera.AccumulativeArcX = accumulativeArcX;
        }
    }

}