using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using ArcCore.UI.Data;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEditor.Timeline;

namespace ArcCore.Serialization
{
    public static class FileManagement
    {
        public static string currentChartDirectory;
        public static string GetRealPathFromUserInput(string input)
            => Path.Combine(input[0] == FileStatics.GlobalMarker ? FileStatics.GlobalsPath : currentChartDirectory, input);

        public static Dictionary<string, int> globalsMap;

        public static List<Level> levels;
        public static List<Pack> packs;

        public static bool IsValidFilePath(string path)
        {
            bool isValid = Uri.TryCreate(path, UriKind.Absolute, out Uri result);
            return isValid && result != null && result.IsLoopback;
        }

        public static void OnAppStart()
        {
            // SETUP FILES IF NON-EXISTENT //

            // ...\__user_settings.json
            if (!File.Exists(FileStatics.UserSettingsPath))
            {
                UserSettings.Instance = UserSettings.Default;

                using (var fs = File.CreateText(FileStatics.UserSettingsPath))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings {Converters = Converters.Levels});
                    var writer = new JsonTextWriter(fs);

                    serializer.Serialize(writer, UserSettings.Instance);
                }
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.UserSettingsPath))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings {Converters = Converters.Levels});
                    var reader = new JsonTextReader(fs);

                    UserSettings.Instance = serializer.Deserialize<UserSettings>(reader);
                }
            }

            UserSettings.FinalizeInstance();

            // ...\__game_settings.json
            if (!File.Exists(FileStatics.GameSettingsPath))
            {
                GameSettings.Instance = GameSettings.Default;

                using (var fs = File.CreateText(FileStatics.UserSettingsPath))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings {Converters = Converters.Levels});
                    var writer = new JsonTextWriter(fs);

                    serializer.Serialize(writer, UserSettings.Instance);
                }
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.UserSettingsPath))
                {
                    var serializer = JsonSerializer.Create(new JsonSerializerSettings {Converters = Converters.Levels});
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
                using (var fs = File.CreateText(FileStatics.LevelsListPath))
                {
                    fs.Write(FileStatics.LevelsDefault);
                }

                levels = FileStatics.LevelsDefaultSerialized;

                //copy in pre-built charts
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.LevelsListPath))
                {
                    var serializer = JsonSerializer.Create();
                    var reader = new JsonTextReader(fs);

                    levels = serializer.Deserialize<List<Level>>(reader);
                }
            }

            //packs path
            if (!Directory.Exists(FileStatics.PacksPath))
            {
                Directory.CreateDirectory(FileStatics.PacksPath);
                using (var fs = File.CreateText(FileStatics.PacksListPath))
                {
                    fs.Write(FileStatics.PacksDefault);
                }

                packs = FileStatics.PacksDefaultSerialized;

                //copy in pre-built packs
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.PacksListPath))
                {
                    var serializer = JsonSerializer.Create();
                    var reader = new JsonTextReader(fs);

                    packs = serializer.Deserialize<List<Pack>>(reader);
                }
            }
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
        public static void UpdateLevelList()
        {
            using (var fs = File.Open(FileStatics.LevelsPath, FileMode.Truncate, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
            {
                var serializer = JsonSerializer.Create();
                var writer = new JsonTextWriter(sw);

                serializer.Serialize(writer, levels);
            }
        }
        private static void UpdatePackList()
        {
            using(var fs = File.Open(FileStatics.PacksPath, FileMode.Truncate, FileAccess.Write))
            using(var sw = new StreamWriter(fs))
            {
                var serializer = JsonSerializer.Create();
                var writer = new JsonTextWriter(sw);

                serializer.Serialize(writer, packs);
            }
        }

        public static void AddLevelArchive(ZipArchive archive)
        {
            archive.ExtractToDirectory(FileStatics.TempPath);

            //try catch to prevent memory leakage
            try
            {
                ImportDirectory(new DirectoryInfo(FileStatics.TempPath));
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
            var (item, globalsToCopy, localsToCopy) = ReadItem(dir, ArccoreInfoType.All);

            foreach(var (file, path, name) in globalsToCopy)
            {
                if(!globalsMap.ContainsKey(name))
                {
                    globalsMap.Add(name, 1);
                    file.CopyTo(path);
                }
                else
                {
                    globalsMap[name]++;
                }
            }

            foreach (var (file, path, _) in localsToCopy)
            {
                file.CopyTo(path);
            }
        }


        public static (IArccoreInfo item, 
            List<(FileInfo file, string path, string name)> globalsToCopy, 
            List<(FileInfo file, string path, string name)> localsToCopy) 
            ReadItem(DirectoryInfo sourceDirectory, ArccoreInfoType allowedTypes)
        {
            // Find settings file and globals directory
            var settingsFile = sourceDirectory.EnumerateFiles().FirstOrDefault(f => f.Name == FileStatics.SettingsJson) 
                               ?? throw new JsonReaderException("Directory must contain a '__settings.json'.");

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

            var dataReader = data.CreateReader();

            if (type == "level")
            {
                if (!allowedTypes.HasFlag(ArccoreInfoType.Level))
                    throw new JsonReaderException("Levels are dissallowed in the current context.");

                (info, useDirectories) = ReadLevel(sourceDirectory, dataReader);
            }
            else if (type == "pack")
            {
                if (!allowedTypes.HasFlag(ArccoreInfoType.Pack))
                    throw new JsonReaderException("Packs are dissallowed in the current context.");

                (info, useDirectories) = ReadPack(sourceDirectory, dataReader);
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
            var locals = useDirectories.SelectMany(useDir => useDir.EnumerateFiles().Where(f => f.Name != FileStatics.SettingsJson))
                                       .ToDictionary(f => f.Name, f => f);
            var globals = useDirectories.SelectMany(useDir => useDir.EnumerateDirectories().Where(d => d.Name == FileStatics.Globals))
                                        .SelectMany(d => d.GetFiles())
                                        .ToDictionary(f => f.Name, f => f);

            // Verify references
            var localsToCopy = new List<(FileInfo file, string path, string name)>();
            var globalsToCopy = new List<(FileInfo file, string path, string name)>();
            var nameMap = new Dictionary<string, string>();

            foreach(var r in info.References)
            {
                if(!FileStatics.IsValidFileReference(r))
                {
                    throw new JsonReaderException($"Invalid file reference: '{r}'");
                }

                if (FileStatics.IsGlobalReference(r, out var glb))
                {
                    if (globals?.ContainsKey(glb) is bool b && b)
                    {
                        var path = Path.Combine(FileStatics.GlobalsPath, glb);

                        globalsToCopy.Add((globals[glb], path, glb));
                        nameMap.Add(r, path);
                    }
                    else
                    {
                        throw new JsonReaderException($"Referenced global '{glb}' does not exist.");
                    }
                }
                else
                {
                    if (locals.ContainsKey(r))
                    {
                        var path = Path.Combine(info.GetStoragePath(), r);

                        localsToCopy.Add((locals[r], path, r));
                        nameMap.Add(r, path);
                    }
                    else
                    {
                        throw new JsonReaderException($"Referenced file '{r}' does not exist.");
                    }
                }
            }

            // Modify references
            info.ModifyReferences(s => nameMap[s]);

            // Setup globals
            info.ImportedGlobals = globalsToCopy.Select(g => g.file.Name).ToArray();

            // Return!
            return (info, localsToCopy, globalsToCopy);
        }

        public static (Level level, IList<DirectoryInfo> useDirectories) ReadLevel(DirectoryInfo sourceDirectory, JsonReader settingsReader)
        {
            //check globals
            if(sourceDirectory.EnumerateFiles().Any(d => d.Name != FileStatics.Globals))
            {
                throw new JsonReaderException("Level folders cannot have subdirectories other than a possible 'globals' folder.");
            }

            var level = JsonUserInput.ReadLevelJson(settingsReader);
            level.Id = Settings.MaxLevelId++;

            //return level
            return (
                level,
                new DirectoryInfo[] { sourceDirectory }
            );
        }

        public static (Pack pack, IList<DirectoryInfo> useDirectories) ReadPack(DirectoryInfo sourceDirectory, JsonReader settingsReader)
        {
            //check folders
            if(sourceDirectory.EnumerateFiles().Any(f => f.Name != FileStatics.SettingsJson))
            {
                throw new JsonReaderException("Pack folders cannot have files other than '__settings.json'.");
            }

            var subdirs = sourceDirectory.GetDirectories();
            var useDirs = subdirs.ToList();
            useDirs.Add(sourceDirectory);

            var pack = JsonUserInput.ReadPackJson(settingsReader);
            pack.Id = Settings.MaxPackId++;

            return (
                pack,
                useDirs
            );
        }

        public static void DeletePack(Pack pack)
        {
            //delete globals
            foreach(var glb in pack.ImportedGlobals)
            {
                DeleteGlobal(glb);
            }

            foreach(var glb in levels.Where(l => l.Pack == pack).SelectMany(l => l.ImportedGlobals))
            {
                DeleteGlobal(glb);
            }

            //delete pack data
            Directory.Delete(pack.GetStoragePath(), true);

            //delete from maps and update
            packs.Remove(pack);
            UpdatePackList();

            levels.RemoveAll(l => l.Pack == pack);
            UpdateLevelList();
        }

        /// WARNING: might cause errors if files are tampered with or there are bugs!
        public static void DeleteSingle(Level single)
        {
            //delete globals
            foreach (var glb in single.ImportedGlobals)
            {
                DeleteGlobal(glb);
            }

            //delete level data
            Directory.Delete(single.GetStoragePath(), true);

            //delete from maps and update
            levels.Remove(single);
            UpdateLevelList();
        }
    }
}