using Newtonsoft.Json;
using System;
using UnityEngine;

namespace ArcCore.Serialization
{
    public class JsonColorConverter : JsonConverter<Color>
    {
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var hexcode = (string)reader.Value;

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

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            Color32 value32 = value;
            writer.WriteValue($"#{value32.r:x2}{value32.g:x2}{value32.b:x2}");
        }
    }
}