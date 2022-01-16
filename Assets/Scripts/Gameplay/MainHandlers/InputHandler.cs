// #define UPD

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Mathematics;
using ArcCore.Gameplay.Data;
using System.Collections.Generic;
using System.Collections;
using Lean.Touch;

namespace ArcCore.Gameplay.Behaviours
{
    public class InputHandler : MonoBehaviour, IEnumerable<TouchPoint>
    {
        public IEnumerator<TouchPoint> GetEnumerator()
        {
            int idx = -1;

            while (true) //yes i know. im awful.
            {
                do
                {
                    idx++;
                    if (idx >= MaxTouches) yield break;
                }
                while (touchPoints[idx].fingerId == TouchPoint.NullId);

                yield return touchPoints[idx];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The maximum number of touches which will be registered during gameplay.
        /// </summary>
        public const int MaxTouches = 10;
        
        /// <summary>
        /// The minimum distance which a touch has to move in order to recalculate its position.
        /// </summary>
        public const float TouchEps = 0.002f;

        /// <summary>
        /// The current touch points.
        /// </summary>
        [HideInInspector]
        public NativeArray<TouchPoint> touchPoints;
        /// <summary>
        /// The current minimum index which is not occupied by a meaningful touch point.
        /// </summary>
        [HideInInspector]
        public int safeIndex = 0;

        /// <summary>
        /// A track array which tracks which tracks are currently held.
        /// </summary>
        [HideInInspector]
        public Struct4X<int> tracksHeld;
        public Struct4X<bool> tracksTapped;

        /// <summary>
        /// The camera to be used in casting raw inputs.
        /// </summary>
        public Camera CameraCast => PlayManager.GameplayCamera.innerCam;

        public InputVisualFeedback inputVisualFeedback;

        void Awake()
        {
            touchPoints = new NativeArray<TouchPoint>(MaxTouches, Allocator.Persistent);
            tracksHeld = default;
            //EnhancedTouchSupport.Enable();

            for(int i = 0; i < MaxTouches; i++)
            {
                var t = touchPoints[i];
                t.fingerId = TouchPoint.NullId;
                touchPoints[i] = t;
            }
        }

        void Update()
        {
            if (!PlayManager.IsUpdating) return;

            PollInput();

            inputVisualFeedback.DisableLines();
            for (int i = 0; i < MaxTouches; i++)
            {
                var t = touchPoints[i];
                if (t.fingerId == TouchPoint.NullId)
                    continue;

                if (t.InputPlaneValid && t.inputPosition.Value.y > 2f)
                {
                    inputVisualFeedback.HorizontalLineAt(t.inputPosition.Value.y, i);
                }
                if (t.TrackValid)
                {
                    inputVisualFeedback.HighlightLane(t.track - 1);
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
#endif
            }
        }

        void OnDestroy()
        {
            touchPoints.Dispose();
        }
        
        /// <summary>
        /// Get the index of the first (and, if code executes correctly, only) touch point with an id of a given value.
        /// If no point is found, this returns -1.
        /// </summary>
        private int IdIndex(int id)
        {
            for (int i = 0; i < MaxTouches; i++)
                if (touchPoints[i].fingerId == id)
                    return i;
            return -1;
        }
        
        /// <summary>
        /// Get the first index for which no valid touch point exists.
        /// </summary>
        private int SafeIndex()
        {
            for (int i = 0; i < MaxTouches; i++)
                if (touchPoints[i].IsNull)
                    return safeIndex = i;
            return safeIndex = MaxTouches;
        }

        /// <summary>
        /// Update current input.
        /// </summary>
        public void PollInput()
        {
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
                        touchPoints[index] = TouchPoint.Null;
                        if (tp.track != -1) tracksHeld[tp.track]--;
                    }

                    continue;
                }

                //no more space
                if (SafeIndex() == MaxTouches) continue;
                index = IdIndex(f.Index);

                //tapped
                //hardware index does not exist == touch was registered just now == tap
                if (index == -1)
                {
                    (float2? exact, Rect2D? ipt, int track) = Projection.PerformInputRaycast(CameraCast.ScreenPointToRay(f.ScreenPosition));
                    touchPoints[safeIndex] = new TouchPoint(exact, ipt, track, PlayManager.Conductor.receptorTime, TouchPoint.Status.Tapped, f.Index);

                    if(track != -1)
                    {
                        tracksHeld[track]++;
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

                    (tp.inputPosition, tp.inputPlane, tp.track) = Projection.PerformInputRaycast(CameraCast.ScreenPointToRay(f.ScreenPosition));
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