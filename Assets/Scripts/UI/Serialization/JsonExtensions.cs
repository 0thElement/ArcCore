using Newtonsoft.Json;
using ArcCore.UI.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ArcCore.Serialization
{
    public static class JsonExtensions
    {
        public static Dictionary<string, DifficultyGroup> DifficultyGroupPresets
            => new Dictionary<string, DifficultyGroup>
            {
                {
                    "past", 
                    new DifficultyGroup
                    {
                        Color = ColorExtensions.FromHexcode("000000"),
                        Name = "past",
                        Precedence = 0
                    }
                },
                {
                    "present",
                    new DifficultyGroup
                    {
                        Color = ColorExtensions.FromHexcode("000000"),
                        Name = "present",
                        Precedence = 100
                    }
                },
                {
                    "future",
                    new DifficultyGroup
                    {
                        Color = ColorExtensions.FromHexcode("000000"),
                        Name = "future",
                        Precedence = 200
                    }
                },
                {
                    "beyond",
                    new DifficultyGroup
                    {
                        Color = ColorExtensions.FromHexcode("000000"),
                        Name = "beyond",
                        Precedence = 300
                    }
                }
            };

        private static JToken DefaultReadJToken(JsonReader reader)
        {
            var serializer = new JsonSerializer();
            var obj = serializer.Deserialize<JToken>(reader);
            return obj;
        }

        public static DifficultyGroup ReadDifficultyGroup(JsonReader reader)
        {
            var obj = DefaultReadJToken(reader);

            if (obj.Type == JTokenType.String)
                return DifficultyGroupPresets[obj.ToObject<string>()];

            return new DifficultyGroup
            {
                Color       = ColorExtensions.FromHexcode((string)obj["color"]),
                Name        = (string)obj["name"],
                Precedence  = (int)obj["precedence"]
            };
        }

        public static Difficulty ReadDifficulty(JsonReader reader)
        {
            var obj = DefaultReadJToken(reader);
            var name = (string)obj;
            var isPlus = false;

            if(name.EndsWith("+"))
            {
                name = name.Substring(0, name.Length - 1);
                isPlus = true;
            }

            return new Difficulty
            {
                Name = name,
                IsPlus = isPlus,
            };
        }
    }
}