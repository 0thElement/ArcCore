using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Math;
using ArcCore.Structs;

namespace ArcCore.Behaviours
{
    //ORDERING IS IMPORTANT HERE; POLL_INPUT MUST OCCUR BEFORE ALL JUDGING.
    //UNSURE HOW TO DO THIS
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        public static TouchPoint Get(int index) => Instance.touchPoints[index];

        public const int MaxTouches = 10;

        public NativeArray<TouchPoint> touchPoints;
        public int safeIndex = 0;

        public Camera cameraCast;

        void Awake()
        {
            Instance = this;
            touchPoints = new NativeArray<TouchPoint>(MaxTouches, Allocator.Persistent);
        }

        void Start()
        {
            CreateConnection();
        }

        void OnDestroy()
        {
            KillConnection();
            touchPoints.Dispose();
        }

        public void CreateConnection()
            => Conductor.Instance.OnTimeCalculated += PollInput;

        public void KillConnection()
            => Conductor.Instance.OnTimeCalculated -= PollInput;

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

        private void PollInput(float time)
        {
            for (int ti = 0; ti < touchPoints.Length; ti++)
            {
                if(touchPoints[ti].status == TouchPoint.Status.RELEASED)
                {
                    TouchPoint touchPoint = touchPoints[ti];

                    touchPoint.fingerId = -1;

                    touchPoints[ti] = touchPoint;
                }
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];

                int pTime = (int)math.round((time - t.deltaTime) * 1000);

                if (t.phase == TouchPhase.Began && FreeId(t.fingerId) && SafeIndex() != MaxTouches)
                {

                    (Rect2D ipt, int track) = Projection.PerformInputRaycast(cameraCast, t);
                    touchPoints[safeIndex] = new TouchPoint(ipt, track, pTime, TouchPoint.Status.TAPPED, t.fingerId);

                }
                else if (t.phase == TouchPhase.Moved)
                {
                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        (Rect2D ipt, int track) = Projection.PerformInputRaycast(cameraCast, t);

                        tp.inputPlane = ipt;
                        tp.track = track;
                        tp.time = pTime;
                        tp.status = TouchPoint.Status.HELD;

                        touchPoints[index] = tp;
                    }

                }
                else if (t.phase == TouchPhase.Stationary)
                {

                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.time = pTime;
                        tp.status = TouchPoint.Status.HELD;

                        touchPoints[index] = tp;
                    }

                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {

                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.status = TouchPoint.Status.RELEASED;

                        touchPoints[index] = tp;
                    }

                }
            }
        }
    }
}