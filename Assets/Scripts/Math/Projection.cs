using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Utility;
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
        public static (float2? exactInput, Rect2D? inputPlane, int track) PerformInputRaycast(Ray cameraRay)
        {
            float3 origin = cameraRay.origin;
            float3 dir = math.normalize(cameraRay.direction);

            //-GET AABB2D FOR INPUT PLANE-//
            Rect2D? inputPlane;
            float2? exactInput;

            //Edge case: tap will never collide with plane
            //Multiplication allows for simultaneous checks for no z difference between camera and origin, and invalid z signs
            if (origin.z * dir.z > 0)
            {
                inputPlane = null;
                exactInput = null;
            }
            else
            {
                //Cast ray onto xy plane at z=0
                float zratio = - origin.z / dir.z;
                float projPosX = origin.x + (dir.x * zratio);
                float projPosY = origin.y + (dir.y * zratio);

                exactInput = new float2(projPosX, projPosY);

                //FIND X LENIENCY USING 0TH'S MAGIC
                float deltaY = origin.y - projPosY;
                float deltaX = origin.x - projPosX;

                float distToXAxis = math.sqrt((deltaY * deltaY) + (origin.z * origin.z));
                float distToYAxis = math.sqrt((deltaX * deltaX) + (origin.z * origin.z));

                float nDeltaX = -deltaX;
                float nDeltaY = -deltaY;

                float xMax = distToXAxis * (nDeltaX + (TAN_EPSILON * distToXAxis)) / (
                             distToXAxis - (nDeltaX * TAN_EPSILON))
                             + origin.x;
                
                float xMin = distToXAxis * (nDeltaX - (TAN_EPSILON * distToXAxis)) / (
                             distToXAxis + (nDeltaX * TAN_EPSILON))
                             + origin.x;

                //FIND Y LENIENCY USING 0TH'S MAGIC
                float yMax = distToYAxis * (nDeltaY + (TAN_EPSILON * distToYAxis)) / (
                             distToYAxis - (nDeltaY * TAN_EPSILON))
                             + origin.y;
                
                float yMin = distToYAxis * (nDeltaY - (TAN_EPSILON * distToYAxis)) / (
                             distToYAxis + nDeltaY * TAN_EPSILON)
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
                float lProjPosX = origin.x + (dir.x * yratio);
                float lProjPosZ = origin.z + (dir.z * yratio);

                //Check if cast falls out of acceptable range
                if (-Constants.RenderFloorPositionRange <= lProjPosZ && lProjPosZ <= Constants.RenderFloorPositionRange)
                {
                    track = Conversion.XToTrack(lProjPosX);
                }

                //Reset to "no value" if track is invalid
                if(track > 4 || track < 1)
                {
                    track = -1;
                }
            }

            //RETURN
            return (exactInput, inputPlane, track);
        }
    }
}
