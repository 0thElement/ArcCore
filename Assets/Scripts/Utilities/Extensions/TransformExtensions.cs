using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Utilities.Extensions
{
    public static class TransformExtensions
    {
        public static void SetPositionAndRotation(this Transform transform, float3 pos, float3 rot)
        {
            transform.position = pos;
            transform.rotation = Quaternion.Euler(rot);
        }

        public static void SetLocalPositionAndRotation(this Transform transform, float3 pos, float3 rot)
        {
            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(rot);
        }
    }
}
