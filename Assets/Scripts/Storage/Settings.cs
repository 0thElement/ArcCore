using ArcCore.Storage.Data;
using ArcCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityPreferences;

namespace ArcCore.Storage
{
    public static class Settings
    {
        static Settings()
        {
            Preferences.RegisterType<Level>
            (
                (n, o) => Preferences.HasPref(n) ? FileManagement.levels.First(p => p.Id == Preferences.Get<int>(n)) : o,
                (n, v) => Preferences.Set(n, v.Id),
                n => Preferences.Delete<ulong>(n)
            );
            Preferences.RegisterType<Pack>
            (
                (n, o) => Preferences.HasPref(n) ? FileManagement.packs.First(p => p.Id == Preferences.Get<int>(n)) : o,
                (n, v) => Preferences.Set(n, v.Id),
                n => Preferences.Delete<ulong>(n)
            );
        }


        public const float DefaultSongSpeed = 1;
        public static float SongSpeed
        {
            get => Preferences.Get("song_speed", DefaultSongSpeed);
            set => Preferences.Set("song_speed", value);
        }

        /// <summary>
        /// Get the value which the given <paramref name="timing"/> will take on after squashed by a factor of <see cref="SongSpeed"/>.
        /// </summary>
        public static int GetSpeedModifiedTime(int timing) => (int)(timing / SongSpeed);

        public const float DefaultChartSpeed = 1;
        public static float ChartSpeed
        {
            get => Preferences.Get("chart_speed", DefaultSongSpeed);
            set => Preferences.Set("chart_speed", value);
        }

        public const int DefaultAudioOffset = 0;
        public static int AudioOffset
        {
            get => Preferences.Get("audio_offset", DefaultAudioOffset);
            set => Preferences.Set("audio_offset", value);
        }

        /// <summary>
        /// The colors assigned to the current arcs.
        /// </summary>
        public static readonly Color32[] DefaultArcColors =
            new Color32[]
            {
                "#0DDEEC".ToColor(),
                "#F422EB".ToColor(),
                "#33EF53".ToColor(),
                "#FFC231".ToColor()
            };
        public static Color32[] ArcColors
        {
            get => Preferences.Get("arc_colors", DefaultArcColors);
            set => Preferences.Set("arc_colors", value);
        }

        public static ulong MaxLevelId
        {
            get => Preferences.Get("max_level_id", 0UL);
            set => Preferences.Set("max_level_id", value);
        }
        public static ulong MaxPackId
        {
            get => Preferences.Get("max_pack_id", 0UL);
            set => Preferences.Set("max_pack_id", value);
        }

        public static Pack SelectedPack
        {
            get => Preferences.Get<Pack>("selected_pack_id", null);
            set => Preferences.Set<Pack>("selected_pack_id", value);
        }

        public static Level SelectedLevel
        {
            get => Preferences.Get<Level>("selected_level_id", null);
            set => Preferences.Set<Level>("selected_level_id", value);
        }

        public static Dictionary<Pack, Level> SelectedLevelsByPack
        {
            get => Preferences.Get("selected_levels_by_pack", new Dictionary<Pack, Level>());
            set => Preferences.Set("selected_levels_by_pack", value);
        }

        public static DifficultyGroup SelectedDiff
        {
            get => !Preferences.ReadBool("selected_diff.exists", false) ? null : new DifficultyGroup
            {
                Color = Preferences.Get<Color32>("selected_diff.color"),
                Name = Preferences.Get<string>("selected_diff.name"),
                Precedence = Preferences.Get<int>("selected_diff.prec")
            };
            set
            {
                Preferences.Set("selected_diff.exists", value is null);
                if (value != null)
                {
                    Preferences.Set<Color32>("selected_diff.color", value.Color);
                    Preferences.Set<string>("selected_diff.name", value.Name);
                    Preferences.Set<int>("selected_diff.prec", value.Precedence);
                }
            }
        }

        public static Color32 GetArcColor(int color)
        {
            if (color >= ArcColors.Length) return ArcColors[0];
            return ArcColors[color];
        }
    }
}