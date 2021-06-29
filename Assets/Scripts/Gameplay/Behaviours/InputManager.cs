// #define UPD

using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Math;
using ArcCore.Gameplay.Data;
using System.Collections.Generic;
using System.Collections;
using Lean.Touch;

namespace ArcCore.Gameplay.Behaviours
{
    public class InputManager : MonoBehaviour, IEnumerable<TouchPoint>
    {
        public static InputManager Instance { get; private set; }
        public static TouchPoint Get(int index) => Instance.touchPoints[index];

        public IEnumerator<TouchPoint> GetEnumerator()
        {
            int idx = 0;

            while (true) //yes i know. im awful.
            {
                do
                {
                    idx++;
                    if (idx >= MaxTouches) yield break;
                }
                while (touchPoints[idx].fingerId == FreeTouch);

                yield return touchPoints[idx];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public const int MaxTouches = 10;
        public const int FreeTouch = -69;
        public const float TouchEps = 0.01f;

        [HideInInspector]
        public NativeArray<TouchPoint> touchPoints;
        [HideInInspector]
        public int safeIndex = 0;

        [HideInInspector]
        public NTrackArray<int> tracksHeld;
        public NTrackArray<bool> tracksTapped;

        public Camera cameraCast;

        void Awake()
        {
            Instance = this;
            touchPoints = new NativeArray<TouchPoint>(MaxTouches, Allocator.Persistent);
            tracksHeld = default;
            //EnhancedTouchSupport.Enable();

            for(int i = 0; i < MaxTouches; i++)
            {
                var t = touchPoints[i];
                t.fingerId = FreeTouch;
                touchPoints[i] = t;
            }
        }

        void Update()
        {
            InputVisualFeedback.Instance.DisableLines();
            for (int i = 0; i < MaxTouches; i++)
            {
                var t = touchPoints[i];
                if (t.fingerId == FreeTouch)
                    continue;

                // Debug.Log($"{t.fingerId} is at phase {t.status}");
                if (t.InputPlaneValid && t.inputPosition.Value.y > 2f)
                {
                    InputVisualFeedback.Instance.HorizontalLineAt(t.inputPosition.Value.y, i);
                }
                if (t.TrackValid)
                {
                    InputVisualFeedback.Instance.HighlightLane(t.track - 1);
                }
                
#if UPD
                if(t.InputPlaneValid)
                {
                    Utility.utils.DebugDrawIptRect(t.InputPlane);
                }

                if(t.TrackValid)
                {
                    Debug.DrawRay(new Vector3(Utility.Conversion.TrackToX(t.track), 0.01f, 0), Vector3.back * 150, Color.red);
                }

                Debug.Log(t.InputPlane.min);
                Debug.Log(t.track);
#endif
            }
        }

        void OnDestroy()
        {
            touchPoints.Dispose();
        }

        private int IdIndex(int id)
        {
            for (int i = 0; i < MaxTouches; i++)
                if (touchPoints[i].fingerId == id)
                    return i;
            return -1;
        }
        private int SafeIndex()
        {
            for (int i = safeIndex; i < MaxTouches; i++)
                if (touchPoints[i].fingerId == FreeTouch)
                    return safeIndex = i;
            return safeIndex = MaxTouches;
        }
        private static int GetChartTime(float age)
        {
            return Conductor.Instance.receptorTime - (int)System.Math.Round(age * 1000.0);
        }

        public void PollInput()
        {
            for (int ti = 0; ti < touchPoints.Length; ti++)
            {
                if(touchPoints[ti].status == TouchPoint.Status.Released)
                {
                    TouchPoint touchPoint = touchPoints[ti];

                    touchPoint.fingerId = FreeTouch;

                    touchPoints[ti] = touchPoint;

                    if (ti < safeIndex) safeIndex = ti;
                }
            }

            var touches = LeanTouch.GetFingers(false, false);
            for (int i=0; i < touches.Count; i++)
            {
                LeanFinger f = touches[i];
                int index;

                //released
                if (f.Up)
                {
                    index = IdIndex(f.Index);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.status = TouchPoint.Status.Released;

                        touchPoints[index] = tp;

                        if (tp.track != -1) tracksHeld[tp.track]--;
                    }

                    continue;
                }

                //no more space
                if (SafeIndex() == MaxTouches) continue;
                index = IdIndex(f.Index);

                //tapped
                if (f.Down)
                {
                    //hardware index does not exist
                    if (index == -1)
                    {
                        (float2? exact, Rect2D? ipt, int track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(f.ScreenPosition));
                        touchPoints[safeIndex] = new TouchPoint(exact, ipt, track, GetChartTime(f.Age), TouchPoint.Status.Tapped, f.Index);

                        if(track != -1)
                        {
                            tracksHeld[track]++;
                        }
                    }

                    continue;
                }

                //has not moved significantly
                if (math.lengthsq(f.ScaledDelta) < TouchEps)
                {
                    TouchPoint tp = touchPoints[index];

                    tp.status = TouchPoint.Status.Sustained;

                    touchPoints[index] = tp;
                }
                //has moved
                else
                {
                    TouchPoint tp = touchPoints[index];
                    int oTrack = tp.track;

                    (tp.inputPosition, tp.inputPlane, tp.track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(f.ScreenPosition));
                    tp.status = TouchPoint.Status.Sustained;

                    touchPoints[index] = tp;

                    if (oTrack != tp.track)
                    {
                        if (oTrack != -1) tracksHeld[oTrack]--;
                        if (tp.track != -1) tracksHeld[tp.track]++;
                    }

                }
            }
        }
    }
}