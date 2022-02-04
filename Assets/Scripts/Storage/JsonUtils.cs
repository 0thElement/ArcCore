using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArcCore.Storage
{
    public static class JsonUtils
    {
        public static T TryGet<T>(this JObject obj, string name) where T : class
        {
            if (obj.ContainsKey(name))
                return obj[name].ToObject<T>();

            return null;
        }
        public static T? TryGetStruct<T>(this JObject obj, string name) where T : struct
        {
            if (obj.ContainsKey(name))
                return obj[name].ToObject<T>();

            return null;
        }
        public static T Get<T>(this JObject obj, string name)
        {
            if (obj is JObject jobj && jobj.ContainsKey(name))
                return obj[name].ToObject<T>();

            throw new JsonReaderException($"Expected a property named '{name}'.");
        }

        public static JsonReader ExtractProperty(JObject obj, string property)
        {
            return obj[property].CreateReader();
        }
    }
}