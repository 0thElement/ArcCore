using ArcCore.Math;
using UnityEngine;

namespace ArcCore.Utilities
{
    public static class Misc
    {
        public static T[] NewArrayFill<T>(int length, T value = default) where T: struct
        {
            T[] newarr = new T[length];
            for(int i = 0; i < length; i++)
            {
                newarr[i] = value;
            }
            return newarr;
        }

        public static int BoolToInt(bool b)
            => b ? 1 : 0;

        public static bool IntToBool(int i)
            => i != 0;

        public static void DebugDrawIptRect(Rect2D rect)
        {
            Debug.Log("draw");
            Debug.DrawLine(new Vector3(rect.min.x, rect.min.y, 0), new Vector3(rect.min.x, rect.max.y, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.min.x, rect.max.y, 0), new Vector3(rect.max.x, rect.max.y, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.max.x, rect.max.y, 0), new Vector3(rect.max.x, rect.min.y, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.max.x, rect.min.y, 0), new Vector3(rect.min.x, rect.min.y, 0), Color.red);
            Debug.DrawLine(new Vector3(rect.max.x, rect.max.y, 0), new Vector3(rect.min.x, rect.min.y, 0), Color.red);
        }
    }
}