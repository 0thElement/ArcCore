using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCore
{
    public class GameSettings
    {
        #region --Statics--
        private static bool _instanceInvalid = true;
        private static GameSettings _instance;

        /// <summary>
        /// The instance of settings. Do <b>not</b> use this item's setter unless it known to be is uninitialized (<see langword="null"/>) or that <see cref="FinalizeInstance"/> has not been called.
        /// After initialization, code must call <see cref="FinalizeInstance"/>, which will then cause the setter to throw an error on call.
        /// </summary>
        public static GameSettings Instance
        {
            get => _instance;
            set
            {
                if(_instanceInvalid)
                {
                    _instance = value;
                }
                else
                {
                    throw new Exception("You cannot set Instance directely after it has been created. Please modify it's fields instead.");
                }
            }
        }

        public static void FinalizeInstance()
            => _instanceInvalid = true;

        public static GameSettings GetDefault()
            => new GameSettings
            {
                arcColors = new List<Color>
                {
                    {new Color(0.30f, 0.77f, 0.86f, 0.75f)},
                    {new Color(0.91f, 0.37f, 0.72f, 0.75f)},
                    {new Color(0.48f, 0.89f, 0.32f, 0.75f)},
                    {new Color(0.90f, 0.66f, 0.29f, 0.75f)}
                },
                _songSpeed = 1f,
                chartSpeed = 1f,
                audioOffset = 0
            };
        #endregion

        #region Song Speed
        private float _songSpeed;

        /// <summary>
        /// The modifier which will be placed on song and chart speeds.
        /// </summary>
        public float SongSpeed
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
        public void MarkSongSpeedUsed()
        {
            songSpeedChanged = false;
        }

        /// <summary>
        /// Has the value of <see cref="SongSpeed"/> been changed since its last usage?
        /// <para>
        /// <b>NOTE:</b> users are exected to call <see cref="MarkSongSpeedUsed()"/> after reading from <see cref="SongSpeed"/> in a meaningful way.
        /// </para>
        /// </summary>
        public bool songSpeedChanged = false;

        /// <summary>
        /// Get the value which the given <paramref name="timing"/> will take on after squashed by a factor of <see cref="SongSpeed"/>.
        /// </summary>
        public int GetSpeedModifiedTime(int timing) => (int)(timing / _songSpeed);
        #endregion

        /// <summary>
        /// The chart speed provided by the user.
        /// </summary>
        public float chartSpeed;

        /// <summary>
        /// The offset provided by the user.
        /// </summary>
        public int audioOffset;

        /// <summary>
        /// The colors assigned to the current arcs.
        /// </summary>
        public List<Color> arcColors;

        public Color GetArcColor(int color)
        {
            if (color >= arcColors.Count) return arcColors[0];
            return arcColors[color];
        }
    }
}
