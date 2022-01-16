using ArcCore.Mathematics;
using UnityEngine;

namespace ArcCore.Utilities.Extensions
{
    public static class TransformExtensions
    {
        public static void SetPositionAndRotation(this Transform transform, PosRot posRot)
        {
            transform.position = posRot.position;
            transform.rotation = Quaternion.Euler(posRot.rotation);
        }

        public static void SetLocalPositionAndRotation(this Transform transform, PosRot posRot)
        {
            transform.localPosition = posRot.position;
            transform.localRotation = Quaternion.Euler(posRot.rotation);
        }
    }
}
