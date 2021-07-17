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
            Red
        }

        public enum Event {
            Collide,
            Lift,
            WrongFinger,
            Scheduled
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
                // return Mathf.Clamp(result, 0, 1);
                return 1;
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
            fsm = new Action<int>[5,4] {
                             //Collide , Lift   , WrongFinger, Scheduled
                /* Await     */{Assign , null   , null       , null   },
                /* Listening */{null   , Lift   , Red        , null   },
                /* Lifted    */{LiftRed, null   , null       , Reset  },
                /* LiftedRed */{null   , null   , null       , Reset  },
                /* Red       */{null   , LiftRed, null       , StopRed}
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
            Debug.Log(color + " Assign " + id);
        }
        private void Lift(int id)
        {
            fingerId = TouchPoint.NullId;
            schedule = Conductor.Instance.receptorTime + Constants.ArcRedArcWindow;
            state = State.Lifted;
            Debug.Log(color + " Lifted");
        }
        private void Red(int id)
        {
            schedule = Conductor.Instance.receptorTime + Constants.ArcRedArcWindow;
            state = State.Red;
            Debug.Log(color + " Red");
        }
        private void LiftRed(int id)
        {
            state = State.LiftedRed;
            Debug.Log(color + " LiftRed");
        }
        private void Reset(int id)
        {
            fingerId = TouchPoint.NullId;
            state = State.Await;
            Debug.Log(color + " Reset");
        }
        private void StopRed(int id)
        {
            state = State.Listening;
            Debug.Log(color + " Recovered");
        }

        public bool IsAwaiting()
        {
            return state == State.Await;
        }
        public void CancelSchedule()
        {
            schedule = 0;
        }
    }
}