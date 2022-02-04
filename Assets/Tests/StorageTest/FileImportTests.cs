using System.Collections;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using ArcCore.Storage;
using ArcCore.Storage.Data;

namespace Tests.StorageTests
{
    public class FileImportTests
    {
        #region Helper
        private static string tempPath => FileStatics.TempPath;
        private static readonly string backupPath = Path.Combine(Application.dataPath, "_storagebackup");

        private void CopyFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, ".*",SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destPath), true);
        }

        private string PathOf(string filename)
            => Path.Combine(tempPath, filename);

        private void CreateFile(string fileName, string content = "content")
        {
            string path = PathOf(fileName);
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) File.Delete(path);
            using (StreamWriter stream = File.CreateText(PathOf(path)))
            {
                stream.Write(content);
            }
        }

        private void CreatePack(Pack pack, string exid, string path)
        {
            string settingsJson = JsonHelper.GenerateSettingsJson(pack, exid);
            CreateFile(Path.Combine(path, FileStatics.SettingsJson), settingsJson);
            CreateFile(Path.Combine(path, pack.ImagePath));
        }

        private void CreateLevel(Level level, string exid, string path)
        {
            string settingsJson = JsonHelper.GenerateSettingsJson(level, exid);
            CreateFile(Path.Combine(path, FileStatics.SettingsJson), settingsJson);
            foreach (Chart chart in level.Charts)
            {
                CreateFile(Path.Combine(path, chart.SongPath ?? "base.ogg"));
                CreateFile(Path.Combine(path, chart.ImagePath ?? "base.jpg"));
                CreateFile(Path.Combine(path, chart.ChartPath ?? JsonUserInput.GetChartPathFromPresetGroup(chart.DifficultyGroup)));
                CreateFile(Path.Combine(path, chart.Background));
            }
        }

        private void CreateFakeLevel(string exid, string pack = null, string path = "", string name = "testsong")
        {
            Level level = new Level
            {
                PackExternalId = pack,
                Charts = new Chart[]
                {
                    new Chart {
                        DifficultyGroup = JsonUserInput.Past,
                        Difficulty = new Difficulty("1"),
                        Name = name,
                        Artist = "testartist",
                        Bpm = "100",
                        Constant = 1,
                        Background = "bg.jpg",
                    }
                }
            };
            CreateLevel(level, exid, path);
        }

        private void CreateFakePack(string exid, string path = "")
        {
            Pack pack = new Pack
            {
                Name = "testpack",
                ExternalId = "testpack",
                ImagePath = "pack.png"
            };
            CreatePack(pack, exid, path);
        }


        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(FileStatics.RootPath))
                CopyFolder(FileStatics.RootPath, backupPath);
            Database.Initialize();
            Database.Clear();
            FileStorage.Clear();

            DirectoryInfo dir = new DirectoryInfo(tempPath);
            if (Directory.Exists(tempPath))
            {
                dir.Delete(true);
            }
            dir.Create();
        }

        [TearDown]
        public void TearDown()
        {
            Database.Dispose();
            (new DirectoryInfo(FileStatics.RootPath)).Delete(true);
            if (Directory.Exists(backupPath))
                CopyFolder(backupPath, FileStatics.RootPath);
            DirectoryInfo dir = new DirectoryInfo(tempPath);
            if (Directory.Exists(tempPath))
            {
                dir.Delete(true);
            }
        }
        #endregion

        #region __settings.json
        [Test]
        public void InvalidSettingsImport_MissingExternalId_ThrowsError()
        {
            string json = $@"
            {{
                ""type"": ""level""
                ""data"": ""{{}}""
            }}";
            CreateFile(FileStatics.SettingsJson, json);

            Assert.Throws<JsonReaderException>(() => {
                FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            });
        }

        [Test]
        public void InvalidSettingsImport_WrongDataType_ThrowsError()
        {
            CreateFile("testimg.jpg");
            (_, string packJson) = JsonHelper.GeneratePack("testpack", "testimg.jpg");
            string json = $@"
            {{
                ""type"": ""level"",
                ""external_id"": ""testpack"",
                ""data"": {packJson}
            }}";
            CreateFile(FileStatics.SettingsJson, json);

            Assert.Throws<JsonReaderException>(() => {
                FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            });
        }
        #endregion

        #region Level
        [Test]
        public void LevelImport_Success()
        {
            CreateFakeLevel("testlevel");

            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));

            IEnumerable<Level> levels = LevelQuery.List();
            int count;
            if ((count = levels.Count()) != 1)
                Assert.Fail($"There should only be one level but {count} levels found");

            Level level = levels.First();
            Assert.That(level.ExternalId == "testlevel");
        }

        [Test]
        public void MissingAssetLevelImport_ThrowsError()
        {
            CreateFakeLevel("testlevel");
            File.Delete(PathOf("base.ogg"));

            Assert.Throws<JsonReaderException>(() => {
                FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            });
        }

        [Test]
        public void MissingPackLevelImport_ThrowsError()
        {
            CreateFakeLevel("testlevel", "testpack");

            Assert.Throws<FileLoadException>(() => {
                FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            });
        }
        #endregion

        #region Pack
        [Test]
        public void PackImport_Success()
        {
            CreateFakePack("testpack");

            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));

            IEnumerable<Pack> packs = PackQuery.List();
            int count;
            if ((count = packs.Count()) != 1)
                Assert.Fail($"There should only be one pack but {count} packs found");
            
            Pack pack = packs.First();
            Assert.That(pack.ExternalId == "testpack");
        }

        [Test]
        public void MissingAssetPackImport_ThrowsError()
        {
            CreateFakePack("testpack");
            File.Delete(PathOf("pack.png"));

            Assert.Throws<JsonReaderException>(() => {
                FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            });
        }
        #endregion

        #region Combination
        [Test]
        public void LevelAndPackImport_Success()
        {
            CreateFakeLevel("testlevel", "testpack", "level");
            CreateFakePack("testpack", "pack");

            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));

            int levelCount = LevelQuery.List().Count();
            int packCount = PackQuery.List().Count();

            Assert.That(packCount == 1 && levelCount == 1);
        }

        [Test]
        public void DuplicateExternalIdentifier_ThrowsError()
        {
            CreateFakeLevel("testlevel", "testpack", "level");
            CreateFakePack("testlevel", "pack");

            Assert.Throws<FileLoadException>(() => {
                FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            });
        }
        #endregion

        #region Conflict resolve
        [Test]
        public void ExternalIdConflictImport_CanRetrievePendingConflicts()
        {
            CreateFakeLevel("testlevel", null);
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));

            CreateFakeLevel("testlevel", null);
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));

            foreach (List<IArccoreInfo> conflict in FileManagement.GetConflicts())
            {
                Assert.Pass($"{conflict[0].ExternalId} item has conflict with {conflict.Count} items");
            }
            Assert.Fail("No conflicts found");
        }

        [Test]
        public void ExternalIdConflictImport_ReplaceOld()
        {
            CreateFakeLevel("testlevel", null);
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            Level oldLevel = LevelQuery.List().First();

            CreateFakeLevel("testlevel", null, "", "replacedSong");
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));

            foreach (List<IArccoreInfo> conflict in FileManagement.GetConflicts())
                FileManagement.Replace(conflict[0]);
            FileManagement.FinalizeImport();

            Assert.AreEqual(LevelQuery.Get(oldLevel.Id).Charts[0].Name, "replacedSong");
        }

        [Test]
        public void ExternalIdConflictImport_CreateNew()
        {
            CreateFakeLevel("testlevel", null);
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            Level oldLevel = LevelQuery.List().First();

            CreateFakeLevel("testlevel", null, "", "newSong");
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            FileManagement.FinalizeImport();

            IEnumerable<Level> levels = LevelQuery.List();
            int count = levels.Count();

            if (count != 2)
                Assert.Fail($"Expected 2 levels but {count} exists");

            foreach(Level level in levels)
            {
                if (level.Charts[0].Name == "newSong")
                {
                    Assert.Pass("Level imported as new");
                }
            }
        }
        
        [Test]
        public void ExternalIdConflictImport_Abort()
        {
            CreateFakeLevel("testlevel", null);
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            Level oldLevel = LevelQuery.List().First();

            CreateFakeLevel("testlevel", null, "", "newSong");
            FileManagement.ImportDirectory(new DirectoryInfo(tempPath));
            FileManagement.Cleanup();

            IEnumerable<Level> levels = LevelQuery.List();
            int count = levels.Count();

            if (count != 1)
                Assert.Fail($"Expected 1 levels but {count} exists");
            
            Level level = levels.First();
            Assert.AreEqual(level.Charts[0].Name, "testsong");
        }
        #endregion
    }
}