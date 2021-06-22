using System;
using System.Collections.Generic;

namespace ArcCore.Serialization
{
    public class StyleScheme : ICloneable, IEquatable<StyleScheme>
    {
        public Color trackColor;
        public Color particleColor;
        public Color tapColor;
        public Color holdColor;
        public Color arctapColor;

        public object Clone() => new StyleScheme
        {
            trackColor = trackColor,
            particleColor = particleColor,
            tapColor = tapColor,
            holdColor = holdColor,
            arctapColor = arctapColor
        };

        public bool Equals(StyleScheme other)
        {
            return trackColor == other.trackColor &&
                   particleColor == other.particleColor &&
                   tapColor == other.tapColor &&
                   holdColor == other.holdColor &&
                   arctapColor == other.arctapColor;
        }

        public override bool Equals(object obj)
            => obj is StyleScheme styleScheme && Equals(styleScheme);

        public static bool operator ==(StyleScheme a, StyleScheme b) => a.Equals(b);
        public static bool operator !=(StyleScheme a, StyleScheme b) => !(a == b);

        public static StyleScheme Light => new StyleScheme
        {
            trackColor = Color.FromHexcode("#D0D0D0"),
            tapColor = Color.FromHexcode("#A0FFA0"),
            holdColor = Color.FromHexcode("#A0FFA0"),
            arctapColor = Color.FromHexcode("#70FF70"),
            particleColor = Color.FromHexcode("#00FF00")
        };
        public static StyleScheme Conflict => new StyleScheme
        {
            trackColor = Color.FromHexcode("#D0D0D0"),
            tapColor = Color.FromHexcode("#A0FFA0"),
            holdColor = Color.FromHexcode("#A0FFA0"),
            arctapColor = Color.FromHexcode("#70FF70"),
            particleColor = Color.FromHexcode("#00FF00")
        };

        public static Dictionary<string, StyleScheme> Presets
            => new Dictionary<string, StyleScheme>
            {
            {"light", Light},
            {"conflict", Conflict}
            };
    }
}