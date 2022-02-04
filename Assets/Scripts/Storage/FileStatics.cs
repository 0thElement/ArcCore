using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArcCore.Storage.Data;

namespace ArcCore.Storage
{
    public static class FileStatics
    {
        public const string 
            Database = "arccore.db",
            FileStorage = "storage",
            SettingsJson = "__settings.json",
            Level = "level",
            Pack = "pack",
            Temp = "__temp";

        public static HashSet<string> SupportedFileExtensions => new HashSet<string>
        {
            ".arc",
            ".png",
            ".jpg",
            ".jpeg",
            ".ogg",
            ".mp3"
        };

#if UNITY_EDITOR
        public static readonly string RootPath = Path.Combine(Application.dataPath, ".imported");
#else
        public static readonly string RootPath = Application.persistentDataPath;
#endif

        public static readonly string
            DatabasePath = Path.Combine(RootPath, Database),
            FileStoragePath = Path.Combine(RootPath, FileStorage),
            TempPath = Application.temporaryCachePath;
    }
}