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
        private static List<Level> importingLevel;
        private static List<Pack> importingPack;
        private static List<IArccoreInfo> importingData;
        private static List<string> importingFiles;
        private static Dictionary<string, IArccoreInfo> externalIdToData;
        private static List<(IArccoreInfo, IArccoreInfo)> toReplace;
        private static List<IArccoreInfo> toStore;
        public static string importError;
        public static Dictionary<string, List<IArccoreInfo>> pendingConflicts;
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
            importingLevel = new List<Level>();
            importingPack = new List<Pack>();
            importingData = new List<IArccoreInfo>();

            (_, importingFiles) = ReadItem(dir, ArccoreInfoType.All);

            CheckForMissingPackReferences();
            CheckForDuplicateExternalIdWithinPackage();
            if (!CheckForExternalIdentifierConflicts())
                FinalizeImport();
        }
        #endregion
        
        #region Validate
        private static void CheckForMissingPackReferences()
        {
            //No need to worry about duplicate pack external ids for now. User will select one of the existing packs
            //This just ensures there will be 0 errors 
            HashSet<string> packIds = new HashSet<string>(importingPack.Select(p => p.ExternalId));
            packIds.UnionWith( Database.Current.GetCollection<Pack>().FindAll().Select(p => p.ExternalId) );

            foreach (Level level in importingLevel)
            {
                if (!packIds.Contains(level.PackExternalId))
                    throw new FileLoadException($"Cannot import level {level.ExternalId}. Pack {level.PackExternalId} not found.");
            }
        }

        private static void CheckForDuplicateExternalIdWithinPackage()
        {
            HashSet<string> ids = new HashSet<string>();
            List<IArccoreInfo> importing = new List<IArccoreInfo>(importingLevel);
            importing.AddRange(importingPack);
            importing.AddRange(importingData);

            foreach (IArccoreInfo item in importing)
            {
                string id = item.ExternalIdentifier();
                if (ids.Contains(id))
                    throw new FileLoadException($"Duplicate external ids deteced within the same package: {id}");
                ids.Add(id);
            }
        }

        private static bool CheckForExternalIdentifierConflicts()
        {
            pendingConflicts = new Dictionary<string, List<IArccoreInfo>>();
            externalIdToData = new Dictionary<string, IArccoreInfo>();

            foreach (IArccoreInfo data in importingData)
            {
                List<IArccoreInfo> conflicts = data.ConflictingExternalIdentifier();
                if (conflicts.Count > 0) pendingConflicts.Add(data.ExternalIdentifier(), conflicts);
                externalIdToData.Add(data.ExternalIdentifier(), data);
                toStore.Add(data);
            }
            foreach (Pack pack in importingPack)
            {
                List<IArccoreInfo> conflicts = pack.ConflictingExternalIdentifier();
                if (conflicts.Count > 0) pendingConflicts.Add(pack.ExternalIdentifier(), conflicts);
                externalIdToData.Add(pack.ExternalIdentifier(), pack);
                toStore.Add(pack);
            }
            foreach (Level level in importingLevel)
            {
                List<IArccoreInfo> conflicts = level.ConflictingExternalIdentifier();
                if (conflicts.Count > 0) pendingConflicts.Add(level.ExternalIdentifier(), conflicts);
                externalIdToData.Add(level.ExternalIdentifier(), level);
                toStore.Add(level);
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
            IArccoreInfo replaceWith = externalIdToData[replaced.ExternalIdentifier()];
            toReplace.Add((replaced, replaceWith));
            toStore.Remove(replaceWith);
        }

        //Called after all conflicts are resolved
        public static void FinalizeImport()
        {
            //There should be no issues importing now
            foreach ((IArccoreInfo replaced, IArccoreInfo replaceWith) in toReplace)
                replaced.Update(replaceWith);
            foreach (IArccoreInfo item in toStore) 
            {
                if (item is Level level)
                {
                    level.Pack = externalIdToData[level.PackExternalId] as Pack;
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
            importingLevel = null;
            importingPack = null;
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
            foreach(string path in importingFiles)
            {
                FileStorage.ImportFile(Path.Combine(currentDir.FullName, path), path);
            }
        }

        // 0th: I have no idea what to do with allowedTypes so i'll just keep it as All
        ///<summary>
        ///Recursively reads the directory and add detected item to importingLevel and importingData;
        ///</summary>
        ///<returns>List of all files present and list of files referenced by IArccoreInfo defined assets</returnx>
        public static (List<string> allFiles, List<string> referencedFiles)
            ReadItem(DirectoryInfo sourceDirectory, ArccoreInfoType allowedTypes, string sourceDirectoryRelative = "")
        {
            // Get all files
            List<string> allFiles =
                sourceDirectory.EnumerateFiles()
                               .Where(f => f.Name != FileStatics.SettingsJson && FileStatics.SupportedFileExtensions.Contains(f.Extension))
                               .Select(f => Path.Combine(sourceDirectoryRelative, f.Name)).ToList();
            List<string> referencedFiles = new List<string>();

            //Recursion
            foreach(DirectoryInfo dir in sourceDirectory.EnumerateDirectories())
            {
                (List<string> subdirAll, List<string> subdirReferenced)
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
            var ids = jObj.Get<string>("external_id");
            var data = jObj.Get<JObject>("data");

            // Read correct info
            var dataReader = data.CreateReader();

            try
            {
                switch (type)
                {
                    case "level":
                        if (!allowedTypes.HasFlag(ArccoreInfoType.Level))
                            throw new JsonReaderException("Levels are dissallowed in the current context.");

                        Level level = JsonUserInput.ReadLevelJson(dataReader);
                        referencedFiles.AddRange(ApplyReference(level, allFiles));
                        importingLevel.Add(level);
                        break;

                    case "pack":
                        if (!allowedTypes.HasFlag(ArccoreInfoType.Pack))
                            throw new JsonReaderException("Packs are dissallowed in the current context.");

                        Pack pack = JsonUserInput.ReadPackJson(dataReader);
                        referencedFiles.AddRange(ApplyReference(pack, allFiles));
                        importingPack.Add(pack);
                        break;

                    case "partner":
                        throw new NotImplementedException();
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

        private static IEnumerable<string> ApplyReference(IArccoreInfo aInfo, List<string> files)
        {
            List<string> optimized = aInfo.TryApplyReferences(files, out string missing);
            if (missing == null) throw new JsonReaderException($"Referenced {missing} file does not exist");

            string prefix = aInfo.VirtualPathPrefix();

            return optimized.Select(path => Path.Combine(prefix, path));
        }
        #endregion
    }
}