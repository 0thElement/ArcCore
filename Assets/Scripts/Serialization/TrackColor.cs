using ArcCore.Serialization.NewtonsoftExtensions;
using UnityEngine;

namespace ArcCore.Serialization
{
    public struct TrackColor : IJsonPresetSpecialization<Color>
    {
        public Color Value { get; set; }

        [JsonPreset]
        public static Color Light => new Color();
        [JsonPreset]
        public static Color Conflict => new Color();
    }
}