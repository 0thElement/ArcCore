using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    public struct ArcColorID : ISharedComponentData
    {
        /// <summary>
        /// The colorId of this arc
        /// </summary>
        public int id;

        /// <summary>
        /// The touchId associated with this arc. Used for judgement.
        /// Id = -1 means no touch is associated with this color and judgement system should assign a new touch as soon as one collides with an arc
        /// of this color
        /// </summary>
        public int touchId;

        /// <summary>
        /// Timing at which this arc color stop being red.
        /// Scheduled by red arc checking systems.
        /// </summary>
        public int endRedArcSchedule;

        /// <summary>
        /// Timing after which <see cref="touchId"> is allowed to be reset.
        /// After an arc color is released (the current <see cref="touchId"> no longer exists), the color keep the same <see cref="touchId"> for an extended
        /// period of time before a new one can be assigned.
        /// </summary>
        public int resetTouchIdSchedule;

        public ArcColorID(int id)
        {
            this.id = id;
            this.touchId = -1;
            this.endRedArcSchedule = 0;
            this.resetTouchIdSchedule = 0;
        }
    }
}
