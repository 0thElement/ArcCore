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

        [HideInInspector]
        public NativeArray<TouchPoint> touchPoints;
        [HideInInspector]
        public int safeIndex = 0;

        [HideInInspector]
        public QuadArr<int> tracksHeld;

        public Camera cameraCast;

        public struct Enumerator : IEnumerator<TouchPoint>
        {
            private readonly NativeArray<TouchPoint> touchPoints;
            private int index;

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
                while(Current.fingerId == -1)
                {
                    index++;
                    if (index > InputManager.MaxTouches) return false;
                }
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
                if (touchPoints[i].fingerId == -1)
                    return safeIndex = i;
            return safeIndex = MaxTouches;
        }

        public void PollInput()
        {
            for (int ti = 0; ti < touchPoints.Length; ti++)
            {
                if(touchPoints[ti].status == TouchPoint.Status.Released)
                {
                    TouchPoint touchPoint = touchPoints[ti];

                    touchPoint.fingerId = -1;

                    touchPoints[ti] = touchPoint;

                    if (safeIndex < ti) safeIndex = ti;
                }
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];

                if (t.phase == TouchPhase.Began)
                {
                    if (FreeId(t.fingerId) && SafeIndex() != MaxTouches)
                    {
                        (Rect2D? ipt, int track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(t.position), t);
                        touchPoints[safeIndex] = new TouchPoint(ipt, track, TouchPoint.Status.Tapped, t.fingerId);

                        if(track != -1)
                        {
                            tracksHeld[track]++;
                        }
                    }
                }
                else if (t.phase == TouchPhase.Moved)
                {
                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];
                        int oTrack = tp.track;

                        (tp.inputPlane, tp.track) = Projection.PerformInputRaycast(cameraCast.ScreenPointToRay(t.position), t);
                        tp.status = TouchPoint.Status.Sustained;

                        touchPoints[index] = tp;

                        if(oTrack != tp.track)
                        {
                            if (oTrack != -1) tracksHeld[oTrack]--;
                            if (tp.track != -1) tracksHeld[tp.track]++;
                        }
                    }

                }
                else if (t.phase == TouchPhase.Stationary)
                {

                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.status = TouchPoint.Status.Sustained;

                        touchPoints[index] = tp;
                    }

                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {

                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.status = TouchPoint.Status.Released;

                        touchPoints[index] = tp;

                        if (tp.track != -1) tracksHeld[tp.track]--;
                    }

                }
            }
        }
    }
}