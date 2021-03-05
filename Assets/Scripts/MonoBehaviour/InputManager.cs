using ArcCore.Utility;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public struct TouchPoint
    {
        public float2 inputPlanePoint;
        public int lane;
        public int time;
        public bool pressed;
        public int fingerId;

        public TouchPoint(float2 inputPlanePoint, int lane, int time, bool pressed, int fingerId)
        {
            this.inputPlanePoint = inputPlanePoint;
            this.lane = lane;
            this.time = time;
            this.pressed = pressed;
            this.fingerId = fingerId;
        }
    }

    public enum LaneState
    {
        pressed,
        held,
        released
    }

    //ORDERING IS IMPORTANT HERE; POLL_INPUT MUST OCCUR BEFORE ALL JUDGING.
    //UNSURE HOW TO DO THIS
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public const int MaxTouches = 10;

        public NativeArray<TouchPoint> touchPoints;
        public NativeArray<LaneState> laneStates;
        public int safeIndex = 0;

        public Camera cameraCast;
        [HideInInspector]
        public float yLeniency;
        public float BaseYLenDist { get; private set; }

        void Awake()
        {
            Instance = this;

            touchPoints = new NativeArray<TouchPoint>(new TouchPoint[MaxTouches], Allocator.Persistent);
            laneStates = new NativeArray<LaneState>(new LaneState[4], Allocator.Persistent);

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
            CalculateYLeniency(); //SHOULDNT BE RECALCULATED FOR CHARTS WITHOUT CAMERA MOTION

            for (int ls = 0; ls < 4; ls++)
                laneStates[ls] = LaneState.released;

            for(int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];

                if (t.phase == TouchPhase.Began && FreeId(t.fingerId) && SafeIndex() != MaxTouches) 
                {

                    (float2 ipt, int lane) = PerformRayCast(t);
                    int timeT = (int)math.round((time - t.deltaTime) * 1000);
                    touchPoints[safeIndex] = new TouchPoint(ipt, lane, timeT, true, t.fingerId);
                    if (lane != -1)
                        laneStates[lane] = LaneState.held;

                } 
                else if (t.phase == TouchPhase.Moved)
                {
                    int index = IdIndex(t.fingerId);
                    if(index == -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        (float2 ipt, int lane) = PerformRayCast(t);
                        int timeT = (int)math.round((time - t.deltaTime) * 1000);

                        tp.inputPlanePoint = ipt;
                        tp.lane = lane;
                        tp.time = timeT;
                        tp.pressed = false;

                        touchPoints[index] = tp;

                        if (lane != -1 && laneStates[lane] == LaneState.released)
                            laneStates[lane] = LaneState.held;
                    }

                }
                else if (t.phase == TouchPhase.Stationary)
                {

                    int index = IdIndex(t.fingerId);
                    if (index == -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        int timeT = (int)math.round((time - t.deltaTime) * 1000);

                        tp.time = timeT;
                        tp.pressed = false;

                        touchPoints[index] = tp;

                        if (tp.lane != -1 && laneStates[tp.lane] == LaneState.released)
                            laneStates[tp.lane] = LaneState.held;
                    }

                } 
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {

                    int index = IdIndex(t.fingerId);
                    if (index != -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        tp.fingerId = -1;

                        touchPoints[index] = tp;
                    }

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

            float2 ipt;
            float ratio;

            if (Mathf.Approximately(ray.direction.z, 0))
            {
                ipt = new float2(ray.origin.x, 0);
            }
            else
            {
                ratio = ray.origin.z / ray.direction.z;
                ipt = new float2(ray.origin.x - ray.direction.x * ratio, ray.origin.y - ray.direction.y * ratio);
            }

            int lane = -1;
            if (ipt.y < Constants.ArcYZero * yLeniency)
            {
                ratio = ray.origin.y / ray.direction.y;
                float laneX = ray.origin.x - ray.direction.x * ratio;
                lane = Convert.XToTrack(laneX);
            }

            return (ipt, lane);
        }
    }
}