using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using ArcCore.UI.Data;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace ArcCore.Serialization
{
    [Flags]
    public enum ArccoreInfoTypes : byte
    {
        Level = 1 << 0,
        Pack = 1 << 1,
        Partner = 1 << 2,

        All = Level | Pack | Partner
    }

    internal static class Converters
    {
        internal static JsonConverter[] Settings => new JsonConverter[]
        {
            new JsonColorConverter()
        };
        internal static JsonConverter[] Levels => Settings;
    }

    public static class FileManagement
    {
        public static string currentChartDirectory;
        public static string GetRealPathFromUserInput(string input)
            => Path.Combine(input[0] == FileStatics.GlobalMarker ? FileStatics.GlobalsPath : currentChartDirectory, input);

        public static Dictionary<string, int> globalsMap;
        public static Dictionary<long, Level> levels;
        public static Dictionary<long, Pack> packs;

        public static bool IsValidFilePath(string path)
        {
            bool isValid = Uri.TryCreate(path, UriKind.Absolute, out Uri result);
            return isValid && result != null && result.IsLoopback;
        }

        public static void OnAppStart()
        {
            // SETUP FILES IF NON-EXISTENT //

            // ...\__settings.json
            if (!File.Exists(FileStatics.SettingsJsonPath))
            {
                GameSettings.Instance = GameSettings.Default;

                using (var fs = File.CreateText(FileStatics.SettingsJsonPath))
                {
                    var settings = new JsonSerializerSettings { Converters = Converters.Settings };
                    var serializer = JsonSerializer.Create(settings);
                    var writer = new JsonTextWriter(fs);

                    serializer.Serialize(writer, GameSettings.Instance);
                }
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.SettingsJsonPath))
                {
                    var settings = new JsonSerializerSettings { Converters = Converters.Settings };
                    var serializer = JsonSerializer.Create(settings);
                    var reader = new JsonTextReader(fs);

                    GameSettings.Instance = serializer.Deserialize<GameSettings>(reader);
                }
            }

            // TEMPORARY: we don't have a settings menu yet
            GameSettings.Instance = GameSettings.GetDefault();
            GameSettings.FinalizeInstance();

            // ...\globals
            if (!Directory.Exists(FileStatics.GlobalsPath))
            {
                Directory.CreateDirectory(FileStatics.GlobalsPath);
                using (var fs = File.CreateText(FileStatics.GlobalsMapPath))
                {
                    fs.Write(FileStatics.GlobalsMapDefault);
                }

                globalsMap = FileStatics.GlobalsMapDefaultSerialized;

                //copy in light.png and conflict.png
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.GlobalsMapPath))
                {
                    var serializer = JsonSerializer.Create();
                    var reader = new JsonTextReader(fs);

                    globalsMap = serializer.Deserialize<Dictionary<string, int>>(reader);
                }
            }

            // ...\levels
            if (!Directory.Exists(FileStatics.LevelsPath))
            {
                Directory.CreateDirectory(FileStatics.LevelsPath);
                using (var fs = File.CreateText(FileStatics.LevelsMapPath))
                {
                    fs.Write(FileStatics.LevelsDefault);
                }

                levels = FileStatics.ChartsDefaultSerialized;

                //copy in pre-built charts
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.LevelsMapPath))
                {
                    var settings = new JsonSerializerSettings
                    {
                        Converters = Converters.Levels
                    };
                    var serializer = JsonSerializer.Create(settings);
                    var reader = new JsonTextReader(fs);

                    levels = serializer.Deserialize<Dictionary<long, Level>>(reader);
                }
            }

            //packs path
            if (!Directory.Exists(FileStatics.PacksPath))
            {
                Directory.CreateDirectory(FileStatics.PacksPath);
                using (var fs = File.CreateText(FileStatics.PacksMapPath))
                {
                    fs.Write(FileStatics.PacksDefault);
                }

                packs = FileStatics.PacksDefaultSerialized;

                //copy in pre-built packs
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.PacksMapPath))
                {
                    var serializer = JsonSerializer.Create();
                    var reader = new JsonTextReader(fs);

                    packs = serializer.Deserialize<Dictionary<long, Pack>>(reader);
                }
            }
        }

        public static void UpdateGlobalMap()
        {
            using (var fs = File.Open(FileStatics.GlobalsMapPath, FileMode.Truncate, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
            {
                var serializer = new JsonSerializer();
                var writer = new JsonTextWriter(sw);

                serializer.Serialize(writer, globalsMap);
            }
        }

        public static void ImportGlobal(FileInfo info, List<string> tracker)
        {
            string destination = Path.Combine(FileStatics.GlobalsPath, info.Name);

            if (File.Exists(destination))
            {
                //Already imported
                globalsMap[info.Name]++;
            }
            else
            {
                //New import
                File.Copy(info.FullName, destination);
                globalsMap.Add(info.Name, 1);
            }

            tracker.Add(info.Name);
        }

        public static void DeleteGlobal(string filename)
        {
            globalsMap[filename]--;

            //no more references left
            if (globalsMap[filename] <= 0)
            {
                string target = Path.Combine(FileStatics.GlobalsPath, filename);
                File.Delete(target);
                globalsMap.Remove(filename);
            }
        }

        public static void UpdateLevelMap()
        {
            using (var fs = File.Open(FileStatics.LevelsMapPath, FileMode.Truncate, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings {Converters = Converters.Levels});
                var writer = new JsonTextWriter(sw);

                serializer.Serialize(writer, levels);
            }
        }

        public static void AddLevelCollection(ZipArchive archive)
        {
            Directory.CreateDirectory(FileStatics.TempPath);
            archive.ExtractToDirectory(FileStatics.TempPath);

            var tempInfo = new DirectoryInfo(Directory.GetDirectories(FileStatics.TempPath)[0]);

            //prevent files
            if (tempInfo.GetFiles().Length != 0)
            {
                Directory.Delete(FileStatics.TempPath);
                throw new Exception("Level collections may not contain anything other than subdirectories.");
            }

            //add all level directories
            foreach (var dir in tempInfo.EnumerateDirectories())
            {
                try
                {
                    ReadSingle(FileStatics.TempPath, false);
                }
                catch (Exception e)
                {
                    Directory.Delete(FileStatics.TempPath, true);
                    throw e;
                }
            }

            //update maps
            UpdateLevelMap();
            UpdateGlobalMap();

            //finalize
            Directory.Delete(FileStatics.TempPath, true);
        }

        public static void AddLevelArchive(ZipArchive archive)
        {
            Directory.CreateDirectory(FileStatics.TempPath);
            archive.ExtractToDirectory(FileStatics.TempPath);

            //try catch to prevent memory leakage
            try
            {
                ReadSingle(Directory.GetDirectories(FileStatics.TempPath)[0]);
            }
            catch (Exception e)
            {
                Directory.Delete(FileStatics.TempPath, true);
                throw e;
            }

            Directory.Delete(FileStatics.TempPath, true);
        }
        

        public static void ImportDirectory(DirectoryInfo dir)
        {
            var (item, filesToCopy, globalsToCopy) = ReadItem(dir, ArccoreInfoTypes.All);

            foreach(var global in globalsToCopy)
            {
                if(!globalsMap.ContainsKey())
            }
        }


        public static (IArccoreInfo item, IList<FileInfo> filesToCopy, IList<FileInfo> globalsToCopy) ReadItem(DirectoryInfo sourceDirectory, ArccoreInfoTypes allowedTypes)
        {
            // Find settings file and globals directory
            var settingsFile = sourceDirectory.EnumerateFiles().FirstOrDefault(f => f.Name == FileStatics.SettingsJson) 
                               ?? throw new JsonReaderException("Directory must contain a '__settings.json'.");
            var files = sourceDirectory.EnumerateFiles().ToDictionary(f => f.Name, f => f);

            // Verify other files are valid
            if(sourceDirectory.EnumerateFiles().FirstOrDefault(
                f => f.Name == FileStatics.SettingsJson || FileStatics.SupportedFileExtensions.Contains(f.Extension)
                ) is var invalidFile && invalidFile != null)
            {
                throw new JsonReaderException($"Invalid file: {invalidFile.Name}");
            }

            // Read data from settings file
            JObject jObj;

            using(var reader = settingsFile.OpenText())
            {
                var jReader = new JsonTextReader(reader);
                var jSerializer = JsonSerializer.CreateDefault();
                jObj = jSerializer.Deserialize<JObject>(jReader);
            }

            var type = jObj.Get<string>("type");
            var data = jObj.Get<JObject>("data");

            // Read correct info
            IArccoreInfo info;
            IList<DirectoryInfo> useDirectories;

            if (type == "level")
            {
                if (!allowedTypes.HasFlag(ArccoreInfoTypes.Level))
                    throw new JsonReaderException("Levels are dissallowed in the current context.");

                (info, useDirectories) = ReadLevel(sourceDirectory, data.CreateReader());
            }
            else if (type == "pack")
            {
                if (!allowedTypes.HasFlag(ArccoreInfoTypes.Pack))
                    throw new JsonReaderException("Packs are dissallowed in the current context.");

                (info, useDirectories) = ReadPack(sourceDirectory);
            }
            else if (type == "partner")
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new JsonReaderException($"Unknown data type '{type}'.");
            }

            // Get globals
            var globals = useDirectories.SelectMany(useDir => useDir.EnumerateDirectories()
                                                                    .Where(d => d.Name == FileStatics.Globals))
                                        .SelectMany(d => d.GetFiles())
                                        .ToDictionary(f => f.Name, f => f);

            // Verify references
            var filesToCopy = new List<FileInfo>();
            var globalsToCopy = new List<FileInfo>();

            foreach (var r in info.GetReferences())
            {
                if(!FileStatics.IsValidFileReference(r))
                {
                    throw new JsonReaderException($"Invalid file reference: '{r}'");
                }

                if (FileStatics.IsGlobalReference(r, out var glb))
                {
                    if (globals?.ContainsKey(glb) is bool b && b)
                    {
                        globalsToCopy.Add(globals[glb]);
                    }
                    else
                    {
                        throw new JsonReaderException($"Referenced global '{glb}' does not exist.");
                    }
                }
                else
                {
                    if (files.ContainsKey(r))
                    {
                        filesToCopy.Add(files[r]);
                    }
                    else
                    {
                        throw new JsonReaderException($"Referenced file '{r}' does not exist.");
                    }
                }
            }

            // Setup globals
            info.ImportedGlobals = globalsToCopy.Select(g => g.Name).ToArray();

            // Return!
            return (info, filesToCopy, globalsToCopy);
        }

        public static (Level level, IList<DirectoryInfo> useDirectories) ReadLevel(DirectoryInfo sourceDirectory, JsonReader settingsReader)
        {
            //check globals
            if(sourceDirectory.EnumerateFiles().Any(d => d.Name != FileStatics.Globals))
            {
                throw new JsonReaderException("Level folders cannot have subdirectories other than a possible 'globals' folder.");
            }

            //return level
            return (
                JsonUserInput.ReadLevelJson(settingsReader),
                new DirectoryInfo[] { sourceDirectory }
            );
        }

        public static (Pack pack, IList<DirectoryInfo> useDirectories) ReadPack(DirectoryInfo sourceDirectory)
        {
            //check folders
            if(sourceDirectory.EnumerateFiles().Any(f => f.Name != FileStatics.SettingsJson))
            {
                throw new JsonReaderException("Pack folders cannot have files other than '__settings.json'.");
            }

            var subdirs = sourceDirectory.GetDirectories();
            var useDirs = subdirs.ToList();
            useDirs.Add(sourceDirectory);

            return (
                new Pack
                {
                    Levels = subdirs
                             .Where(d => d.Name != FileStatics.Globals)
                             .Select(d => (Level)ReadItem(d, ArccoreInfoTypes.Level).item)
                             .ToArray()
                },
                useDirs
            );
        }



        /// WARNING: might cause errors if files are tampered with or there are bugs!
        public static void DeleteSingle(long id)
        {
            var single = levels[id];

            //delete globals
            foreach (var glb in single.ImportedGlobals)
            {
                DeleteGlobal(glb);
            }

            //delete level data
            Directory.Delete(Path.Combine(FileStatics.LevelsPath, single.Directory), true);

            //delete from maps and update
            levels.Remove(id);

            UpdateLevelMap();
            UpdateGlobalMap();
        }
    }
}