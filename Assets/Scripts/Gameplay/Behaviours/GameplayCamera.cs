using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;
using ArcCore.Gameplay.Utility;
using ArcCore.Parsing.Data;
using Unity.Mathematics;
using ArcCore.Math;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Behaviours
{
    public class GameplayCamera : MonoBehaviour
    {
        public static GameplayCamera Instance { get; private set; }

        public bool isUpdating;

        /// <summary>
        /// All movements attached to this camera, assumed to be sorted by time.
        /// </summary>
        public CameraEvent[] cameraMovements;
        public int firstInactiveIndex;
        public List<int> activeIndices;
        /// <summary>
        /// Is the camera currently reset?
        /// </summary>
        public bool isReset;

        public int[] resetTimings;
        public int currentResetTiming;

        public PosRot accumulate;

        /// <summary>
        /// The internal camera.
        /// </summary>
        public Camera innerCam;

        public float AspectRatio
            => (float)innerCam.pixelWidth / innerCam.pixelHeight;

        public const float Ratio4By3 = 4f / 3f;
        public const float Ratio16By9 = 16f / 9f;

        public float AspectRatioLerp
            => math.clamp((AspectRatio - Ratio4By3) / (Ratio16By9 - Ratio4By3), 0, 1);

        public float3 ResetPosition
            => math.lerp(
                new float3(0, 9, 9),
                new float3(0, 9, 8),
               AspectRatioLerp
            );
        public float3 ResetRotation
            => math.lerp(
                new float3(26.5f, 180, 0),
                new float3(27.4f, 180, 0),
                AspectRatioLerp
            );

        public PosRot ResetPosRot
            => new PosRot(ResetPosition, ResetRotation);

        public float FieldOfView
            => math.lerp(50, 65, AspectRatioLerp);

        public void Reset()
        {
            accumulate = ResetPosRot;
            isReset = true;

            //print("sus");
        }
        public void Start()
        {
            Instance = this;

            innerCam = GetComponent<Camera>();
            innerCam.fieldOfView = FieldOfView;
            innerCam.nearClipPlane = 1;
            innerCam.farClipPlane = 5000;

            Reset();
            transform.SetPositionAndRotation(accumulate);

            activeIndices = new List<int>();
        }

        public void Update()
        {
            if (!isUpdating) return;

            if (cameraMovements.Length > 0) UpdateMove();
            if (isReset) UpdateTilt();

            transform.SetPositionAndRotation(accumulate);
        }

        public void UpdateMove()
        {
            int time = Conductor.Instance.receptorTime;

            //handle resets.
            bool needsReset = false;
            while (currentResetTiming < resetTimings.Length && resetTimings[currentResetTiming] < time)
            {
                needsReset = true;
                currentResetTiming++;
            }
            if (needsReset)
            {
                Reset();
            }

            //update active indices
            while (firstInactiveIndex < cameraMovements.Length && time > cameraMovements[firstInactiveIndex].Timing)
            {
                activeIndices.Add(firstInactiveIndex);

                //move on to the next movement.
                firstInactiveIndex++;
            }

            //handle active movements.
            var toRemove = new List<int>();
            foreach (int i in activeIndices)
            {
                //check if index has ended.
                if (cameraMovements[i].EndTiming < time)
                {
                    //add remaining if reset not encountered (to prevent bugginess)
                    if (!needsReset)
                    {
                        accumulate += cameraMovements[i].Remaining;
                    }

                    //mark for removal
                    toRemove.Add(i);
                } 
                else 
                {
                    //update all internal variables
                    cameraMovements[i].Update(time);

                    //add delta to accumulate
                    accumulate += cameraMovements[i].delta;
                }

                isReset = false;
            }

            /*
            if (activeIndices.Count > 0)
            {
                print($"{{{cameraMovements[activeIndices[0]].targetDelta}, {transform.position}, {transform.eulerAngles}}}");
            }
            */

            //remove dead indices.
            foreach (int i in toRemove)
            {
                activeIndices.Remove(i);
            }
        }
        public void UpdateTilt()
        {

        }
    }
}