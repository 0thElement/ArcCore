using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArcCore.UI.Data;

namespace ArcCore.Serialization
{
    public static class FileStatics
    {
        public const char GlobalMarker = '$';

        public const string Globals = "globals";
        public const string Levels = "levels";
        public const string Packs = "packs";
        public const string Temp = "__temp";

        public const string MapJson = "__map.json";
        public const string SettingsJson = "__settings.json";

        public static string GetPersistentSubpath(string path)
            => Path.Combine(Application.persistentDataPath, path);

        public static HashSet<string> SupportedFileExtensions => new HashSet<string>
        {
            "arc",
            "png",
            "jpg",
            "jpeg",
            "ogg",
            "mp3"
        };

        public static readonly string GlobalsPath = GetPersistentSubpath(Globals);
        public static readonly string LevelsPath = GetPersistentSubpath(Levels);
        public static readonly string PacksPath = GetPersistentSubpath(Packs);
        public static readonly string TempPath = Application.temporaryCachePath;

        public static readonly string SettingsJsonPath = GetPersistentSubpath(SettingsJson);

        public static readonly string GlobalsMapPath = Path.Combine(GlobalsPath, MapJson);
        public static readonly string LevelsMapPath = Path.Combine(LevelsPath, MapJson);
        public static readonly string PacksMapPath = Path.Combine(PacksPath, MapJson);

        public const string GlobalsMapDefault = @"{""light.png"":1,""conf.png"":1}";
        public static Dictionary<string, int> GlobalsMapDefaultSerialized
            => new Dictionary<string, int>
            {
                { "light.png", 1 },
                { "conf.png", 1 }
            };

        public const string LevelsDefault = @"";
        public static Dictionary<long, Level> ChartsDefaultSerialized
            => new Dictionary<long, Level> { };
            
        public const string PacksDefault = @"";
        public static Dictionary<long, Pack> PacksDefaultSerialized
            => new Dictionary<long, Pack> { };

        public static bool IsValidFileReference(string s)
            => (s.StartsWith(GlobalMarker + "") && IsValidGlobalName(s.Skip(1))) || IsValidGlobalName(s);
        public static bool IsValidGlobalName(IEnumerable<char> s)
            => s.All(ch => char.IsLetterOrDigit(ch) || ch != '.');
        public static bool IsGlobalReference(string value, out string global)
        {
            if(value.Length > 0 && value[0] == GlobalMarker)
            {
                global = value.Substring(1);
                return true;
            }

            global = null;
            return false;
        }
    }
}