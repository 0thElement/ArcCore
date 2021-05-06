using ArcCore.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Math
{
    public static class Projection
    {
        public const float TAN_EPSILON = 0.10033f; //Equal to tan(0.1 rad)
        public const float Y_MAX_FOR_TRACK = 1.9f; //Equal to Convert.GetWorldY(0.2f)

        public static (Rect2D inputPlane, int track) PerformInputRaycast(Camera cam, Touch touch)
        {
            Ray baseRay = cam.ScreenPointToRay(touch.position);

            float3 camPos = baseRay.origin;
            float3 camToOrigin = -camPos;

            //-GET AABB2D FOR INPUT PLANE-//
            Rect2D inputPlane;

            //Edge case: tap will never collide with plane
            //Multiplication allows for simultaneous checks for no z difference between camera and origin, and invalid z signs
            if (camToOrigin.z * baseRay.direction.z <= 0)
            {
                inputPlane = Rect2D.none;
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
                inputPlane = new Rect2D(new float2(xMin, yMin), new float2(xMax, yMax));
            }

            //-GET TRACK RANGE-//
            int track = -1;

            //Check if the tap is too high on the input plane for a track tap
            if ((inputPlane.min.y < Y_MAX_FOR_TRACK || inputPlane.IsNone)
            && camToOrigin.y * baseRay.direction.y > 0)
            {
                //Cast ray onto xz plane at y=0
                float yratio = baseRay.origin.y / baseRay.direction.y;
                float projX = baseRay.origin.x - baseRay.direction.x * yratio;
                float projZ = baseRay.origin.z - baseRay.direction.z * yratio;

                //Check if cast falls out of acceptable range
                if (-Constants.RenderFloorPositionRange <= projZ && projZ <= Constants.RenderFloorPositionRange)
                {
                    track = Conversion.XToTrack(projX);
                }
            }

            //RETURN
            return (inputPlane, track);
        }
    }
}
