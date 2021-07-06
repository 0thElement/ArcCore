namespace ArcCore
{
    public static class GameSettings
    {
        private static float _songSpeed = 1f;

        /// <summary>
        /// The modifier which will be placed on song and chart speeds.
        /// </summary>
        public static float SongSpeed
        {
            get => _songSpeed;
            set
            {
                if (_songSpeed == value)
                    return;

                songSpeedChanged = true;
                _songSpeed = value;
            }
        }

        /// <summary>
        /// Indicate that the current value of <see cref="SongSpeed"/> has been used in a meaningful manner.
        /// </summary>
        public static void MarkSongSpeedUsed()
        {
            songSpeedChanged = false;
        }

        /// <summary>
        /// Has the value of <see cref="SongSpeed"/> been changed since its last usage?
        /// <para>
        /// <b>NOTE:</b> users are exected to call <see cref="MarkSongSpeedUsed()"/> after reading from <see cref="SongSpeed"/> in a meaningful way.
        /// </para>
        /// </summary>
        public static bool songSpeedChanged = false;

        /// <summary>
        /// Get the value which the given <paramref name="timing"/> will take on after squashed by a factor of <see cref="SongSpeed"/>.
        /// </summary>
        public static int GetSpeedModifiedTime(int timing) => (int)(timing / _songSpeed);
    }
}
