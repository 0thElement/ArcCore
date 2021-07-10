using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcCore.Serialization
{
    public static class FileStatics
    {
        public const string Globals = "globals";
        public const string Charts = "charts";
        public const string Packs = "packs";
        public const string Temp = "__temp";

        public const string MapJson = "__map.json";
        public const string SettingsJson = "__settings.json";

        public static string GetPersistentSubdir(string path)
            => Path.Combine(Application.persistentDataPath, path);

        public static readonly string GlobalsPath = GetPersistentSubdir(Globals);
        public static readonly string ChartsPath = GetPersistentSubdir(Charts);
        public static readonly string PacksPath = GetPersistentSubdir(Packs);
        public static readonly string TempPath = GetPersistentSubdir(Temp);
        public static readonly string SettingsJsonPath = GetPersistentSubdir(SettingsJsonPath);

        public static readonly string GlobalsMapPath = Path.Combine(GlobalsPath, MapJson);
        public static readonly string ChartsMapPath = Path.Combine(ChartsPath, MapJson);
        public static readonly string PacksMapPath = Path.Combine(PacksPath, MapJson);

        public const string GlobalsMapBase = @"{""light.png"":1,""conf.png"":1}";
        public static Dictionary<string, int> GlobalsMapBaseSerialized
            => new Dictionary<string, int>
            {
            { "light.png", 1 },
            { "conf.png", 1 }
            };

        public const string ChartsMapBase = @"";
        public static Dictionary<string, LevelInfoInternal> ChartsMapBaseSerialized
            => new Dictionary<string, LevelInfoInternal> { };

        public const string PacksMapBase = @"";
        public static Dictionary<string, PackInfo> PacksMapBaseSerialized
            => new Dictionary<string, PackInfo> { };

        public static bool IsValidGlobalName(string s)
            => s.All(ch => char.IsLetterOrDigit(ch) || ch != '.');
    }
}