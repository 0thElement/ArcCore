using ArcCore.Utility;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public struct TouchPoint
    {
        public enum Status
        {
            TAPPED,
            HELD,
            RELEASED
        }

        public AABB2D inputPlane;
        public bool   inputPlaneValid;
        public AABB2D trackPlane;
        public bool   trackPlaneValid;

        public int time;
        public Status status;
        public int fingerId;

        public TouchPoint(AABB2D inputPlane, bool inputPlaneValid, AABB2D trackPlane, bool trackPlaneValid, int time, Status status, int fingerId)
        {
            this.inputPlane = inputPlane;
            this.inputPlaneValid = inputPlaneValid;
            this.trackPlane = trackPlane;
            this.trackPlaneValid = trackPlaneValid;
            this.time = time;
            this.status = status;
            this.fingerId = fingerId;
        }

        [BurstDiscard]
        public void MutatePlanes(AABB2D? inputPlane, AABB2D? trackPlane)
        {
            this.inputPlane = inputPlane.GetValueOrDefault();
            inputPlaneValid = inputPlane != null;
            this.trackPlane = trackPlane.GetValueOrDefault();
            trackPlaneValid = trackPlane != null;
        }

        [BurstDiscard]
        public static TouchPoint FromNullables(AABB2D? inputPlane, AABB2D? trackPlane, int time, Status status, int fingerId)
            => new TouchPoint(
                inputPlane.GetValueOrDefault(), inputPlane != null,
                trackPlane.GetValueOrDefault(), trackPlane != null,
                time, status, fingerId
                );
    }

    //ORDERING IS IMPORTANT HERE; POLL_INPUT MUST OCCUR BEFORE ALL JUDGING.
    //UNSURE HOW TO DO THIS
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public const int MaxTouches = 10;

        public NativeArray<TouchPoint> touchPoints;
        public int safeIndex = 0;

        public Camera cameraCast;

        void Awake()
        {
            Instance = this;
            touchPoints = new NativeArray<TouchPoint>(new TouchPoint[MaxTouches], Allocator.Persistent);
        }

        void Start()
        {
            CreateConnection();
        }

        void OnDestroy()
        {
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

                if (t.phase == TouchPhase.Began && FreeId(t.fingerId) && SafeIndex() != MaxTouches)
                {

                    (AABB2D? ipt, AABB2D? track) = ProjectionMaths.PerformInputRaycast(cameraCast, t);
                    int timeT = (int)math.round((time - t.deltaTime) * 1000);
                    touchPoints[safeIndex] = TouchPoint.FromNullables(ipt, track, timeT, TouchPoint.Status.TAPPED, t.fingerId);

                }
                else if (t.phase == TouchPhase.Moved)
                {
                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        (AABB2D? ipt, AABB2D? track) = ProjectionMaths.PerformInputRaycast(cameraCast, t);
                        int timeT = (int)math.round((time - t.deltaTime) * 1000);

                        tp.MutatePlanes(ipt, track);
                        tp.time = timeT;
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

                        int timeT = (int)math.round((time - t.deltaTime) * 1000);

                        tp.time = timeT;
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