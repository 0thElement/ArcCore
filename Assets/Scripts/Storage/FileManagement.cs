using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using ArcCore.Storage.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcCore.Storage
{
    public static class FileManagement
    {
        public static List<Level> levels;
        public static List<Pack> packs;

        #region State
        //Because this might be run on startup and MonoBehaviours are not yet instantiated
        //These properties are stored in case there are errors or conflicts that need user's confirmation
        private static DirectoryInfo currentDir;
        private static List<IArccoreInfo> importingData;
        private static Dictionary<(IArccoreInfo, string), string> importingFileReferences;
        private static Dictionary<string, IArccoreInfo> externalIdToData;
        private static List<(IArccoreInfo, IArccoreInfo)> toReplace;
        private static List<IArccoreInfo> toStore;
        public static string importError;
        private static Dictionary<string, List<IArccoreInfo>> pendingConflicts;
        //though is this really the best way to do this wtf
        #endregion

        #region Startup
        public static void OnAppStart()
        {
            //IMPORT DEFAULT ARCCOREPKG IF DB DOES NOT EXIST
            if (!File.Exists(FileStatics.DatabasePath))
            {
                if (Directory.Exists(FileStatics.FileStoragePath)) Directory.Delete(FileStatics.FileStoragePath, true);
                Database.Initialize();
                if (Application.platform == RuntimePlatform.Android)
                {
                    Debug.Log("Detected Android platform. Using UnityWebRequest to fetch default package");
                    LoadDefaultPackageAndroid(FileStatics.DefaultPackagePath);
                }
                else
                {
                    ImportLevelArchiveFile(FileStatics.DefaultPackagePath);
                }
            }
            else
            {
                Debug.Log("db file exists at " + FileStatics.DatabasePath);
                Database.Initialize();
            }
        }

        private static async void LoadDefaultPackageAndroid(string path)
        {
            //Fetch data from within obb with UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(path);
            await www.SendWebRequest();

            if (!string.IsNullOrWhiteSpace(www.error))
            {
                throw new System.Exception($"Cannot load default package");
            }

            //Copy to temporary path
            byte[] data = www.downloadHandler.data;
            string copyPath = Path.Combine(FileStatics.TempPath, FileStatics.DefaultPackage);
            using (FileStream fs = new FileStream(copyPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Write(data, 0, data.Length);
            }
            
            //Import copied file
            ImportLevelArchiveFile(copyPath);
            File.Delete(copyPath);
        }
        #endregion

        #region Actions
        public static void ImportLevelArchiveFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                Debug.Log("Importing package from " + path);
                ImportLevelArchive(new ZipArchive(fs));
            }
        }

        public static void ImportLevelArchive(ZipArchive archive)
        {
            importError = "";
            archive.ExtractToDirectory(FileStatics.TempImportPath);

            //try catch to prevent memory leakage
            try
            {
                ImportDirectory(new DirectoryInfo(FileStatics.TempImportPath));
            }
            catch (Exception e)
            {
                Directory.Delete(FileStatics.TempImportPath, true);
                importError = e.ToString();
                throw e;
            }
        }
        
        public static void ImportDirectory(DirectoryInfo dir)
        {
            currentDir = dir;
            importingData = new List<IArccoreInfo>();
            pendingConflicts = new Dictionary<string, List<IArccoreInfo>>();
            externalIdToData = new Dictionary<string, IArccoreInfo>();
            toStore = new List<IArccoreInfo>();
            toReplace = new List<(IArccoreInfo, IArccoreInfo)>();

            (_, importingFileReferences) = ReadItem(dir, ArccoreInfoType.All);

            CheckForMissingPackReferences();
            CheckForDuplicateExternalIdWithinPackage();
            if (CheckForExternalIdentifierConflicts())
                FinalizeImport();
        }
        #endregion
        
        #region Validate
        private static void CheckForMissingPackReferences()
        {
            HashSet<string> packIds = new HashSet<string>(importingData.Where(p => p is Pack).Select(p => p.ExternalId));

            foreach (Level level in importingData.Where(l => l is Level))
            {
                if (level.PackExternalId != null && !packIds.Contains(level.PackExternalId))
                    throw new FileLoadException($"Cannot import level {level.ExternalId}. Pack {level.PackExternalId} not found.");
            }
        }

        private static void CheckForDuplicateExternalIdWithinPackage()
        {
            HashSet<string> ids = new HashSet<string>();

            foreach (IArccoreInfo item in importingData)
            {
                string id = item.ExternalId;
                if (ids.Contains(id))
                    throw new FileLoadException($"Duplicate external ids deteced within the same package: {id}");
                ids.Add(id);
            }
        }

        private static bool CheckForExternalIdentifierConflicts()
        {

            foreach (IArccoreInfo data in importingData)
            {
                List<IArccoreInfo> conflicts = data.ConflictingExternalIdentifier();
                if (conflicts.Count > 0) pendingConflicts.Add(data.ExternalId, conflicts);
                externalIdToData.Add(data.ExternalId, data);
                toStore.Add(data);
            }

            return pendingConflicts.Count == 0;
        }
        #endregion

        #region Callback for conflict resolver
        public static IEnumerable<List<IArccoreInfo>> GetConflicts()
        {
            foreach (string id in pendingConflicts.Keys)
                yield return pendingConflicts[id];
        }

        public static void Replace(IArccoreInfo replaced)
        {
            IArccoreInfo replaceWith = externalIdToData[replaced.ExternalId];
            toReplace.Add((replaced, replaceWith));
            toStore.Remove(replaceWith);
        }

        //Called after all conflicts are resolved
        public static void FinalizeImport()
        {
            Dictionary<string, int> packInternalId = new Dictionary<string, int>();

            //There should be no issues importing now
            foreach ((IArccoreInfo replaced, IArccoreInfo replaceWith) in toReplace)
            {
                int id = replaced.Update(replaceWith);
                if (replaceWith is Pack) packInternalId.Add(replaceWith.ExternalId, id);
            }

            foreach (IArccoreInfo item in toStore) 
            {
                if (item is Pack pack)
                {
                    int id = pack.Insert();
                    packInternalId.Add(pack.ExternalId, id);
                }
            }

            foreach (IArccoreInfo item in toStore) 
            {
                if (item is Level level)
                {
                    if (level.PackExternalId == null)
                        level.PackId = -1;
                    else
                        level.PackId = packInternalId[level.PackExternalId];
                    level.Insert();
                }
                else if (!(item is Pack))
                    item.Insert();
            }
            ImportFiles();
            Cleanup();
        }

        public static void Cleanup()
        {
            Directory.Delete(FileStatics.TempPath, true);
            importingData = null;
            importingFileReferences = null;
            toReplace = null;
            externalIdToData = null;
            pendingConflicts = null;
            importError = null;
        }
        #endregion

        #region Storage
        private static void ImportFiles()
        {
            foreach (IArccoreInfo data in importingData)
            {
                foreach (string rawVirtualPath in data.FileReferences)
                {
                    string virtualPath = Path.Combine(data.VirtualPathPrefix(), rawVirtualPath);
                    string realPath = Path.Combine(currentDir.FullName, importingFileReferences[(data, rawVirtualPath)]);
                    FileStorage.ImportFile(realPath, virtualPath);
                }
            }
        }

        private static List<string> CombineWithDirectory(List<string> files, string dir)
        {
            return files.Select(f => Path.Combine(dir, f)).ToList();
        }

        private static Dictionary<(IArccoreInfo, string), string> CombineWithDirectory(Dictionary<(IArccoreInfo, string), string> files, string dir)
        {
            List<(IArccoreInfo, string)> keys = files.Select(pair => pair.Key).ToList();
            foreach ((IArccoreInfo item, string refPath) in keys)
                files[(item, refPath)] = Path.Combine(dir, files[(item, refPath)]);
            return files;
        }

        private static void CombineDictionaries(Dictionary<(IArccoreInfo, string), string> dict, Dictionary<(IArccoreInfo, string), string> other)
        {
            foreach ((IArccoreInfo item, string refPath) in other.Keys)
                dict.Add((item, refPath), other[(item, refPath)]);
        }

        // 0th: I have no idea what to do with allowedTypes so i'll just keep it as All
        ///<summary>
        ///Recursively reads the directory and add detected item to importingLevel and importingData;
        ///</summary>
        ///<returns>List of all files present and list of files referenced by IArccoreInfo defined assets</returnx>
        public static (List<string> allFiles, Dictionary<(IArccoreInfo, string), string> fileReferences)
            ReadItem(DirectoryInfo sourceDirectory, ArccoreInfoType allowedTypes, string sourceDirectoryRelative = "")
        {
            // Get all files
            List<string> allFiles =
                sourceDirectory.EnumerateFiles()
                               .Where(f => f.Name != FileStatics.SettingsJson && FileStatics.SupportedFileExtensions.Contains(f.Extension))
                               .Select(f => f.Name).ToList();
            Dictionary<(IArccoreInfo, string), string> fileReferences = new Dictionary<(IArccoreInfo, string), string>();

            //Recursion
            foreach(DirectoryInfo dir in sourceDirectory.EnumerateDirectories())
            {
                (List<string> subdirAll, Dictionary<(IArccoreInfo, string), string> subdirReferenced)
                    = ReadItem(dir, allowedTypes, Path.Combine(sourceDirectoryRelative, dir.Name));

                allFiles.AddRange(subdirAll);
                CombineDictionaries(fileReferences, subdirReferenced);
            }

            //If settings file doesn't exists then just grab all the files available and quit
            var settingsFile = sourceDirectory.EnumerateFiles().FirstOrDefault(f => f.Name == FileStatics.SettingsJson);
            if (settingsFile == null)
                return (CombineWithDirectory(allFiles, sourceDirectoryRelative), CombineWithDirectory(fileReferences, sourceDirectoryRelative));

            // Read data from settings file
            JObject jObj;

            using(var reader = settingsFile.OpenText())
            {
                var jReader = new JsonTextReader(reader);
                var jSerializer = JsonSerializer.CreateDefault();
                jObj = jSerializer.Deserialize<JObject>(jReader);
            }

            var type = jObj.Get<string>("type");
            var exid = jObj.Get<string>("external_id");
            var dataReader = JsonUtils.ExtractProperty(jObj, "data");

            try
            {
                switch (type)
                {
                    case "level":
                        if (!allowedTypes.HasFlag(ArccoreInfoType.Level))
                            throw new JsonReaderException("Levels are dissallowed in the current context.");

                        Level level = JsonUserInput.ReadLevelJson(dataReader);
                        level.ExternalId = exid;
                        CombineDictionaries(fileReferences, ApplyReference(level, allFiles));
                        importingData.Add(level);
                        break;

                    case "pack":
                        if (!allowedTypes.HasFlag(ArccoreInfoType.Pack))
                            throw new JsonReaderException("Packs are dissallowed in the current context.");

                        Pack pack = JsonUserInput.ReadPackJson(dataReader);
                        pack.ExternalId = exid;
                        CombineDictionaries(fileReferences, ApplyReference(pack, allFiles));
                        importingData.Add(pack);
                        break;

                    default: 
                        throw new JsonReaderException($"Unknown data type '{type}'.");
                }
            }
            catch (Exception e)
            {
                throw new JsonReaderException($"Error parsing __settings.json in {sourceDirectoryRelative}.\n{e}");
            }
            return (CombineWithDirectory(allFiles, sourceDirectoryRelative), CombineWithDirectory(fileReferences, sourceDirectoryRelative));
        }

        private static Dictionary<(IArccoreInfo, string), string> ApplyReference(IArccoreInfo aInfo, List<string> files)
        {
            List<string> optimized = aInfo.TryApplyReferences(files, out string missing);
            if (missing != null) throw new JsonReaderException($"Referenced {missing} file does not exist");

            return optimized.ToDictionary(path => (aInfo, path));
        }
        #endregion
    }
}