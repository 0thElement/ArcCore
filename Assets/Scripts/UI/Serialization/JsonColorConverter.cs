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
            return ColorExtensions.FromHexcode(hexcode);
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToHexcode());
        }
    }
}