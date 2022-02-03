﻿using Newtonsoft.Json;
using ArcCore.Storage.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ArcCore.Utilities;

namespace ArcCore.Storage
{
    public static class JsonUserInput
    {
        //TODO: De-hardcode the hexcodes
        public static DifficultyGroup Past
            => new DifficultyGroup
            {
                Color = "#48D4D4".ToColor(),
                Name = "Past",
                Precedence = 0
            };

        public static DifficultyGroup Present
            => new DifficultyGroup
            {
                Color = "#8CE75D".ToColor(),
                Name = "Present",
                Precedence = 100
            };

        public static DifficultyGroup Future
            => new DifficultyGroup
            {
                Color = "#E05CF7".ToColor(),
                Name = "Future",
                Precedence = 200
            };

        public static DifficultyGroup Beyond 
            => new DifficultyGroup 
            { 
                Color = "#E10C0C".ToColor(), 
                Name = "Beyond",
                Precedence = 300 
            };

        public static DifficultyGroup FromPreset(string preset)
        {
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
                return FromPreset(preset);
            }

            return new DifficultyGroup
            {
                Color = ((string)obj["color"]).ToColor(),
                Name = (string)obj["name"],
                Precedence = (int)obj["precedence"]
            };
        }

        public static Difficulty ReadDifficultyJson(JsonReader reader)
        {
            var obj = DefaultReadJToken(reader);
            var name = (string)obj;

            return new Difficulty(name);
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

                SongPath = obj.TryGet<string>("song_path") ?? "base.ogg",
                ImagePath = obj.TryGet<string>("image_path") ?? "base.jpg",

                Name = obj.Get<string>("name"),

                Artist = obj.Get<string>("artist"),

                Illustrator = obj.TryGet<string>("illustrator") ?? "",
                Charter = obj.TryGet<string>("charter") ?? "",

                Background = obj.Get<string>("background"),
                Style = obj.Get<Style>("style"),

                ChartPath = obj.TryGet<string>("chart_path")
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
            var jToken = DefaultReadJToken(reader);
            if (!(jToken is JObject obj))
                throw new JsonReaderException("Expected an object.");

            string pack = obj.TryGet<string>("pack");
            var chartsobj = obj.TryGet<JObject>("charts");

            var charts = new List<Chart>();

            if (!(DefaultReadJToken(chartsobj.CreateReader()) is JArray chartsJson))
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