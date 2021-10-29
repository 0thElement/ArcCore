using Newtonsoft.Json;

namespace ArcCore.Serialization
{
    public static class Converters
    {
        public static JsonConverter[] Levels
            => new JsonConverter[]
            {
                new JsonPresetConverter<StyleScheme>(StyleScheme.Presets),
                new JsonPresetConverter<DifficultyType>(DifficultyType.Presets),
                new JsonColorConverter()
            };

        public static JsonConverter[] Settings
            => new JsonConverter[]
            {
                new JsonColorConverter()
            };
    }
}