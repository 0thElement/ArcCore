using System;
using UnityEngine;
using ArcCore.Gameplay.Behaviours;

namespace ArcCore.Gameplay.Data
{
    public class ArcColorFSM
    {
        public enum State {
            //Waiting for new finger
            Await,
            //Listening and listening for a finger
            Listening,
            //The assigned finger is no longer touching the screen
            Lifted,
            //Lifted and a finger is trying to touch this color
            LiftedRed,
            //Wrong finger held this color
            Red,
            //Grace period where any finger can hold any arc
            Grace
        }

        public enum Event {
            //A finger hit this color
            Collide,
            //The associated finger is no longer touching the screen
            Lift,
            //Wrong finger hit this color
            WrongFinger,
            //Global timer went past the schedule
            Scheduled,
            //No arcs of this color is present in the judge range
            Rest,
            //Grace period activated
            Grace
        }

        //Acts as a timer inbetween state changes
        private int schedule;
        private State state;
        private Action<int>[,] fsm;

        public float redmix
        {
            get 
            {
                if (state != State.Red && state != State.LiftedRed) return 0;
                float result = (float)(schedule - Conductor.Instance.receptorTime) / Constants.ArcRedArcWindow;
                return Mathf.Clamp(result, 0, 1);
            }
        }
        private int color;
        private int fingerId = TouchPoint.NullId;
        public int FingerId
        {
            get => fingerId;
        }

        public ArcColorFSM(int color)
        {
            schedule = 0;
            this.color = color;
            state = State.Await;
            fsm = new Action<int>[6,6] {
                             //Collide , Lift   , WrongFinger, Scheduled, Rest , Grace
                /* Await     */{Assign , null   , null       , null     , null , Grace},
                /* Listening */{null   , Lift   , Red        , null     , Reset, Grace},
                /* Lifted    */{LiftRed, null   , null       , Reset    , Reset, Grace},
                /* LiftedRed */{null   , null   , null       , Reset    , Reset, Grace},
                /* Red       */{null   , LiftRed, null       , StopRed  , null , Grace},
                /* Grace     */{null   , null   , null       , Reset    , null , Grace}
            };
        } 
        public void Execute(Event e, int id = TouchPoint.NullId)
        {
            Action<int> action = fsm[(int)state, (int)e];
            if (action != null) action.Invoke(id);
        }
        public void CheckSchedule()
        {
            if (Conductor.Instance.receptorTime >= schedule)
                Execute(Event.Scheduled);
        }

        private void Assign(int id)
        {
            fingerId = id;
            state = State.Listening;
        }
        private void Lift(int id)
        {
            fingerId = TouchPoint.NullId;
            schedule = Conductor.Instance.receptorTime + Constants.ArcRedArcWindow;
            state = State.Lifted;
        }
        private void Red(int id)
        {
            schedule = Conductor.Instance.receptorTime + Constants.ArcRedArcWindow;
            state = State.Red;
        }
        private void LiftRed(int id)
        {
            state = State.LiftedRed;
        }
        private void Reset(int id)
        {
            fingerId = TouchPoint.NullId;
            state = State.Await;
            schedule = int.MaxValue;
        }
        private void Grace(int id)
        {
            fingerId = TouchPoint.NullId;
            schedule = Conductor.Instance.receptorTime + Constants.ArcGraceDuration;
            state = State.Grace;
        }
        private void StopRed(int id)
        {
            state = State.Listening;
            schedule = int.MaxValue;
        }

        public bool IsAwaiting()
            => state == State.Await;

        public bool IsValidId(int fingerId)
            => state == State.Grace || fingerId == this.fingerId;

        public bool IsRedArc()
            => state == State.Red || state == State.LiftedRed;
    }
}