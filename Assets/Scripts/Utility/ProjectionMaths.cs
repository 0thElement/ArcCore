using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Utility
{
    public static class ProjectionMaths
    {
        public const float TAN_EPSILON = 0.10033f; //Equal to tan(0.1 rad)
        public const float Y_MAX_FOR_TRACK = 1.9f; //Equal to Convert.GetWorldY(0.2f)

        public static float DotXY(this float3 a, float3 b)
            => a.x * b.x + a.y * b.y;
        public static float DotXZ(this float3 a, float3 b)
           => a.x * b.x + a.z * b.z;
        public static float DotYZ(this float3 a, float3 b)
           => a.y * b.y + a.z * b.z;

        public static (AABB2D inputPlane, TrackRange trackRange) PerformInputRaycast(Camera cam, Touch touch)
        {
            Ray baseRay = cam.ScreenPointToRay(touch.position);

            float3 camPos = baseRay.origin;
            float3 camToOrigin = -camPos;

            //-GET AABB2D FOR INPUT PLANE-//
            AABB2D inputPlane;

            //Edge case: tap will never collide with plane
            //Multiplication allows for simultaneous checks for no z difference between camera and origin, and invalid z signs
            if (camToOrigin.z * baseRay.direction.z <= 0)
            {
                inputPlane = AABB2D.none;
            }
            else
            {
                //Cast ray onto xy plane at z=0
                float zratio = baseRay.origin.z / baseRay.direction.z;
                float2 projPosXY = new float2(baseRay.origin.x - baseRay.direction.x * zratio, baseRay.origin.y - baseRay.direction.y * zratio);

                //FIND X LENIENCY USING 0TH'S MAGIC
                float deltaY = camPos.y - projPosXY.y;
                float distXProj = math.sqrt(deltaY * deltaY + camPos.z * camPos.z);

                float xMax = distXProj * (projPosXY.x - camPos.x + TAN_EPSILON * distXProj) / (
                             distXProj - (projPosXY.x - camPos.x) * TAN_EPSILON);
                
                float xMin = distXProj * (projPosXY.x - camPos.x - TAN_EPSILON * distXProj) / (
                             distXProj + (projPosXY.x - camPos.x) * TAN_EPSILON);

                //FIND Y LENIENCY USING 0TH'S MAGIC
                float deltaX = camPos.x - projPosXY.x;
                float distYProj = math.sqrt(deltaX * deltaX + camPos.z * camPos.z);

                float yMax = distYProj * (projPosXY.y - camPos.y + TAN_EPSILON * distYProj) / (
                             distYProj - (projPosXY.y - camPos.y) * TAN_EPSILON);
                
                float yMin = distYProj * (projPosXY.y - camPos.y - TAN_EPSILON * distYProj) / (
                             distYProj + (projPosXY.y - camPos.y) * TAN_EPSILON);

                //Input plane
                inputPlane = new AABB2D(new float2(xMax, yMax), new float2(xMin, yMin));
            }

            //-GET TRACK RANGE-//
            TrackRange trackRange;
            
            //Safe due to NaN magic! (See IEEE754 nan requirements for comparison)
            //Check if the tap is too high on the input plane for a track tap
            if (inputPlane.min.y >= Y_MAX_FOR_TRACK)
            {
                trackRange = TrackRange.none;
            }
            //Edge case: tap will never collide with plane
            //Dot products allow for checks if the tap will ever touch the plane
            //Before dot products though, we must check if the y-direction matches that of the y-position
            else if (camToOrigin.y * baseRay.direction.y <= 0)
            {
                trackRange = TrackRange.none;
            }
            else
            {
                //Cast ray onto xz plane at y=0
                float yratio = baseRay.origin.y / baseRay.direction.y;
                float2 projPosXZ = new float2(baseRay.origin.x - baseRay.direction.x * yratio, baseRay.origin.z - baseRay.direction.z * yratio);

                //Check if cast falls out of acceptable range
                if (-Constants.RenderFloorPositionRange >= projPosXZ.y && projPosXZ.y >= Constants.RenderFloorPositionRange)
                {
                    //FIND X LENIENCY USING 0TH'S MAGIC
                    float deltaZ = camPos.z - projPosXZ.y;
                    float distXProj = math.sqrt(deltaZ * deltaZ + camPos.z * camPos.z);

                    float xMax = distXProj * (projPosXZ.x - camPos.x + TAN_EPSILON * distXProj) / (
                             distXProj - (projPosXZ.x - camPos.x) * TAN_EPSILON);

                    float xMin = distXProj * (projPosXZ.x - camPos.x - TAN_EPSILON * distXProj) / (
                             distXProj + (projPosXZ.x - camPos.x) * TAN_EPSILON);

                    //Convert min and max to tracks
                    int minTrack = Convert.XToTrack(xMin);
                    int maxTrack = Convert.XToTrack(xMax);

                    //Get track range
                    trackRange = new TrackRange(minTrack, maxTrack);
                }
                //If so, set the range to `none`
                else
                {
                    trackRange = TrackRange.none;
                }
            }

            //RETURN
            return (inputPlane, trackRange);
        }
    }
}
