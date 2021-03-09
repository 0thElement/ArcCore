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

        public const float TAN_EPSILON = 0.1003f;

        /*public static float3 ProjectOntoXYPlane(Ray ray)
        {
            if (ray.direction.z == 0)
            {
                return new float3(ray.origin.x, 0, 0);
            }
            else
            {
                float ratio = ray.origin.z / ray.direction.z;
                return new float3(ray.origin.x - ray.direction.x * ratio, ray.origin.y - ray.direction.y * ratio, 0);
            }
        }

        public static float3 ProjectOntoXZPlane(Ray ray)
        {
            if (ray.direction.y == 0)
            {
                return new float3(ray.origin.x, 0, 0);
            }
            else
            {
                float ratio = ray.origin.y / ray.direction.y;
                return new float3(ray.origin.x - ray.direction.x * ratio, 0, ray.origin.z - ray.direction.z * ratio);
            }
        }

        public static float3 ProjectOntoYZPlane(Ray ray)
        {
            if (ray.direction.x == 0)
            {
                return new float3(0, ray.origin.y, 0);
            }
            else
            {
                float ratio = ray.origin.x / ray.direction.x;
                return new float3(0, ray.origin.y - ray.direction.y * ratio, ray.origin.z - ray.direction.z * ratio);
            }
        }

        public static (float min, float max) GetXExtentsJudgeLen(float3 camPos, float3 projPos)
        {
            if (camPos.z == 0)
                return (float.NegativeInfinity, float.PositiveInfinity);

            float deltaY = camPos.y - projPos.y;
            //dx is sqrt((camy - projy)^2 + cam z^2)
            float distXProj = math.sqrt(deltaY * deltaY + camPos.z * camPos.z);

            return (
                //MINIMUM
                distXProj * (projPos.x - camPos.x - TAN_EPSILON * distXProj) / (
                distXProj + (projPos.x - camPos.x) * TAN_EPSILON),
                //MAXIMUM
                distXProj * (projPos.x - camPos.x + TAN_EPSILON * distXProj) / (
                distXProj - (projPos.x - camPos.x) * TAN_EPSILON)
            );
        }

        public static (float min, float max) GetYExtentsJudgeLen(float3 camPos, float3 projPos)
        {
            if (camPos.z == 0)
                return (float.NegativeInfinity, float.PositiveInfinity);

            float deltaX = camPos.x - projPos.x;
            //dy is sqrt((camy - projy)^2 + cam z^2)
            float distYProj = math.sqrt(deltaX * deltaX + camPos.z * camPos.z);

            return (
                //MINIMUM
                distYProj * (projPos.y - camPos.y - TAN_EPSILON * distYProj) / (
                distYProj + (projPos.y - camPos.y) * TAN_EPSILON),
                //MAXIMUM
                distYProj * (projPos.y - camPos.y + TAN_EPSILON * distYProj) / (
                distYProj - (projPos.y - camPos.y) * TAN_EPSILON)
            );
        }

        public static float GetXExtentsSizeJudgeLen(float3 camPos, float3 projPos)
        {
            (float min, float max) = GetXExtentsJudgeLen(camPos, projPos);
            return max - min;
        }

        public static float GetYExtentsSizeJudgeLen(float3 camPos, float3 projPos)
        {
            (float min, float max) = GetYExtentsJudgeLen(camPos, projPos);
            return max - min;
        }

        public static AABB GetAABBJudgePoint(float3 camPos, float3 projPos)
            => new AABB()
            {
                Center = projPos,
                Extents = new float3(GetXExtentsSizeJudgeLen(camPos, projPos), GetYExtentsSizeJudgeLen(camPos, projPos), 0)
            };*/

        public static (AABB2D? inputPlane, AABB2D? trackPlane) PerformInputRaycast(Camera cam, Touch touch)
        {
            Ray baseRay = cam.ScreenPointToRay(touch.position);

            float3 camPos = cam.transform.position;

            //GET AABB2D FOR INPUT PLANE
            AABB2D? inputPlane;
            if (baseRay.direction.z == 0)
            {
                inputPlane = null;
            }
            else if (camPos.z == 0)
            {
                inputPlane = new AABB2D(float2.zero, new float2(float.PositiveInfinity, float.PositiveInfinity));
            }
            else
            {
                //CAST RAY ONTO (X, Y) PLANE @ Z=0
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

                inputPlane = AABB2D.FromCorners(new float2(xMax, yMax), new float2(xMin, yMin));
            }

            AABB2D? trackPlane;
            if (baseRay.direction.y == 0)
            {
                trackPlane = null;
            }
            else if (camPos.y == 0)
            {
                trackPlane = new AABB2D(float2.zero, new float2(float.PositiveInfinity, float.PositiveInfinity));
            }
            else
            {
                //CAST RAY ONTO (X, Y) PLANE @ Z=0
                float yratio = baseRay.origin.y / baseRay.direction.y;
                float2 projPosXZ = new float2(baseRay.origin.x - baseRay.direction.x * yratio, baseRay.origin.z - baseRay.direction.z * yratio);

                //FIND X LENIENCY USING 0TH'S MAGIC
                float deltaZ = camPos.z - projPosXZ.y;
                float distXProj = math.sqrt(deltaZ * deltaZ + camPos.z * camPos.z);

                float xMax = distXProj * (projPosXZ.x - camPos.x + TAN_EPSILON * distXProj) / (
                             distXProj - (projPosXZ.x - camPos.x) * TAN_EPSILON);

                float xMin = distXProj * (projPosXZ.x - camPos.x - TAN_EPSILON * distXProj) / (
                             distXProj + (projPosXZ.x - camPos.x) * TAN_EPSILON);

                //FIND Y LENIENCY USING 0TH'S MAGIC
                float deltaX = camPos.x - projPosXZ.x;
                float distYProj = math.sqrt(deltaX * deltaX + camPos.z * camPos.z);

                float zMax = distYProj * (projPosXZ.y - camPos.z + TAN_EPSILON * distYProj) / (
                             distYProj - (projPosXZ.y - camPos.z) * TAN_EPSILON);

                float zMin = distYProj * (projPosXZ.y - camPos.z - TAN_EPSILON * distYProj) / (
                             distYProj + (projPosXZ.y - camPos.z) * TAN_EPSILON);

                trackPlane = math.min(zMin, zMax) < -2f ? (AABB2D?)null : AABB2D.FromCorners(new float2(xMax, zMax), new float2(xMin, zMin));
            }

            //RETURN
            return (inputPlane, trackPlane);
        }
    }
}
