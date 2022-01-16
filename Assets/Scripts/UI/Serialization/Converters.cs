using Newtonsoft.Json;

namespace ArcCore.Serialization
{
    internal static class Converters
    {
        internal static JsonConverter[] Settings => new JsonConverter[]
        {
            new JsonColorConverter()
        };
        internal static JsonConverter[] Levels => Settings;
    }
}