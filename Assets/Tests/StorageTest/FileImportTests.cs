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
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, ".*",SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destPath), true);
        }

        private void ClearFiles()
        {
            FileStorage.Clear();
        }

        private string PathOf(string filename)
            => Path.Combine(tempPath, filename);

        private void CreateFile(string fileName, string content = "content")
        {
            string path = PathOf(fileName);
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(tempPath);
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
            CreateFile(pack.ImagePath);
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

        private void CreateFakeLevel(string exid, string pack = null, string path = "")
        {
            Level level = new Level
            {
                PackExternalId = pack,
                Charts = new Chart[]
                {
                    new Chart {
                        DifficultyGroup = JsonUserInput.Past,
                        Difficulty = new Difficulty("1"),
                        Name = "testsong",
                        Artist = "testartist",
                        Bpm = "100",
                        Constant = 1,
                        Background = "bg.jpg",
                    }
                }
            };
            CreateLevel(level, exid, path);
        }


        [SetUp]
        public void SetUp()
        {
            Database.Initialize();
            if (!Directory.Exists(tempPath)) return;
            DirectoryInfo dir = new DirectoryInfo(tempPath);
            dir.Delete(true);
            dir.Create();

            if (Directory.Exists(FileStatics.RootPath))
            {
                CopyFolder(FileStatics.RootPath, backupPath);
                ClearFiles();
            }
        }

        [TearDown]
        public void TearDown()
        {
            ClearFiles();
            if (Directory.Exists(backupPath))
            {
                CopyFolder(backupPath, FileStatics.RootPath);
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
            Assert.Fail("Implement this test");
        }

        [Test]
        public void MissingAssetPackImport_ThrowsError()
        {
            Assert.Fail("Implement this test");
        }
        #endregion

        #region Combination
        [Test]
        public void LevelAndPackImport_Success()
        {
            Assert.Fail("Implement this test");
        }

        [Test]
        public void DuplicateExternalIdentifier_ThrowsError()
        {
            Assert.Fail("Implement this test");
        }
        #endregion

        #region Conflict resolve
        [Test]
        public void ExternalIdConflictImport_CanRetrievePendingConflicts()
        {
            Assert.Fail("Implement this test");
        }

        [Test]
        public void ExternalIdConflictImport_ReplaceOld()
        {
            Assert.Fail("Implement this test");
        }

        [Test]
        public void ExternalIdConflictImport_CreateNew()
        {
            Assert.Fail("Implement this test");
        }
        
        [Test]
        public void ExternalIdConflictImport_Abort()
        {
            Assert.Fail("Implement this test");
        }
        #endregion
    }
}