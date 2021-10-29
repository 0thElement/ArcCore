using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ArcCore.Serialization
{
    public class JsonPresetConverter<T> : JsonConverter<T>
        where T : ICloneable
    {
        private readonly Dictionary<string, T> givenValues;
        private readonly bool serializePreset;
        public JsonPresetConverter(Dictionary<string, T> givenValues, bool serializePreset = true)
        {
            if (typeof(T) == typeof(string))
            {
                throw new ArgumentException("Type of preset cannot be string.");
            }

            this.givenValues = givenValues;
            this.serializePreset = serializePreset;
        }

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            int i = 0;
            while (serializer.Converters[i].GetType() != GetType()) i++;
            serializer.Converters.RemoveAt(i);

            JObject obj;

            if (reader.Value is string preset)
            {
                obj = JObject.FromObject(givenValues[preset], serializer);
            }
            else
            {
                obj = serializer.Deserialize<JObject>(reader);

                if (obj.Count != 0)
                {
                    var firstProp = obj.First as JProperty;
                    if (firstProp.Name == "$base")
                    {
                        var newObj = JObject.FromObject(givenValues[(string)firstProp.Value], serializer);

                        obj.Remove("$base");
                        foreach (var kvp in obj)
                        {
                            if (newObj.ContainsKey(kvp.Key)) newObj[kvp.Key] = kvp.Value;
                            else newObj.Add(kvp.Key, kvp.Value);
                        }

                        //swap! owo
                        obj = newObj;
                    }
                }
            }

            var ret = obj.ToObject<T>(serializer);
            serializer.Converters.Insert(i, this);

            return ret;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            if (serializePreset)
            {
                foreach (var kvp in givenValues)
                {
                    if (kvp.Value.Equals(value))
                    {
                        writer.WriteValue(kvp.Key);
                        return;
                    }
                }
            }

            int i = 0;
            while (serializer.Converters[i].GetType() != GetType()) i++;
            serializer.Converters.RemoveAt(i);

            serializer.Serialize(writer, value);
            serializer.Converters.Insert(i, this);
        }
    }
}