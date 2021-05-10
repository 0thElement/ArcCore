using ArcCore.Math;
using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Structs
{
    public struct TouchPoint
    {
        public enum Status
        {
            Tapped,
            Sustained,
            Released
        }

        public Rect2D? inputPlane;
        public int track;

        public Rect2D InputPlane => inputPlane.GetValueOrDefault();
        public bool InputPlaneValid => inputPlane is not null;
        public bool TrackValid => track != -1;

        //public int time;
        public Status status;
        public int fingerId;

        public TouchPoint(Rect2D? inputPlane, int track, /*int time,*/ Status status, int fingerId)
        {
            this.inputPlane = inputPlane;
            this.track = track;
            //this.time = time;
            this.status = status;
            this.fingerId = fingerId;
        }
    }
}