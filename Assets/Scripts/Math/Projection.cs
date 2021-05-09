using ArcCore.Utility;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Math
{
    public static class Projection
    {
        public const float TAN_EPSILON = 0.10033f; //Equal to tan(0.1 rad)
        public const float Y_MAX_FOR_TRACK = 1.9f; //Equal to Convert.GetWorldY(0.2f)

        [BurstCompile(FloatMode = FloatMode.Fast)]
        public static (Rect2D? inputPlane, int track) PerformInputRaycast(Ray cameraRay, Touch touch)
        {
            float3 camPos = cameraRay.origin;
            float3 camToOrigin = -camPos;

            //-GET AABB2D FOR INPUT PLANE-//
            Rect2D? inputPlane;

            //Edge case: tap will never collide with plane
            //Multiplication allows for simultaneous checks for no z difference between camera and origin, and invalid z signs
            if (camToOrigin.z * cameraRay.direction.z <= 0)
            {
                inputPlane = null;
            }
            else
            {
                //Cast ray onto xy plane at z=0
                float zratio = cameraRay.origin.z / cameraRay.direction.z;
                float projPosX = cameraRay.origin.x - cameraRay.direction.x * zratio,
                      projPosY = cameraRay.origin.y - cameraRay.direction.y * zratio;

                //FIND X LENIENCY USING 0TH'S MAGIC
                float deltaY = camPos.y - projPosY;
                float distXProj = math.sqrt(deltaY * deltaY + camPos.z * camPos.z);

                float xMax = distXProj * (projPosX - camPos.x + TAN_EPSILON * distXProj) / (
                             distXProj - (projPosX - camPos.x) * TAN_EPSILON);
                
                float xMin = distXProj * (projPosX - camPos.x - TAN_EPSILON * distXProj) / (
                             distXProj + (projPosX - camPos.x) * TAN_EPSILON);

                //FIND Y LENIENCY USING 0TH'S MAGIC
                float deltaX = camPos.x - projPosX;
                float distYProj = math.sqrt(deltaX * deltaX + camPos.z * camPos.z);

                float yMax = distYProj * (projPosY - camPos.y + TAN_EPSILON * distYProj) / (
                             distYProj - (projPosY - camPos.y) * TAN_EPSILON);
                
                float yMin = distYProj * (projPosY - camPos.y - TAN_EPSILON * distYProj) / (
                             distYProj + (projPosY - camPos.y) * TAN_EPSILON);

                //Input plane
                inputPlane = new Rect2D(xMin, yMin, xMax, yMax);
            }

            //-GET TRACK RANGE-//
            int track = -1;

            //Check if the tap is too high on the input plane for a track tap
            if (
                (inputPlane?.min.y < Y_MAX_FOR_TRACK || inputPlane.HasValue)
                && camToOrigin.y * cameraRay.direction.y > 0
                )
            {
                //Cast ray onto xz plane at y=0
                float yratio = cameraRay.origin.y / cameraRay.direction.y;
                float projX = cameraRay.origin.x - cameraRay.direction.x * yratio;
                float projZ = cameraRay.origin.z - cameraRay.direction.z * yratio;

                //Check if cast falls out of acceptable range
                if (-Constants.RenderFloorPositionRange <= projZ && projZ <= Constants.RenderFloorPositionRange)
                {
                    track = Conversion.XToTrack(projX);
                }
            }

            //RETURN
            return (inputPlane: inputPlane, track: track);
        }
    }
}
