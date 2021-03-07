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

        public static float3 ProjectOntoXYPlane(Ray ray)
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
            };

    }
}
