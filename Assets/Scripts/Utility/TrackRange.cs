namespace ArcCore.Utility
{
    public readonly struct TrackRange
    {
        /// <summary>
        /// Inclusive
        /// </summary>
        public readonly int min;
        /// <summary>
        /// <b>NOT</b> inclusive
        /// </summary>
        public readonly int max;

        public TrackRange(int min, int max)
            => (this.min, this.max) = (min, max);

        /// <summary>
        /// Check if the given track falls within the range described by this struct
        /// </summary>
        /// <param name="tr">The track to check</param>
        public bool Contains(int tr)
            => min <= tr && tr < max;

        /// <summary>
        /// Whether or not the current object represents no tracks
        /// If this struct is <c>TrackRange.none</c>, this will always return true
        /// </summary>
        public bool IsNone => (min == -1);

        /// <summary>
        /// A <c>TrackRange</c> representing no tracks
        /// </summary>
        public static readonly TrackRange none = new TrackRange(-1, 0);
    }
}
