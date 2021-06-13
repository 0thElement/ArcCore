// #define UPD

using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Math;
using ArcCore.Gameplay.Data;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

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
            EnhancedTouchSupport.Enable();

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
            foreach (var t in this)
            {
                // Debug.Log($"{t.fingerId} is at phase {t.status}");
                if (t.InputPlaneValid && t.inputPosition.Value.y > 2f)
                {
                    InputVisualFeedback.Instance.HorizontalLineAt(t.inputPosition.Value.y, t.fingerId);
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

        private bool FreeId(int id) => !touchPoints.Any(t => t.fingerId == id);
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
        private static int GetChartTime(double realTime)
        {
            return Conductor.Instance.receptorTime - (int)System.Math.Round((Time.realtimeSinceStartup - realTime) * 1000);
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

            for (int i=0; i < Touch.activeFingers.Count; i++)
            {
                Finger f = Touch.activeFingers[i];
                Touch t = f.currentTouch;
                int index;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {

                    index = IdIndex(f.index);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.status = TouchPoint.Status.Released;

                        touchPoints[index] = tp;

                        if (tp.track != -1) tracksHeld[tp.track]--;
                    }

                    continue;

                }

                if (SafeIndex() == MaxTouches) continue;

                index = IdIndex(f.index);
                bool isNewTouchPoint = index == -1;

                if (isNewTouchPoint && t.phase == TouchPhase.Began)
                {
                    if (FreeId(f.index))
                    {
                        (float2? exact, Rect2D? ipt, int track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(t.screenPosition));
                        touchPoints[safeIndex] = new TouchPoint(exact, ipt, track, GetChartTime(t.startTime), TouchPoint.Status.Tapped, f.index, t.touchId);

                        if(track != -1)
                        {
                            tracksHeld[track]++;
                        }
                    }

                    continue;
                }
                else if (!isNewTouchPoint)
                {
                    TouchPoint tp = touchPoints[index];
                    int oTrack = tp.track;

                    (tp.inputPosition, tp.inputPlane, tp.track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(t.screenPosition));
                    tp.status = TouchPoint.Status.Sustained;

                    touchPoints[index] = tp;

                    if (oTrack != tp.track)
                    {
                        if (oTrack != -1) tracksHeld[oTrack]--;
                        if (tp.track != -1) tracksHeld[tp.track]++;
                    }

                }
                // TouchPhase in new input system is never stationary for some god awful reason
                // else if (t.phase == TouchPhase.Stationary)
                // {
                //     TouchPoint tp = touchPoints[index];

                //     tp.status = TouchPoint.Status.Sustained;

                //     touchPoints[index] = tp;
                // }
            }
        }
    }
}