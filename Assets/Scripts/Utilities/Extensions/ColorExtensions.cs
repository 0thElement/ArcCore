using System;
using UnityEngine;

namespace ArcCore.Utilities
{
    public static class ColorExtensions
    {
        public static Color ToColor(this string hexcode)
        {
            if (hexcode[0] != '#')
            {
                throw new Exception("sus");
            }

            int content = int.Parse(hexcode.Substring(1), System.Globalization.NumberStyles.HexNumber);

            return new Color32(
                (byte)((content & 0xFF0000) >> 16),
                (byte)((content & 0x00FF00) >> 8),
                (byte)(content & 0x0000FF),
                0xFF
            );
        }

        public static string ToHexcode(this Color value)
        {
            Color32 value32 = value;
            return $"#{value32.r:x2}{value32.g:x2}{value32.b:x2}";
        }
    }
}
