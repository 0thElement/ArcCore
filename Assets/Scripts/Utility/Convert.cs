using Unity.Mathematics;
using UnityEngine;

namespace Arcaoid.Utility
{
    public class Convert
    {
        public static float TrackToX(int track)
        {
            switch(track)
            {
                case 1:
                    return 6.375f;
                case 2:
                    return 2.125f;
                case 3:
                    return -2.125f;
                case 4:
                    return -6.375f;
                default:
                    return 6.375f;
            }
        }
        
        public static float S(float start, float end, float t) 
            => start * (1 - t) + end * t;
        public static float O(float start, float end, float t) 
            => start + (end - start) * (1 - Mathf.Cos(1.5707963f * t));
        public static float I(float start, float end, float t)
            => start + (end - start) * (Mathf.Sin(1.5707963f * t));
        public static float B(float start, float end, float t)
        {
            float o = 1 - t;
            return (o * o * o * start) + (3 * o * o * t * start) + (3 * o * t * t * end) + (t * t * t * end);
        }

        public static float GetXAt(float t, float startX, float endX, ArcEasing easing)
        {
            switch (easing)
            {
                default:
                case ArcEasing.s:
                    return S(startX, endX, t);
                case ArcEasing.b:
                    return B(startX, endX, t);
                case ArcEasing.si:
                case ArcEasing.sisi:
                case ArcEasing.siso:
                    return I(startX, endX, t);
                case ArcEasing.so:
                case ArcEasing.sosi:
                case ArcEasing.soso:
                    return O(startX, endX, t);
            }
        }
        public static float GetYAt(float t, float startY, float endY, ArcEasing easing)
        {
            switch (easing)
            {
                default:
                case ArcEasing.s:
                case ArcEasing.si:
                case ArcEasing.so:
                    return S(startY, endY, t);
                case ArcEasing.b:
                    return B(startY, endY, t);
                case ArcEasing.sisi:
                case ArcEasing.sosi:
                    return I(startY, endY, t);
                case ArcEasing.siso:
                case ArcEasing.soso:
                    return O(startY, endY, t);
            }
        }

        public static float2 GetPosAt(float t, float2 start, float2 end, ArcEasing easing)
            => new float2(GetXAt(t, start.x, end.x, easing), GetYAt(t, start.y, end.y, easing));

        public static float GetWorldX(float x) 
            => -8.5f * x + 4.25f;
        public static float GetWorldY(float y) 
            => 4.5f * y + 1f;
    }
}