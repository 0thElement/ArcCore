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
        public AABB inputPlanePoint;
        public int lane; //CHANGE THIS TO YET ANOTHER AABB
        public int time;
        public bool pressed;
        public int fingerId;

        public TouchPoint(AABB inputPlanePoint, int lane, int time, bool pressed, int fingerId)
        {
            this.inputPlanePoint = inputPlanePoint;
            this.lane = lane;
            this.time = time;
            this.pressed = pressed;
            this.fingerId = fingerId;
        }
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
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];

                if (t.phase == TouchPhase.Began && FreeId(t.fingerId) && SafeIndex() != MaxTouches)
                {

                    (AABB ipt, int lane) = PerformRaycast(t);
                    int timeT = (int)math.round((time - t.deltaTime) * 1000);
                    touchPoints[safeIndex] = new TouchPoint(ipt, lane, timeT, true, t.fingerId);

                }
                else if (t.phase == TouchPhase.Moved)
                {
                    int index = IdIndex(t.fingerId);
                    if (index == -1)
                    {
                        TouchPoint tp = touchPoints[index];

                        (AABB ipt, int lane) = PerformRaycast(t);
                        int timeT = (int)math.round((time - t.deltaTime) * 1000);

                        tp.inputPlanePoint = ipt;
                        tp.lane = lane;
                        tp.time = timeT;
                        tp.pressed = false;

                        touchPoints[index] = tp;
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

        public (AABB ipt, int lane) PerformRaycast(Touch t)
        {
            Ray ray = cameraCast.ScreenPointToRay(new Vector3(t.position.x, t.position.y));
            float3 ipt = ProjectionMaths.ProjectOntoXYPlane(ray);
            AABB ipt_aabb = ProjectionMaths.GetAABBJudgePoint(cameraCast.transform.position, ipt);

            int lane = 0;
            float3 lane_proj = ProjectionMaths.ProjectOntoXZPlane(ray);
            if (ipt.z < 1) { 
                lane = Convert.XToTrack(lane_proj.x);
            }

            return (ipt_aabb, lane);
        }
    }
}