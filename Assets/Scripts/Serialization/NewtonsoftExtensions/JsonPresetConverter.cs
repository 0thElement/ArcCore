using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArcCore.Serialization.NewtonsoftExtensions
{

    public sealed class JsonPresetConverter : JsonConverter
    {
        private readonly Dictionary<Type, Dictionary<string, object>> storedTypePresets = new Dictionary<Type, Dictionary<string, object>>();
        private const string BaseKeyword = "$base";

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetGenericInterface(typeof(IJsonPresetSpecialization<>)) != null 
                || objectType.IsDefined(typeof(JsonHasPresetsAttribute));
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Remove this converter
            int i = 0;
            while (serializer.Converters[i].GetType() != GetType()) i++;
            serializer.Converters.RemoveAt(i);

            //Get the value type of this item (accounting for specialization)
            var valueType = objectType;
            var specialization = objectType.GetGenericInterface(typeof(IJsonPresetSpecialization<>));
            if (specialization != null)
                valueType = specialization.GenericTypeArguments[0];

            //Get corresponding presets
            Dictionary<string, object> presets;
            if(storedTypePresets.ContainsKey(objectType))
            {
                presets = storedTypePresets[objectType];
            }
            else
            {
                var newPresets = GetPresets(objectType, valueType);
                storedTypePresets.Add(objectType, newPresets);
                presets = newPresets;
            }

            //Read value
            var value = ReadWithPresets(presets, reader, valueType, serializer);

            if (value.GetType() != valueType)
                throw new JsonReaderException("Value was of incorrect type.");

            //Add back this converter
            serializer.Converters.Insert(i, this);

            //Return read value as real type
            if (objectType == valueType)
                return value;
            else return MakeSpecializedInstance(value, objectType, valueType);
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //Nothing special at all.

            //Remove this converter
            int i = 0;
            while (serializer.Converters[i].GetType() != GetType()) i++;
            serializer.Converters.RemoveAt(i);

            //Write
            serializer.Serialize(writer, value);

            //Add back this converter
            serializer.Converters.Insert(i, this);
        }

        /// <summary>
        /// Get all the presets defined for a given type, specialized or not.
        /// </summary>
        /// <param name="declaringType"></param>
        /// <returns></returns>
        private static Dictionary<string, object> GetPresets(Type declaringType, Type valueType)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            foreach (var m in declaringType.GetValueMembers(BindingFlags.Static | BindingFlags.Public).Where(m => m.GetValueType() == valueType))
            {
                object value = m.GetStaticValue();

                //Check for JsonPresetAttribute
                var singlePresetAttr = m.GetCustomAttribute<JsonPresetAttribute>();
                if (singlePresetAttr != null)
                {
                    var name = singlePresetAttr.NameOverride ?? m.Name;
                    dict.Add(NormalizeName(name), value);
                }
            }

            return dict;
        }
        private static string NormalizeName(string name)
        {
            var n1 = char.ToLower(name[0]);
            return n1 + name.Substring(1);
        }

        private static object MakeSpecializedInstance(object inner, Type objectType, Type valueType)
        {
            var setter = objectType.GetProperties()
                                   .First(p => p.PropertyType == valueType && p.Name == nameof(IJsonPresetSpecialization<object>.Value))
                                   .SetMethod;

            var newInst = Activator.CreateInstance(objectType);
            setter.Invoke(newInst, new object[] { inner });

            return newInst;
        }

        private static object ReadWithPresets(Dictionary<string, object> presets,
                                              JsonReader reader,
                                              Type objectType,
                                              JsonSerializer serializer)
        {
            //If the first token is a string
            if (reader.TokenType == JsonToken.String)
            {
                //Read it
                var text = (string)reader.Value;

                //And check if it is a preset name
                if (text.Length > 1 && text[0] == '$')
                {
                    var preset = text.Substring(1);

                    //And for escaping
                    if (text[1] != '$')
                    {
                        //If it is, read the preset into obj.
                        if (!presets.ContainsKey(preset))
                            throw new Exception("Unfound preset.");

                        return presets[preset];
                    }
                    else
                    {
                        //Otherwise return the escaped string.
                        return preset;
                    }
                }
                //Otherwise return the string.
                else
                {
                    return text;
                }
            }
            //If the first token is an object start
            else if(reader.TokenType == JsonToken.StartObject)
            {
                //Deserialize the object into a JObject
                var obj = serializer.Deserialize<JObject>(reader);

                //Check for empty objects
                if (obj.Count != 0)
                {
                    //Use the first property to determine keywords / special cases
                    var firstProp = obj.First as JProperty;

                    //Use the given preset as a base if the base keyword is found
                    if (firstProp.Name == BaseKeyword)
                    {
                        var key = (string)firstProp.Value;
                        if (!presets.ContainsKey(key))
                            throw new Exception("Unfound preset.");

                        JObject newObj;

                        //Handle non-object type errors
                        try
                        {
                            newObj = JObject.FromObject(presets[key], serializer);
                        }
                        catch(ArgumentException)
                        {
                            throw new JsonReaderException("Object cannot use the $base feature of presets since it is of a literal value.");
                        }

                        //Combine objects
                        obj.Remove(BaseKeyword);
                        foreach (var kvp in obj)
                        {
                            if (newObj.ContainsKey(kvp.Key)) newObj[kvp.Key] = kvp.Value;
                            else newObj.Add(kvp.Key, kvp.Value);
                        }

                        //Return the new object
                        return newObj.ToObject(objectType, serializer);

                        //(swap! owo)
                    }
                }

                //If control falls through, use the given item as a literal value.
                return obj.ToObject(objectType, serializer);
            }
            //Return the literal value
            else
            {
                return serializer.Deserialize(reader, objectType);
            }
        }
    }
}