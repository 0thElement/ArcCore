using System;
using UnityEngine;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Data
{
    public class ArcColorFSM
    {
        public enum State {
            //Waiting for new finger
            Await,
            //Waiting for assigned finger to be lifted
            AwaitLift,
            //Listening and listening for a finger
            Listening,
            //The assigned finger is no longer touching the screen
            Lifted,
            //Lifted and a finger is trying to touch this Color
            LiftedRed,
            //Wrong finger held this Color
            Red,
            //Grace period where any finger can hold any arc
            Grace
        }

        public enum Event {
            //A finger hit this Color
            Collide,
            //The associated finger is no longer touching the screen
            Lift,
            //Wrong finger hit this Color
            WrongFinger,
            //Global timer went past the schedule
            Scheduled,
            //No arcs of this Color is present in the judge range
            Rest,
            //Opposite of above
            Unrest,
            //Grace period activated
            Grace
        }

        //Acts as a timer inbetween state changes
        private int schedule;
        private State state;
        public State _state => state;
        private Action<int>[,] fsm;

        public float redmix
        {
            get 
            {
                if (state != State.Red && state != State.LiftedRed) return 0;
                float result = (float)(schedule - PlayManager.ReceptorTime) / Constants.ArcRedArcWindow;
                return Mathf.Clamp(result, 0, 1);
            }
        }
        public int Color { get; private set; }
        private int fingerId = TouchPoint.NullId;
        public int FingerId
        {
            get => fingerId;
        }

        public ArcColorFSM(int color)
        {
            schedule = 0;
            this.Color = color;
            state = State.Await;
            fsm = new Action<int>[7,7] {
                             //Collide , Lift   , WrongFinger, Scheduled, Rest     , Unrest, Grace
                /* Await     */{Assign , null   , null       , null     , null     , null  , Grace},
                /* AwaitLift */{null   , Reset  , null       , null     , null     , Unrest, Grace},
                /* Listening */{null   , Lift   , Red        , null     , AwaitLift, null  , Grace},
                /* Lifted    */{LiftRed, null   , null       , Reset    , Reset    , null  , Grace},
                /* LiftedRed */{null   , null   , null       , Reset    , Reset    , null  , Grace},
                /* Red       */{null   , LiftRed, null       , StopRed  , null     , null  , Grace},
                /* Grace     */{null   , null   , null       , Reset    , null     , null  , Grace}
            };
        } 
        public void Execute(Event e, int id = TouchPoint.NullId)
        {
            Action<int> action = fsm[(int)state, (int)e];
            if (action != null) action.Invoke(id);
        }
        public void CheckSchedule()
        {
            if (PlayManager.ReceptorTime >= schedule)
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
            schedule = PlayManager.ReceptorTime + Constants.ArcRedArcWindow;
            state = State.Lifted;
        }
        private void Red(int id)
        {
            schedule = PlayManager.ReceptorTime + Constants.ArcRedArcWindow;
            state = State.Red;
        }
        private void LiftRed(int id)
        {
            state = State.LiftedRed;
        }
        private void AwaitLift(int id)
        {
            state = State.AwaitLift;
        }
        private void Unrest(int id)
        {
            state = State.Listening;
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
            schedule = PlayManager.ReceptorTime + Constants.ArcGraceDuration;
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