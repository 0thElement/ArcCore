using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using ArcCore.Storage.Data;
using Newtonsoft.Json.Linq;

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
        private static List<(string virtualPath, string realPath)> importingFiles;
        private static Dictionary<string, IArccoreInfo> externalIdToData;
        private static List<(IArccoreInfo, IArccoreInfo)> toReplace;
        private static List<IArccoreInfo> toStore;
        public static string importError;
        private static Dictionary<string, List<IArccoreInfo>> pendingConflicts;
        //though is this really the best way to do this wtf
        #endregion

        #region Actions
        public static void OnAppStart()
        {
            //IMPORT DEFAULT ARCCOREPKG IF DB DOES NOT EXIST
            if (!File.Exists(FileStatics.DatabasePath))
            {
                Directory.Delete(FileStatics.FileStoragePath);
                //this is complicated... we should put the pkg in a streaming asset but android is really fussy about it
            }
            Database.Initialize();
        }

        public static void AddLevelArchive(ZipArchive archive)
        {
            importError = "";
            archive.ExtractToDirectory(FileStatics.TempPath);

            //try catch to prevent memory leakage
            try
            {
                ImportDirectory(new DirectoryInfo(FileStatics.TempPath));
            }
            catch (Exception e)
            {
                Directory.Delete(FileStatics.TempPath, true);
                importError = e.ToString();
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

            (_, importingFiles) = ReadItem(dir, ArccoreInfoType.All);

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

        #region Callback
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
                UnityEngine.Debug.Log(item.ExternalId);
                if (item is Level level)
                {
                    if (level.PackExternalId == null)
                        level.PackId = -1;
                    else
                        level.PackId = packInternalId[level.PackExternalId];
                    level.Insert();
                }
                else
                    item.Insert();
            }
            ImportFiles();
            Cleanup();
        }

        public static void Cleanup()
        {
            Directory.Delete(FileStatics.TempPath, true);
            importingData = null;
            importingFiles = null;
            toReplace = null;
            externalIdToData = null;
            pendingConflicts = null;
            importError = null;
        }
        #endregion

        #region Storage
        private static void ImportFiles()
        {
            foreach ((string virtualPath, string realPath) in importingFiles)
            {
                FileStorage.ImportFile(Path.Combine(currentDir.FullName, realPath), virtualPath);
            }
        }

        // 0th: I have no idea what to do with allowedTypes so i'll just keep it as All
        ///<summary>
        ///Recursively reads the directory and add detected item to importingLevel and importingData;
        ///</summary>
        ///<returns>List of all files present and list of files referenced by IArccoreInfo defined assets</returnx>
        public static (List<string> allFiles, List<(string virtualPath, string realPath)> referencedFiles)
            ReadItem(DirectoryInfo sourceDirectory, ArccoreInfoType allowedTypes, string sourceDirectoryRelative = "")
        {
            // Get all files
            List<string> allFiles =
                sourceDirectory.EnumerateFiles()
                               .Where(f => f.Name != FileStatics.SettingsJson && FileStatics.SupportedFileExtensions.Contains(f.Extension))
                               .Select(f => Path.Combine(sourceDirectoryRelative, f.Name)).ToList();
            List<(string, string)> referencedFiles = new List<(string, string)>();

            //Recursion
            foreach(DirectoryInfo dir in sourceDirectory.EnumerateDirectories())
            {
                (List<string> subdirAll, List<(string,string)> subdirReferenced)
                    = ReadItem(dir, allowedTypes, Path.Combine(sourceDirectoryRelative, dir.Name));

                allFiles.AddRange(subdirAll);
                referencedFiles.AddRange(subdirReferenced);
            }

            //If settings file doesn't exists then just grab all the files available and quit
            var settingsFile = sourceDirectory.EnumerateFiles().FirstOrDefault(f => f.Name == FileStatics.SettingsJson);
            if (settingsFile == null) return (allFiles, referencedFiles);

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
                        referencedFiles.AddRange(ApplyReference(level, allFiles));
                        importingData.Add(level);
                        break;

                    case "pack":
                        if (!allowedTypes.HasFlag(ArccoreInfoType.Pack))
                            throw new JsonReaderException("Packs are dissallowed in the current context.");

                        Pack pack = JsonUserInput.ReadPackJson(dataReader);
                        pack.ExternalId = exid;
                        referencedFiles.AddRange(ApplyReference(pack, allFiles));
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
            return (allFiles, referencedFiles);
        }

        private static IEnumerable<(string, string)> ApplyReference(IArccoreInfo aInfo, List<string> files)
        {
            List<string> optimized = aInfo.TryApplyReferences(files, out string missing);
            if (missing != null) throw new JsonReaderException($"Referenced {missing} file does not exist");

            string prefix = aInfo.VirtualPathPrefix();

            return optimized.Select(path => (Path.Combine(prefix, path), path));
        }
        #endregion
    }
}