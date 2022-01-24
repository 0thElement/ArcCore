using ArcCore.Utitlities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityPreferences;

namespace ArcCore
{
    public static class Settings
    {
        public const float DefaultSongSpeed = 1;
        public static float SongSpeed
        {
            get => Preferences.GetFloat("song_speed", DefaultSongSpeed);
            set => Preferences.SetFloat("song_speed", value);
        }

        /// <summary>
        /// Get the value which the given <paramref name="timing"/> will take on after squashed by a factor of <see cref="SongSpeed"/>.
        /// </summary>
        public static int GetSpeedModifiedTime(int timing) => (int)(timing / SongSpeed);

        public const float DefaultChartSpeed = 1;
        public static float ChartSpeed
        {
            get => Preferences.GetFloat("chart_speed", DefaultSongSpeed);
            set => Preferences.SetFloat("chart_speed", value);
        }

        public const int DefaultAudioOffset = 0;
        public static int AudioOffset
        {
            get => Preferences.GetInt("audio_offset", DefaultAudioOffset);
            set => Preferences.SetInt("audio_offset", value);
        }

        /// <summary>
        /// The colors assigned to the current arcs.
        /// </summary>
        public static readonly Color32[] DefaultArcColors =
            new Color32[]
            {
                ColorExtensions.FromHexcode("#0DDEEC"),
                ColorExtensions.FromHexcode("#F422EB"),
                ColorExtensions.FromHexcode("#33EF53"),
                ColorExtensions.FromHexcode("#FFC231")
            };
        public static Color32[] ArcColors
        {
            get => Preferences.GetArray("arc_colors", DefaultArcColors);
            set => Preferences.SetArray("arc_colors", value);
        }

        public static ulong MaxLevelId
        {
            get => Preferences.GetULong("max_level_id", 0);
            set => Preferences.SetULong("max_level_id", value);
        }
        public static ulong MaxPackId
        {
            get => Preferences.GetULong("max_pack_id", 0);
            set => Preferences.SetULong("max_pack_id", value);
        }

        public static Color32 GetArcColor(int color)
        {
            if (color >= ArcColors.Length) return ArcColors[0];
            return ArcColors[color];
        }
    }
}
