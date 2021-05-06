using ArcCore.Math;

namespace ArcCore.Structs
{
    public struct TouchPoint
    {
        public enum Status
        {
            TAPPED,
            HELD,
            RELEASED
        }

        public Rect2D inputPlane;
        public int track;

        public bool InputPlaneValid => !inputPlane.IsNone;
        public bool TrackValid => track != -1;

        public int time;
        public Status status;
        public int fingerId;

        public TouchPoint(Rect2D inputPlane, int track, int time, Status status, int fingerId)
        {
            this.inputPlane = inputPlane;
            this.track = track;
            this.time = time;
            this.status = status;
            this.fingerId = fingerId;
        }
    }
}