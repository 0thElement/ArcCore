#define UPD
#define TestOnComputer

using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Math;
using ArcCore.Structs;
using System.Collections.Generic;
using System.Collections;

namespace ArcCore.Behaviours
{
    //ORDERING IS IMPORTANT HERE; POLL_INPUT MUST OCCUR BEFORE ALL JUDGING.
    //UNSURE HOW TO DO THIS
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        public static TouchPoint Get(int index) => Instance.touchPoints[index];
        public static Enumerator GetEnumerator() => new Enumerator(Instance);

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

        public struct Enumerator : IEnumerator<TouchPoint>, IEnumerable<TouchPoint>
        {
            private readonly NativeArray<TouchPoint> touchPoints;
            private int index;

            public IEnumerator<TouchPoint> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;

            public Enumerator(InputManager inputManager)
            {
                touchPoints = inputManager.touchPoints;
                index = -1;
            }

            public TouchPoint Current
            {
                get => touchPoints[index];
            }

            object IEnumerator.Current
            {
                get => touchPoints[index];
            }

            public void Dispose()
            {}

            public bool MoveNext()
            {
                do
                {
                    index++;
                    if (index >= MaxTouches) return false;
                } 
                while (Current.fingerId == FreeTouch);

                return true;
            }

            public void Reset()
            {
                index = -1;
            }
        }

        void Awake()
        {
            Instance = this;
            touchPoints = new NativeArray<TouchPoint>(MaxTouches, Allocator.Persistent);
            tracksHeld = default;

            for(int i = 0; i < MaxTouches; i++)
            {
                var t = touchPoints[i];
                t.fingerId = FreeTouch;
                touchPoints[i] = t;
            }
        }

#if UPD
        void Update()
        {
            foreach(var t in GetEnumerator())
            {
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
            }
        }
#endif

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

        public void PollMouseInput()
        {
            if(Input.GetMouseButtonDown(0))
            {
                (Rect2D? ipt, int track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(Input.mousePosition));
                touchPoints[0] = new TouchPoint(ipt, track, TouchPoint.Status.Tapped, 1);

                if (track != -1)
                {
                    tracksHeld[track]++;
                    tracksTapped[track] = true;
                }
            } 
            else if(Input.GetMouseButtonUp(0))
            {
                TouchPoint tp = touchPoints[0];

                tp.status = TouchPoint.Status.Released;

                touchPoints[0] = tp;

                if (tp.track != -1) tracksHeld[tp.track]--;
            }
            else if(Input.GetMouseButton(0))
            {
                TouchPoint tp = touchPoints[0];
                int oTrack = tp.track;

                (tp.inputPlane, tp.track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(Input.mousePosition));
                tp.status = TouchPoint.Status.Sustained;

                touchPoints[0] = tp;

                if (oTrack != tp.track)
                {
                    if (oTrack != -1) tracksHeld[oTrack]--;
                    if (tp.track != -1) tracksHeld[tp.track]++;
                }
            }
        }

        public void PollInput()
        {
            //Debug.Log(safeIndex);

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

#if TestOnComputer
            PollMouseInput();
#else

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];
                int index;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {

                    index = IdIndex(t.fingerId);
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

                if (t.phase == TouchPhase.Began)
                {
                    if (FreeId(t.fingerId))
                    {
                        (Rect2D? ipt, int track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(t.position));
                        touchPoints[safeIndex] = new TouchPoint(ipt, track, TouchPoint.Status.Tapped, t.fingerId);

                        if(track != -1)
                        {
                            tracksHeld[track]++;
                        }
                    }

                    continue;
                }

                index = IdIndex(t.fingerId);
                if (index == -1) continue;

                if (t.phase == TouchPhase.Moved)
                {
                    TouchPoint tp = touchPoints[index];
                    int oTrack = tp.track;

                    (tp.inputPlane, tp.track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(t.position));
                    tp.status = TouchPoint.Status.Sustained;

                    touchPoints[index] = tp;

                    if (oTrack != tp.track)
                    {
                        if (oTrack != -1) tracksHeld[oTrack]--;
                        if (tp.track != -1) tracksHeld[tp.track]++;
                    }

                }
                else if (t.phase == TouchPhase.Stationary)
                {
                    TouchPoint tp = touchPoints[index];

                    tp.status = TouchPoint.Status.Sustained;

                    touchPoints[index] = tp;
                }
            }
#endif
        }
    }
}