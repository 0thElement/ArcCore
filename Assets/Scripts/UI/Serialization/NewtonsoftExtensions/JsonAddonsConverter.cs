using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [Obsolete]
    public sealed class JsonAddonsConverter : JsonConverter
    {
        private readonly Dictionary<Type, Dictionary<string, object>> storedTypePresets = new Dictionary<Type, Dictionary<string, object>>();
        private readonly Dictionary<Type, Dictionary<string, object>> storedTypeVariables = new Dictionary<Type, Dictionary<string, object>>();
        
        private const string BaseKeyword   = "#base";
        private const string AssignKeyword = "#assign";
        private const string ValueKeyword  = "#value";

        public void ResetVariables()
        {
            storedTypeVariables.Clear();
        }

        [Flags]
        public enum Keywords
        {
            Base = 1,
            Assign = 2,
            Val = 4,
        }

        private bool IsPresetType(Type objectType)
        {
            return objectType.IsDefined(typeof(JsonHasPresetsAttribute)) || objectType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJsonPresetSpecialization<>));
        }

        public override bool CanConvert(Type objectType)
            => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Remove this converter
            int i = 0;
            while (serializer.Converters[i].GetType() != GetType()) i++;
            serializer.Converters.RemoveAt(i);

            //Get the value type of this item (accounting for specialization)
            GetValueType(objectType, out var valueType);

            //Get corresponding presets
            if (IsPresetType(objectType) && !storedTypePresets.ContainsKey(objectType))
            {
                var newPresets = GetPresets(objectType, valueType);
                storedTypePresets.Add(objectType, newPresets);
            }

            //Read value
            var value = ReadInternal(serializer.Deserialize<JToken>(reader), valueType, objectType, serializer);
            value = MakeObject(value, objectType, valueType);

            //Add back this converter
            serializer.Converters.Insert(i, this);

            //Return read value as real type
            return value;
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
        private static Dictionary<string, object> GetPresets(Type declaringType, Type valueType)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            var candidates =
                declaringType
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(i => i.FieldType == valueType)
                .Where(i => i.IsDefined(typeof(JsonPresetAttribute)))
                .Select(i => (
                    (MemberInfo)i, 
                    i.GetValue(null), 
                    i.GetCustomAttribute<JsonPresetAttribute>()
                ))
                .Concat(
                    declaringType
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(i => i.PropertyType == valueType && i.CanRead)
                    .Where(i => i.IsDefined(typeof(JsonPresetAttribute)))
                    .Select(i => (
                        (MemberInfo)i, 
                        i.GetValue(null), 
                        i.GetCustomAttribute<JsonPresetAttribute>()
                    ))
                );

            foreach (var (member, value, attribute) in candidates)
            {
                var name = attribute.NameOverride ?? member.Name;
                dict.Add(NormalizeName(name), value);
            }

            return dict;
        }
        private static string NormalizeName(string name)
        {
            var n1 = char.ToLower(name[0]);
            return n1 + name.Substring(1);
        }

        /// <summary>
        /// Get the value type of a given possibly-specialized type.
        /// </summary>
        private static void GetValueType(in Type declaringType, out Type valueType)
        {
            valueType = declaringType;

            var specialization = declaringType.GetInterfaces().SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() ==  typeof(IJsonPresetSpecialization<>));
            if (specialization != null)
                valueType = specialization.GenericTypeArguments[0];
        }

        /// <summary>
        /// Create a new object with the given value of the given declaring type and value type.
        /// If <paramref name="declaredType"/> and <paramref name="valueType"/> are the same, the output is <paramref name="inner"/>,
        /// otherwise it is an instance of <paramref name="declaredType"/> which has the value of <paramref name="inner"/>.
        /// </summary>
        /// <param name="inner">The value of the resulting object</param>
        /// <param name="declaredType">The actual type of the resulting object</param>
        /// <param name="valueType">The type of the underlying value of the resulting object</param>
        /// <returns></returns>
        private static object MakeObject(object inner, Type declaredType, Type valueType)
        {
            if (declaredType == valueType) 
                return inner;

            var setter = declaredType.GetProperties()
                                   .First(p => p.PropertyType == valueType && p.Name == nameof(IJsonPresetSpecialization<object>.Value))
                                   .SetMethod;

            var newInst = Activator.CreateInstance(declaredType);
            setter.Invoke(newInst, new object[] { inner });

            return newInst;
        }

        /// <summary>
        /// Read the given object internally (a huge fucken mess).
        /// </summary>
        private object ReadInternal(JToken readValue, Type returnType, Type objectType, JsonSerializer serializer)
        {
            //If the first token is a string
            if (readValue.Type == JTokenType.String)
            {
                //Read it
                var text = (string)readValue;

                //And check if it is a preset name
                if (text.Length > 1)
                {
                    var rtext = text.Substring(1);

                    if (text[0] == '$')
                    {
                        //And for escaping
                        if (text[1] != '$')
                        {
                            //If it is, read the preset into obj.
                            if (storedTypePresets.ContainsKey(objectType) && storedTypePresets[objectType].ContainsKey(rtext))
                                return storedTypePresets[objectType][rtext];

                            else throw new JsonReaderException($"The given preset name or variable does not exist.");
                        }
                        else return rtext;
                    }
                    //Or a variable name
                    else if (text[0] == '&')
                    {
                        //And for escaping
                        if (text[1] != '&')
                        {
                            //If it is, read the preset into obj.
                            if (storedTypeVariables.ContainsKey(objectType) && storedTypeVariables[objectType].ContainsKey(rtext))
                                return storedTypeVariables[objectType][rtext];

                            else throw new JsonReaderException($"The given preset name or variable does not exist.");
                        }
                        else return rtext;
                    }

                }

                //Otherwise return the string
                return text;
            }
            //If the first token is an object start
            else if (readValue.Type == JTokenType.Object)
            {
                //Deserialize the object into a JObject
                JObject rawObj = (JObject)readValue;
                object parsedVal = null;
                string assignTo = null;
                Keywords used = 0;

                //Check for empty objects
                if (rawObj.Count != 0)
                {
                    var props = rawObj.Properties().ToArray();
                    var i = 0;
                    while (true)
                    {
                        //Use the first property to determine keywords / special cases
                        if (i >= props.Length)
                            break;
                        var currentProp = props[i++];

                        //Use the given preset as a base if the base keyword is found
                        if (currentProp.Name == BaseKeyword)
                        {
                            if ((used & (Keywords.Base | Keywords.Val)) != 0)
                                throw new JsonReaderException($"Cannot use {BaseKeyword} more than once or with {ValueKeyword}.");

                            used = used | Keywords.Base;

                            var keyFull = (string)currentProp.Value;
                            bool keyIsPreset = keyFull[0] == '$';
                            bool keyIsVar = keyFull[0] == '&';

                            if (!keyIsPreset && !keyIsVar)
                                throw new JsonReaderException($"Argument supplied to {BaseKeyword} must be either a preset ('$...'), or a variable ('&...').");

                            var key = keyFull.Substring(1);
                            object value;

                            if (keyIsPreset && storedTypePresets[returnType].ContainsKey(key))
                                value = storedTypePresets[returnType][key];
                            else if (keyIsVar && storedTypeVariables[returnType].ContainsKey(key))
                                value = storedTypeVariables[returnType][key];
                            else throw new JsonReaderException($"The given preset or variable does not exist.");

                            JObject newObj;

                            //Handle non-object type errors
                            try
                            {
                                newObj = JObject.FromObject(value, serializer);
                            }
                            catch (ArgumentException)
                            {
                                throw new JsonReaderException($"The given item cannot use {BaseKeyword} since it is serialized as a literal. This may be programmer error.");
                            }

                            //Combine objects (excluding keywords)
                            foreach (var kvp in rawObj)
                            {
                                if (kvp.Key == BaseKeyword || kvp.Key == AssignKeyword) continue;

                                if (newObj.ContainsKey(kvp.Key)) newObj[kvp.Key] = kvp.Value;
                                else newObj.Add(kvp.Key, kvp.Value);
                            }

                            //Set the new object for return
                            parsedVal = newObj.ToObject(returnType, serializer);
                        }
                        else if (currentProp.Name == AssignKeyword)
                        {
                            if ((used & Keywords.Assign) != 0)
                                throw new JsonReaderException($"Cannot use {AssignKeyword} more than once.");

                            used = used | Keywords.Assign;

                            assignTo = (string)currentProp.Value;

                            if (assignTo.Length <= 1)
                                throw new JsonReaderException($"The argument of {AssignKeyword} must be more than one character.");
                            if (assignTo[0] != '&') 
                                throw new JsonReaderException($"The argument of {AssignKeyword} must be a variable ('&...').");
                        }
                        else if(currentProp.Name == ValueKeyword)
                        {
                            if ((used & (Keywords.Val | Keywords.Base)) != 0)
                                throw new JsonReaderException($"Cannot use {ValueKeyword} more than once or with {BaseKeyword}.");

                            used = used | Keywords.Val;

                            parsedVal = ReadInternal(currentProp.Value, returnType, objectType, serializer);
                        }
                        else break;
                    }
                }

                //If control falls through, use the given item as a literal value.
                object ret;

                //Special val
                if (parsedVal != null)
                {
                    //Val is compound
                    if (parsedVal is JToken jobj)
                        ret = jobj.ToObject(returnType, serializer);
                    //Val is singular
                    else 
                        ret = parsedVal;
                }
                //No special val
                else
                {
                    ret = rawObj.ToObject(returnType, serializer);
                }

                //Assign if needed
                if (assignTo != null)
                {
                    if (!storedTypeVariables.ContainsKey(objectType))
                        storedTypeVariables.Add(objectType, new Dictionary<string, object>());
                    storedTypeVariables[objectType].Add(assignTo.Substring(1), ret);
                }

                //Return
                return ret;
            }
            //Return the literal value
            else
            {
                return readValue.ToObject(returnType, serializer);
            }
        }
    }
}