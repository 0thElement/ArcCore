using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;

namespace ArcCore.Serialization
{
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
        public static Dictionary<string, LevelInfoInternal> chartsMap;
        public static Dictionary<string, PackInfo> packsMap;

        public static bool IsValidFilePath(string path)
        {
            bool isValid = Uri.TryCreate(path, UriKind.Absolute, out Uri result);
            return isValid && result != null && result.IsLoopback;
        }

        public static void OnAppStart()
        {
            // SETUP FILES IF NON-EXISTENT //

            //settings json
            if (!File.Exists(FileStatics.SettingsJsonPath))
            {
                GameSettings.Instance = GameSettings.GetDefault();
                using (var fs = File.CreateText(FileStatics.SettingsJsonPath))
                {
                    var settings = new JsonSerializerSettings
                    {
                        Converters = Converters.Settings
                    };
                    var serializer = JsonSerializer.Create(settings);
                    var writer = new JsonTextWriter(fs);

                    serializer.Serialize(writer, GameSettings.Instance);
                }
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.SettingsJsonPath))
                {
                    var settings = new JsonSerializerSettings
                    {
                        Converters = Converters.Settings
                    };
                    var serializer = JsonSerializer.Create(settings);
                    var reader = new JsonTextReader(fs);

                    GameSettings.Instance = serializer.Deserialize<GameSettings>(reader);
                }
            }

            // TEMPORARY: we don't have a settings menu yet
            GameSettings.Instance = GameSettings.GetDefault();
            GameSettings.FinalizeInstance();

            //globals
            if (!Directory.Exists(FileStatics.GlobalsPath))
            {
                Directory.CreateDirectory(FileStatics.GlobalsPath);
                using (var fs = File.CreateText(FileStatics.GlobalsMapPath))
                {
                    fs.Write(FileStatics.GlobalsMapBase);
                }

                globalsMap = FileStatics.GlobalsMapBaseSerialized;

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

            //charts
            if (!Directory.Exists(FileStatics.ChartsPath))
            {
                Directory.CreateDirectory(FileStatics.ChartsPath);
                using (var fs = File.CreateText(FileStatics.ChartsMapPath))
                {
                    fs.Write(FileStatics.ChartsMapBase);
                }

                chartsMap = FileStatics.ChartsMapBaseSerialized;

                //copy in pre-built charts
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.ChartsMapPath))
                {
                    var settings = new JsonSerializerSettings
                    {
                        Converters = Converters.Levels
                    };
                    var serializer = JsonSerializer.Create(settings);
                    var reader = new JsonTextReader(fs);

                    chartsMap = serializer.Deserialize<Dictionary<string, LevelInfoInternal>>(reader);
                }
            }

            //packs path
            if (!Directory.Exists(FileStatics.PacksPath))
            {
                Directory.CreateDirectory(FileStatics.PacksPath);
                using (var fs = File.CreateText(FileStatics.PacksMapPath))
                {
                    fs.Write(FileStatics.PacksMapBase);
                }

                packsMap = FileStatics.PacksMapBaseSerialized;

                //copy in pre-built packs
            }
            else
            {
                using (var fs = File.OpenText(FileStatics.PacksMapPath))
                {
                    var serializer = JsonSerializer.Create();
                    var reader = new JsonTextReader(fs);

                    packsMap = serializer.Deserialize<Dictionary<string, PackInfo>>(reader);
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

        public static void UpdateChartMap()
        {
            using (var fs = File.Open(FileStatics.ChartsMapPath, FileMode.Truncate, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings {Converters = Converters.Levels});
                var writer = new JsonTextWriter(sw);

                serializer.Serialize(writer, chartsMap);
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
                    AddLevel(FileStatics.TempPath, false);
                }
                catch (Exception e)
                {
                    Directory.Delete(FileStatics.TempPath, true);
                    throw e;
                }
            }

            //update maps
            UpdateChartMap();
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
                AddLevel(Directory.GetDirectories(FileStatics.TempPath)[0]);
            }
            catch (Exception e)
            {
                Directory.Delete(FileStatics.TempPath, true);
                throw e;
            }

            Directory.Delete(FileStatics.TempPath, true);
        }

        public static void AddLevel(string directory, bool updateMaps = true)
        {
            var dirInfo = new DirectoryInfo(directory);

            var files = dirInfo.EnumerateFiles();

            //find settings and globals
            FileInfo settings = null;
            foreach (var file in files)
            {
                //settings
                if (file.Name == "settings.json")
                {
                    settings = file;
                }
            }

            if (settings == null)
            {
                throw new Exception("No found settings.json");
            }

            //get level information
            LevelInfo levelInfo;

            using (var fs = settings.OpenText())
            {
                var serializer = JsonSerializer.Create(new JsonSerializerSettings { Converters = Converters.Levels });
                var reader = new JsonTextReader(fs);

                levelInfo = serializer.Deserialize<LevelInfo>(reader);
            }

            //validate namespacing
            if (!levelInfo.ns.All(ch => char.IsLetterOrDigit(ch) || ch == '.'))
            {
                throw new Exception("The namespace of the given file is invalid.");
            }

            string targetChartPath = Path.Combine(FileStatics.ChartsPath, levelInfo.ns);

            //get globals
            var subdirs = dirInfo.GetDirectories();
            List<string> globalsTracker = new List<string>();
            if (subdirs.Length != 0)
            {
                // /...
                if (subdirs.Length > 1 || subdirs[0].Name != FileStatics.Globals)
                {
                    throw new Exception("Subdirectories other than the global subdirectory are not permitted in a .arccorelevel file.");
                }

                var globals = subdirs[0];

                // /globals/...
                if (globals.GetDirectories().Length != 0)
                {
                    throw new Exception("Subdirectories are not permitted within the global subdirectory.");
                }

                //import all
                bool anyExec = false;
                foreach (var file in globals.EnumerateFiles())
                {
                    Console.WriteLine(file.Name);
                    if (!FileStatics.IsValidGlobalName(file.Name))
                    {
                        throw new Exception($"Invalid global name: {file.Name}.");
                    }

                    ImportGlobal(file, globalsTracker);
                    anyExec = true;
                }

                //update global map if needed
                if (anyExec && updateMaps)
                {
                    UpdateGlobalMap();
                }
            }

            //free up target chart path
            if (Directory.Exists(targetChartPath))
            {
                //clear existing files
                foreach (var fs in Directory.EnumerateFiles(targetChartPath))
                {
                    File.Delete(fs);
                }
            }
            else
            {
                Directory.CreateDirectory(targetChartPath);
            }

            //copy in charts
            foreach (var file in files)
            {
                if (file.Name == "settings.json")
                {
                    continue;
                }

                File.Copy(file.FullName, Path.Combine(targetChartPath, file.Name));
            }

            //create and serialize full level info
            LevelInfoInternal levelInfoFull = new LevelInfoInternal(levelInfo, globalsTracker.ToArray());
            chartsMap.Add(levelInfo.ns, levelInfoFull);

            //update chart map if needed
            if (updateMaps)
            {
                UpdateChartMap();
            }
        }

        /// WARNING: might cause errors if files are tampered with or there are bugs!
        public static void DeleteLevel(string levelNs)
        {
            var info = chartsMap[levelNs];

            //delete globals
            foreach (var glb in info.importedGlobals)
            {
                DeleteGlobal(glb);
            }

            //delete level data
            Directory.Delete(Path.Combine(FileStatics.ChartsPath, levelNs), true);

            //delete from maps and update
            chartsMap.Remove(levelNs);

            UpdateChartMap();
            UpdateGlobalMap();
        }
    }
}