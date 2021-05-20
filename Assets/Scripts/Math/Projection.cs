using ArcCore.Behaviours;
using ArcCore.Utility;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Math
{
    public static class Projection
    {
        public const float TAN_EPSILON = 0.01f; //Equal to tan(0.01 rad) //Go learn trig again floof, tan(x) ~= x
        public const float Y_MAX_FOR_TRACK = 1.9f; //Equal to Convert.GetWorldY(0.2f)

        [BurstCompile(FloatMode = FloatMode.Fast)]
        public static (Rect2D? inputPlane, int track) PerformInputRaycast(Ray cameraRay)
        {
            float3 origin = cameraRay.origin;
            float3 dir = math.normalize(cameraRay.direction);

            //-GET AABB2D FOR INPUT PLANE-//
            Rect2D? inputPlane;

            //-LOCALS-//
            float projPosX;

            //Edge case: tap will never collide with plane
            //Multiplication allows for simultaneous checks for no z difference between camera and origin, and invalid z signs
            if (origin.z * dir.z > 0)
            {
                inputPlane = null;
            }
            else
            {
                //Cast ray onto xy plane at z=0
                float zratio = - origin.z / dir.z;
                /***/ projPosX = origin.x + dir.x * zratio;
                float projPosY = origin.y + dir.y * zratio;

                //FIND X LENIENCY USING 0TH'S MAGIC
                float deltaY = origin.y - projPosY;
                float deltaX = origin.x - projPosX;
                float distToXAxis = math.sqrt(deltaY * deltaY + origin.z * origin.z);
                float distToYAxis = math.sqrt(deltaX * deltaX + origin.z * origin.z);

                float xMax = distToXAxis * (projPosX - origin.x + TAN_EPSILON * distToXAxis) / (
                             distToXAxis - (projPosX - origin.x) * TAN_EPSILON)
                             + origin.x;
                
                float xMin = distToXAxis * (projPosX - origin.x - TAN_EPSILON * distToXAxis) / (
                             distToXAxis + (projPosX - origin.x) * TAN_EPSILON)
                             + origin.x;

                //FIND Y LENIENCY USING 0TH'S MAGIC
                float yMax = distToYAxis * (projPosY - origin.y + TAN_EPSILON * distToYAxis) / (
                             distToYAxis - (projPosY - origin.y) * TAN_EPSILON)
                             + origin.y;
                
                float yMin = distToYAxis * (projPosY - origin.y - TAN_EPSILON * distToYAxis) / (
                             distToYAxis + (projPosY - origin.y) * TAN_EPSILON)
                             + origin.y;

                //Input plane
                inputPlane = new Rect2D(xMin, yMin, xMax, yMax);
            }

            //-GET TRACK RANGE-//
            int track = -1;

            //Check if the tap is too high on the input plane for a track tap
            if (
                (!inputPlane.HasValue || inputPlane.Value.min.y < Y_MAX_FOR_TRACK)
                && origin.y * cameraRay.direction.y < 0
                )
            {
                //Cast ray onto xz plane at y=0
                float yratio = - origin.y / dir.y;
                /***/ projPosX = origin.x + dir.x * yratio;
                float projPosZ = origin.z + dir.z * yratio;

                //Check if cast falls out of acceptable range
                if (-Constants.RenderFloorPositionRange <= projPosZ && projPosZ <= Constants.RenderFloorPositionRange)
                {
                    track = Conversion.XToTrack(projPosX);
                }

                //Reset to "no value" if track is invalid
                if(track > 4 || track < 1)
                {
                    track = -1;
                }
            }

            //RETURN
            return (inputPlane, track);
        }
    }
}
