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

        public static float2 ProjectOntoXYPlane(Ray ray)
        {
            if (ray.direction.z == 0)
            {
                return new float2(ray.origin.x, 0);
            }
            else
            {
                float ratio = ray.origin.z / ray.direction.z;
                return new float2(ray.origin.x - ray.direction.x * ratio, ray.origin.y - ray.direction.y * ratio);
            }
        }

        public static float2 ProjectOntoXZPlane(Ray ray)
        {
            if (ray.direction.y == 0)
            {
                return new float2(ray.origin.x, 0);
            }
            else
            {
                float ratio = ray.origin.y / ray.direction.y;
                return new float2(ray.origin.x - ray.direction.x * ratio, ray.origin.z - ray.direction.z * ratio);
            }
        }

        public static float2 ProjectOntoYZPlane(Ray ray)
        {
            if (ray.direction.x == 0)
            {
                return new float2(ray.origin.y, 0);
            }
            else
            {
                float ratio = ray.origin.x / ray.direction.x;
                return new float2(ray.origin.y - ray.direction.y * ratio, ray.origin.z - ray.direction.z * ratio);
            }
        }

        public static (float min, float max) GetXExtents(float3 camPos, float2 projPos)
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

    }
}
