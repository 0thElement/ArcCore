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

        public const string 
            Globals = "globals", 
            Locals = "locals", 
            Packs = "packs", 
            Levels = "levels", 
            Partners = "partners",
            Temp = "__temp";

        public const string 
            MapJson = "__map.json",
            SettingsJson = "__settings.json",
            ListJson = "__list.json",
            UserSettingsJson = "__user_settings.json",
            GameSettingsJson = "__game_settings.json";

        public static HashSet<string> SupportedFileExtensions => new HashSet<string>
        {
            "arc",
            "png",
            "jpg",
            "jpeg",
            "ogg",
            "mp3"
        };

        public static readonly string
            GlobalsPath = Path.Combine(Application.persistentDataPath, Globals),
            LocalsPath = Path.Combine(Application.persistentDataPath, Locals),
            LevelsPath = Path.Combine(Application.persistentDataPath, Locals, Levels),
            PacksPath = Path.Combine(Application.persistentDataPath, Locals, Packs),
            TempPath = Application.temporaryCachePath;

        public static readonly string
            GlobalsMapPath = Path.Combine(GlobalsPath, MapJson),
            LevelsListPath = Path.Combine(LevelsPath, ListJson),
            PacksListPath = Path.Combine(LevelsPath, ListJson),
            UserSettingsPath = Path.Combine(GlobalsPath, UserSettingsJson),
            GameSettingsPath = Path.Combine(GlobalsPath, GameSettingsJson);

        public const string GlobalsMapDefault = @"{""light.png"":1,""conf.png"":1}";
        public static Dictionary<string, int> GlobalsMapDefaultSerialized
            => new Dictionary<string, int>
            {
                { "light.png", 1 },
                { "conf.png", 1 }
            };

        public const string LevelsDefault = @"";
        public static List<Level> LevelsDefaultSerialized
            => new List<Level> {};
            
        public const string PacksDefault = @"";
        public static List<Pack> PacksDefaultSerialized
            => new List<Pack> { };

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

        public static string GetStorageDir(this ArccoreInfoType type)
        {
            switch(type)
            {
                case ArccoreInfoType.Level:
                    return LevelsPath;
                case ArccoreInfoType.Pack:
                    return PacksPath;
            }

            throw new NotImplementedException();
        }

        public static string GetStoragePath(this IArccoreInfo info)
            => Path.Combine(info.Type().GetStorageDir(), info.Id.ToString());
    }
}