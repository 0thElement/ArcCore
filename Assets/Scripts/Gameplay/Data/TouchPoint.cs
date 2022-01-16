using ArcCore.Mathematics;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Data
{
    public struct TouchPoint
    {
        /// <summary>
        /// The sentinel value of <see cref="fingerId"/> for touch points which are <see langword="null"/>-like.
        /// </summary>
        public const int NullId = 666 - 420 - 69 - 621 - 616 - 1377;
        /// <summary>
        /// A <see langword="null"/>-like touch point.
        /// </summary>
        public static TouchPoint Null => new TouchPoint
        {
            fingerId = NullId
        };
        /// <summary>
        /// Is this touch point <see langword="null"/>-like.
        /// </summary>
        public bool IsNull => fingerId == NullId;

        /// <summary>
        /// The status of the touch point. 
        /// <list type="table">
        /// <item><term><see cref="Status.Tapped"/></term> The touch point has just begun</item>
        /// <item><term><see cref="Status.Sustained"/></term> The touch point has remained tapped since the last frame</item>
        /// <item><term><see cref="Status.Released"/></term> The touch point has just ended</item>
        /// </list>
        /// </summary>
        public enum Status
        {
            Tapped,
            Sustained,
            Released
        }

        /// <summary>
        /// The position of the touch point on the input plane.
        /// If <see langword="null"/>, there is no valid point on the input plane.
        /// </summary>
        public float2? inputPosition;
        /// <summary>
        /// The hitbox of the touch point on the input plane.
        /// If <see langword="null"/>, there is no valid hitbox on the input plane.
        /// </summary>
        public Rect2D? inputPlane;

        /// <summary>
        /// The sentinel value of <see cref="track"/> which represents <see langword="abstract"/>tap which does not validly
        /// overlap a track.
        /// </summary>
        public const int NullTrack = -1;

        /// <summary>
        /// The track which this touch overlaps.
        /// </summary>
        public int track;
        public int tapTime;

        /// <summary>
        /// The real value of <see cref="inputPlane"/>, default if not <see cref="InputPlaneValid"/>
        /// </summary>
        public Rect2D InputPlane => inputPlane.GetValueOrDefault();
        /// <summary>
        /// Whether or not this touch point has a valid input plane area.
        /// </summary>
        public bool InputPlaneValid => inputPlane.HasValue;
        /// <summary>
        /// Whether or not this touch point overlaps a valid track.
        /// </summary>
        public bool TrackValid => track != NullTrack;

        //public int time;
        /// <summary>
        /// The status of this tap. See <see cref="Status"/> for details.
        /// </summary>
        public Status status;
        public int fingerId;

        public TouchPoint(float2? inputPosition, Rect2D? inputPlane, int track, int tapTime, Status status, int fingerId)
        {
            this.inputPosition = inputPosition;
            this.inputPlane = inputPlane;
            this.track = track;
            this.tapTime = tapTime;
            this.status = status;
            this.fingerId = fingerId;
        }
    }
}