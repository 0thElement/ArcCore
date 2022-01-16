using Newtonsoft.Json;
using ArcCore.UI.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ArcCore.Serialization
{
    public static class JsonUserInput
    {
        public static readonly DifficultyGroup Past =
            new DifficultyGroup
            {
                Color = ColorExtensions.FromHexcode("000000"),
                Name = "past",
                Precedence = 0
            };

        public static readonly DifficultyGroup Present =
            new DifficultyGroup
            {
                Color = ColorExtensions.FromHexcode("000000"),
                Name = "present",
                Precedence = 100
            };

        public static readonly DifficultyGroup Future =
            new DifficultyGroup
            {
                Color = ColorExtensions.FromHexcode("000000"),
                Name = "future",
                Precedence = 200
            };

        public static readonly DifficultyGroup Beyond =
            new DifficultyGroup
            {
                Color = ColorExtensions.FromHexcode("000000"),
                Name = "beyond",
                Precedence = 300
            };

        private static JToken DefaultReadJToken(JsonReader reader)
        {
            var serializer = new JsonSerializer();
            var obj = serializer.Deserialize<JToken>(reader);
            return obj;
        }

        public static DifficultyGroup ReadDifficultyGroupJson(JsonReader reader)
        {
            var obj = DefaultReadJToken(reader);

            if (obj.Type == JTokenType.String)
            {
                var preset = (string)obj;
                switch (preset)
                {
                    case "past":
                        return Past;
                    case "present":
                        return Present;
                    case "future":
                        return Future;
                    case "beyond":
                        return Beyond;
                    default:
                        throw new JsonReaderException($"Invalid difficulty group '{preset}'.");
                }
            }

            return new DifficultyGroup
            {
                Color = ColorExtensions.FromHexcode((string)obj["color"]),
                Name = (string)obj["name"],
                Precedence = (int)obj["precedence"]
            };
        }

        public static Difficulty ReadDifficultyJson(JsonReader reader)
        {
            var obj = DefaultReadJToken(reader);
            var name = (string)obj;
            var isPlus = false;

            if (name.EndsWith("+"))
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

        public static Chart ReadChartJson(JsonReader reader)
        {
            var jtoken = DefaultReadJToken(reader);
            if (!(jtoken is JObject obj))
                throw new JsonReaderException("Expected an object.");

            var chart = new Chart
            {
                DifficultyGroup = ReadDifficultyGroupJson(reader),
                Difficulty = ReadDifficultyJson(reader),

                SongPath = obj.TryGet<string>("song_path")?.AsFileReference() ?? "base.ogg",
                ImagePath = obj.TryGet<string>("image_path")?.AsFileReference() ?? "base.jpg",

                Name = obj.Get<string>("name"),

                Artist = obj.Get<string>("artist"),

                Illustrator = obj.TryGet<string>("illustrator") ?? "",
                Charter = obj.TryGet<string>("charter") ?? "",

                Background = obj.Get<string>("background")?.AsFileReference(),
                Style = obj.Get<Style>("style"),

                ChartPath = obj.TryGet<string>("chart_path")?.AsFileReference()
            };

            chart.NameRomanized = obj.TryGet<string>("name_romanized") ?? chart.Name;
            chart.ArtistRomanized = obj.TryGet<string>("artist_romanized") ?? chart.Artist;

            if (obj.TryGet<string>("chart_path") is var chartPath && chartPath != null)
            {
                chart.ChartPath = chartPath;
            }
            else
            {
                if (chart.DifficultyGroup == Past)
                    chart.ChartPath = "0.arc";
                else if (chart.DifficultyGroup == Present)
                    chart.ChartPath = "1.arc";
                else if (chart.DifficultyGroup == Future)
                    chart.ChartPath = "2.arc";
                else if (chart.DifficultyGroup == Beyond)
                    chart.ChartPath = "3.arc";
            }

            return chart;
        }

        public static Level ReadLevelJson(JsonReader reader)
        {
            var charts = new List<Chart>();
            if (!(DefaultReadJToken(reader) is JArray chartsJson))
                throw new JsonReaderException("Expected an array.");

            foreach (var chartJson in chartsJson)
            {
                var chartReader = chartJson.CreateReader();
                charts.Add(ReadChartJson(chartReader));
            }

            return new Level
            {
                Charts = charts.ToArray()
            };
        }

        public static Pack ReadPackJson(JsonReader reader)
        {
            var jToken = DefaultReadJToken(reader);
            if (!(jToken is JObject obj))
                throw new JsonReaderException("Expected an object.");

            var pack = new Pack
            {
                ImagePath = obj.TryGet<string>("image_path"),
                Name = obj.TryGet<string>("name"),
            };

            pack.NameRomanized = obj.TryGet<string>("name_romanized") ?? pack.Name;

            return pack;
        }
    }
}