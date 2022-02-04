using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using System.IO;
using ArcCore.Storage;

namespace Tests.StorageTests
{
    public class FileStorageTests
    {
        //This is bad test design but i dont care
        private static readonly string backupPath = Path.Combine(Application.dataPath, "_storagebackup");
        private void CopyFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, ".*",SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destPath), true);
        }

        private void CreateFile(string path, string content)
        {
            using (StreamWriter stream = File.CreateText(path))
            {
                stream.Write(content);
            }
        }

        private bool CompareFile(string path, string content)
        {
            string file = "";
            using (StreamReader stream = File.OpenText(path))
            {
                file = stream.ReadToEnd();
            }
            return file == content;
        }

        [SetUp]
        public void SetupFiles()
        {
            if (Directory.Exists(FileStatics.RootPath))
                CopyFolder(FileStatics.RootPath, backupPath);
            if (!Directory.Exists(FileStatics.TempPath))
                Directory.CreateDirectory(FileStatics.TempPath);
            Database.Initialize();
            Database.Clear();
            FileStorage.Clear();
        }

        [TearDown]
        public void TeardownFiles()
        {
            Database.Dispose();
            (new DirectoryInfo(FileStatics.RootPath)).Delete(true);
            if (Directory.Exists(backupPath))
                CopyFolder(backupPath, FileStatics.RootPath);
        }

        [Test]
        public void InsertSingleFile()
        {
            string path = Path.Combine(FileStatics.TempPath, "importTest.txt");
            string virtualPath = "virualPath";
            string content = "test content";
            CreateFile(path, content);

            FileStorage.ImportFile(path, virtualPath);

            if (File.Exists(path))
            {
                Assert.Fail("File did not get cleaned up");
                File.Delete(path);
            }

            string importedPath = FileStorage.GetFilePath(virtualPath);
            Assert.True(CompareFile(importedPath, content));
        }

        [Test]
        public void InsertTwoFilesWithDuplicateContent_ShouldOnlyStoreOne()
        {
            string content = "test content";

            string path1 = Path.Combine(FileStatics.TempPath, "importTest.txt");
            string virtualPath1 = "virualPath";
            CreateFile(path1, content);

            string path2 = Path.Combine(FileStatics.TempPath, "duplicateContent.txt");
            string virtualPath2 = "virtualPath2";
            CreateFile(path2, content);

            FileStorage.ImportFile(path1, virtualPath1);
            FileStorage.ImportFile(path2, virtualPath2);

            string importedPath1 = FileStorage.GetFilePath(virtualPath1);
            string importedPath2 = FileStorage.GetFilePath(virtualPath2);
            Assert.AreEqual(importedPath1, importedPath2);
        }

        [Test]
        public void InsertTwoFilesWithHashCollision_ShouldStoreTwo()
        {
            string content = "test content";
            string contentWithHashCollision = "collision content";

            //Find hash of contentWithHashCollision, then replacing the file with content, pretending it has the same hash
            string path1 = Path.Combine(FileStatics.TempPath, "importTest.txt");
            string virtualPath1 = "virualPath";

            CreateFile(path1, contentWithHashCollision);
            FileStorage.ImportFile(path1, virtualPath1);

            string importedPath1 = FileStorage.GetFilePath(virtualPath1);
            File.Delete(importedPath1);
            CreateFile(importedPath1, content);

            string path2 = Path.Combine(FileStatics.TempPath, "contentWithHashCollision.txt");
            string virtualPath2 = "virtualPath2";
            CreateFile(path2, contentWithHashCollision);

            FileStorage.ImportFile(path2, virtualPath2);

            string importedPath2 = FileStorage.GetFilePath(virtualPath2);
            Assert.AreNotEqual(importedPath1, importedPath2);
        }

        [Test]
        [TestCase(1)]
        [TestCase(3)]
        public void DeletingFilesWithReference(int referenceCount)
        {
            string content = "test content";
            string realPath = "";

            for (int i = 0; i < referenceCount; i++)
            {
                string path = Path.Combine(FileStatics.TempPath, "file.txt");
                string virtualPath = $"virtualPath{i}";

                CreateFile(path, content);
                FileStorage.ImportFile(path, virtualPath);
                realPath = FileStorage.GetFilePath(virtualPath);
                Debug.Log("real path" + realPath);
            }

            for (int i = 0; i < referenceCount - 1; i++)
            {
                FileStorage.DeleteReference($"virtualPath{i}");
                if (!File.Exists(realPath))
                    Assert.Fail("Real path deleted while virtual paths still exists");
            }

            FileStorage.DeleteReference($"virtualPath{referenceCount - 1}");
            Assert.False(File.Exists(realPath));
        }

        [Test]
        [TestCase(4, 0)]
        [TestCase(4, 2)]
        [TestCase(4, 3)]
        public void DeletingCollisionedFiles(int chainLength, int deleteIndex)
        {
            string content = "test content";
            string contentWithHashCollision = "collision content";

            List<string> pathChain = new List<string>();

            for (int i = 0; i < chainLength; i++)
            {
                string path = Path.Combine(FileStatics.TempPath, "file.txt");
                string virtualPath = $"virtualPath{i}";

                CreateFile(path, contentWithHashCollision);
                FileStorage.ImportFile(path, virtualPath);
                string realPath = FileStorage.GetFilePath(virtualPath);
                pathChain.Add(realPath);

                File.Delete(realPath);
                CreateFile(realPath, content);
            }

            string path2 = Path.Combine(FileStatics.TempPath, "contentWithHashCollision.txt");
            string virtualPathLast = "virtualPathLast";
            CreateFile(path2, contentWithHashCollision);
            FileStorage.ImportFile(path2, virtualPathLast);

            string toDeletePath = $"virtualPath{deleteIndex}";
            FileStorage.DeleteReference(toDeletePath);

            string importedPathLast = FileStorage.GetFilePath(virtualPathLast);

            Assert.AreEqual(importedPathLast, pathChain[deleteIndex]);
        }
    }
}
