using System;
using UnityEngine;
using ArcCore.Utilities;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Data
{
    public class ArcColorState
    {
        private static Dictionary<int, ArcColorState> assignedColorOfFingerId = new Dictionary<int, ArcColorState>();
        public static ArcColorState GetAssignedColorOfFingerId(int fingerId)
        {
            if (assignedColorOfFingerId.ContainsKey(fingerId)) return assignedColorOfFingerId[fingerId];
            return null;
        }

        private static void AssignColorToFingerId(int fingerId, ArcColorState state)
        {
            if (assignedColorOfFingerId.ContainsKey(fingerId)) assignedColorOfFingerId[fingerId] = state;
            else assignedColorOfFingerId.Add(fingerId, state);
        }

        public int Color { get; private set; }
        private int fingerId = TouchPoint.NullId;
        public int FingerId
        {
            get => fingerId;
        }

        //Acts as a timer inbetween state changes
        private int redArcSchedule;
        private int freezeInputSchedule;
        private int graceSchedule;
        private int correctHitSchedule;
        private bool resting;

        public ArcColorState(int color)
        {
            redArcSchedule = -1;
            freezeInputSchedule = -1;
            graceSchedule = -1;
            resting = true;
            this.Color = color;
        } 

        public bool AcceptsInput(int timing) => timing >= freezeInputSchedule || IsGrace(timing);
        public bool IsGrace(int timing) => timing <= graceSchedule;
        public bool IsHeld(int timing) => timing <= correctHitSchedule;
        public bool CanAssignNewInput(int timing) => fingerId == TouchPoint.NullId || IsGrace(timing);

        public float Redmix(int timing)
        {
            if (timing > redArcSchedule) return 0;
            float result = (float)(redArcSchedule - timing) / Constants.ArcRedArcWindow;
            return Mathf.Clamp(result, 0, 1);
        }

        //Finger no longer exists
        public void Lift(int timing)
        {
            if (!resting) freezeInputSchedule = timing + Constants.ArcRedArcWindow;
            assignedColorOfFingerId[fingerId] = null;
            fingerId = TouchPoint.NullId;
        }

        //Grace period started
        public void Grace(int timing)
        {
            graceSchedule = timing + Constants.ArcGraceDuration;
            redArcSchedule = -1;
            freezeInputSchedule = -1;
        }

        //No arcs present on this frame
        public void Rest(int timing)
        {
            freezeInputSchedule = -1;
            redArcSchedule = -1;
            resting = true;
        }

        public void Unrest(int timing)
        {
            resting = false;
        }

        public void RedArc(int timing)
        {
            redArcSchedule = timing + Constants.ArcRedArcWindow;
        }

        ///<summary>
        ///Update the internal state of this arc's color through an input attempt from a finger.
        ///</summary>
        ///<returns>Whether the attempt was successful</returns>
        public bool Hit(int fingerId, int timing)
        {
            if (!AcceptsInput(timing))
            {
                RedArc(timing);
                return false;
            }
            resting = false;
            if (CanAssignNewInput(timing))
            {
                this.fingerId = fingerId;
                AssignColorToFingerId(fingerId, this);
                correctHitSchedule = timing + Constants.FarWindow;
                redArcSchedule = -1;
                freezeInputSchedule = -1;
                return true;
            }

            if (this.fingerId == fingerId)
            {
                correctHitSchedule = timing + Constants.FarWindow;
                redArcSchedule = -1;
                return true;
            }

            if (!IsHeld(timing))
            {
                RedArc(timing);
                return false;
            }

            return false;
        }
    }
}