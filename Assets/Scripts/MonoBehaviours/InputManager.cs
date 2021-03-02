using ArcCore.Utility;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public enum TouchStatus
    {
        NONE,
        PRESSED,
        HELD
    }

    public struct TouchPoint
    {
        public float2 inputPlanePoint;
        public int lane;
        public float time;
        public TouchStatus status;

        public TouchPoint(float2 inputPlanePoint, int lane, float time, TouchStatus status)
        {
            this.inputPlanePoint = inputPlanePoint;
            this.lane  = lane;
            this.time = time;
            this.status = status;
        }

        public bool IsValid => status != TouchStatus.NONE;
    }
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public const int MaxTouches = 10;

        public TouchPoint[] touchPoints = new TouchPoint[MaxTouches];

        public Camera cameraCast;
        [HideInInspector]
        public float yLeniency;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            CreateConnection();
        }

        public void KillConnection()
            => Conductor.Instance.OnTimeCalculated -= PollInput;

        public void CreateConnection()
            => Conductor.Instance.OnTimeCalculated += PollInput;

        private void PollInput(float time)
        {
            CalculateYLeniency(); //SHOULDNT BE RECALCULATED FOR CHARTS WITHOUT CAMERA MOTION
            for(int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.touches[i];

                if (t.fingerId > MaxTouches)
                    continue;

                //todo: FIX ALL THE ISSUES WITH "t.fingerId - 1", FINGER-ID MIGHT BE ZERO INDEXED JASDHKJDHJKD
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved) 
                {
                    (float2 ipt, int lane) = PerformRayCast(t);
                    //UNSURE IF FINGER ID IS ZERO-INDEXED, TEST LATER
                    //UNSURE IF DELTA-TIME AND TIME ARE OF MISMATCHED TIME UNITS, FRICK
                    touchPoints[t.fingerId - 1] = new TouchPoint(ipt, lane, time - t.deltaTime, TouchStatus.PRESSED);
                } 
                else if (t.phase == TouchPhase.Stationary)
                {
                    touchPoints[t.fingerId - 1].status = TouchStatus.HELD;
                    touchPoints[t.fingerId - 1].time = time - t.deltaTime;
                } 
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    touchPoints[t.fingerId - 1].status = TouchStatus.NONE;
                }
            }
        }

        public void CalculateYLeniency()
        {
            Vector3 basepoint = cameraCast.WorldToScreenPoint(Vector3.zero);
            Vector3 toppoint = cameraCast.WorldToScreenPoint(Vector3.up * 4); //REPLACE 4 WITH HEIGHT OF INPUT AREA
            float dist = Vector3.Distance(basepoint, toppoint);
            if (Mathf.Approximately(dist, 0))
                yLeniency = float.PositiveInfinity;
            else
                yLeniency = 1 / dist; // REPLACE 1 WITH THE NORMAL VALUE OF DIST; PROBABLY A CONST OR CALCULATED IN AWAKE BUT IM TOO LAZY LOL
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

            int lane = 0;
            if (ipt.y < 4 / 5 * yLeniency) //REPLACE 4 WITH THE NORMAL HEIGHT OF THE INPUT AREA
            {
                ratio = ray.origin.y / ray.direction.y;
                float laneX = ray.origin.x - ray.direction.x * ratio;
                lane = Convert.XToTrack(laneX);
            }

            return (ipt, lane);
        }
    }
}