using ArcCore.Utility;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public struct TouchPoint
    {
        public float2 inputPlanePoint;
        public int lane;
        public float time;
        public bool pressed;

        public TouchPoint(float2 inputPlanePoint, int lane, float time, bool pressed)
        {
            this.inputPlanePoint = inputPlanePoint;
            this.lane = lane;
            this.time = time;
            this.pressed = pressed;
        }
    }

    public struct TouchPointFull
    {
        public TouchPoint touchPoint;
        public int fingerId;

        public TouchPointFull(TouchPoint touchPoint, int fingerId)
        {
            this.touchPoint = touchPoint;
            this.fingerId = fingerId;
        }
    }

    //ORDERING IS IMPORTANT HERE; POLL_INPUT MUST OCCUR BEFORE ALL JUDGING.
    //UNSURE HOW TO DO THIS
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public const int MaxTouches = 10;

        public Dictionary<int, TouchPoint> touchPoints = new Dictionary<int, TouchPoint>(MaxTouches);

        public Camera cameraCast;
        [HideInInspector]
        public float yLeniency;
        public float BaseYLenDist { get; private set; }

        void Awake()
        {
            Instance = this;

            //ENSURE THAT CAMERA IS RESET BEFORE CALLING THIS
            BaseYLenDist = GetYLeniencyDist();
        }

        void Start()
        {
            CreateConnection();
        }

        public void CreateConnection()
            => Conductor.Instance.OnTimeCalculated += PollInput;

        public void KillConnection()
            => Conductor.Instance.OnTimeCalculated -= PollInput;

        public NativeArray<TouchPointFull> GetJobPreparedTouchPoints()
        {
            NativeArray<TouchPointFull> ret = new NativeArray<TouchPointFull>(touchPoints.Count, Allocator.TempJob);

            int i = 0;

            foreach(var touchPoint in touchPoints)
                ret[i++] = new TouchPointFull(touchPoint.Value, touchPoint.Key);

            return ret;
        }

        private void PollInput(float time)
        {
            CalculateYLeniency(); //SHOULDNT BE RECALCULATED FOR CHARTS WITHOUT CAMERA MOTION

            int count = 0;
            for(int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];

                if (t.phase == TouchPhase.Began && !touchPoints.ContainsKey(t.fingerId)) 
                {

                    (float2 ipt, int lane) = PerformRayCast(t);

                    if (touchPoints.Count <= MaxTouches)
                        touchPoints.Add(
                            t.fingerId, 
                            new TouchPoint(ipt, lane, time - t.deltaTime, true)
                        );

                } 
                else if (t.phase == TouchPhase.Moved && touchPoints.ContainsKey(t.fingerId))
                {

                    TouchPoint tp = touchPoints[t.fingerId];

                    (float2 ipt, int lane) = PerformRayCast(t);

                    tp.inputPlanePoint = ipt;
                    tp.lane = lane;
                    tp.pressed = false;

                    touchPoints[t.fingerId] = tp;

                    count++;

                }
                else if (t.phase == TouchPhase.Stationary && touchPoints.ContainsKey(t.fingerId))
                {

                    TouchPoint tp = touchPoints[t.fingerId];

                    tp.pressed = false;
                    tp.time = time - t.deltaTime;

                    touchPoints[t.fingerId] = tp;

                } 
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {

                    touchPoints.Remove(t.fingerId);

                }
            }
        }

        public float GetYLeniencyDist()
        {
            Vector3 basepoint = cameraCast.WorldToScreenPoint(Vector3.zero);
            Vector3 toppoint = cameraCast.WorldToScreenPoint(Vector3.up * Constants.InputMaxY);
            return Vector3.Distance(basepoint, toppoint);
        }

        public void CalculateYLeniency()
        {
            float dist = GetYLeniencyDist();
            if (Mathf.Approximately(dist, 0))
                yLeniency = float.PositiveInfinity;
            else
                yLeniency = BaseYLenDist / dist;
        }

        public (float2 ipt, int lane) PerformRayCast(Touch t)
        {
            Ray ray = cameraCast.ScreenPointToRay(new Vector3(t.position.x, t.position.y));

            float2 ipt = ProjectionMaths.ProjectOntoXYPlane(ray);

            int lane = 0;
            if (ipt.y < Constants.ArcYZero * yLeniency)
            {
                float2 laneProj = ProjectionMaths.ProjectOntoXZPlane(ray);
                lane = Convert.XToTrack(laneProj.x);
            }

            return (ipt, lane);
        }
    }
}