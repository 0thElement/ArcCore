using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LiteDB;
using System.Security.Cryptography;
using ArcCore.Storage.Data;
using ArcCore.Utilities;

namespace ArcCore.Storage
{
    public class FileStorage
    {
        private static readonly string storagePath = Path.Combine(Application.dataPath, "_storage");

        private static ILiteCollection<FileReference> collection;
        public static ILiteCollection<FileReference> Collection
        {
            get
            {
                if (collection == null) collection = Database.Current.GetCollection<FileReference>();
                return collection;
            }
        }

        private static void IncrementOneBit(ref byte[] bytes)
        {
            byte carry = 1;
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                if (bytes[i] < byte.MaxValue)
                {
                    bytes[i] += carry;
                    return;
                }
                else
                {
                    bytes[i] = 0;
                    carry = 1;
                }
            }
            return;
        }

        private static byte[] ComputeHash(FileStream stream)
        {
            using (SHA512 sha = SHA512Managed.Create())
                return sha.ComputeHash(stream);
        }

        public static void ImportFile(string filePath, string virtualPath)
        {
            //Read file content
            using (FileStream stream = File.OpenRead(filePath))
            {
                //Calculate hash
                byte[] hashBytes = ComputeHash(stream);
                string hash = hashBytes.ToBase62();

                string ext = Path.GetExtension(filePath);
                string correctHashPath = Path.Combine(hash, ext);
                
                //Resolve collision
                bool shouldStoreFile = true;

                string path = correctHashPath;
                //If file with same hash already exists
                while (File.Exists(path))
                {
                    FileStream sameHashStream = File.OpenRead(Path.Combine(hash, ext));

                    //Check if file contents are the same
                    if (stream.Length == sameHashStream.Length)
                    {
                        //Reset position for re-reading
                        stream.Position = 0;
                        int b;
                        while ((b = stream.ReadByte()) == sameHashStream.ReadByte() && b != -1);

                        //EOF. Files are the same
                        if (b == -1)
                        {
                            shouldStoreFile = false;
                            break;
                        }
                    }

                    //Files are different. This is a collision
                    IncrementOneBit(ref hashBytes);
                    path = Path.Combine(hashBytes.ToBase62(), ext);
                }

                //Hash should now be unique. Copy the file (unless a file with the same content already exists).
                if (shouldStoreFile) File.Copy(filePath, path);

                //Store file content into DB
                Collection.Insert(new FileReference {
                    VirtualPath = virtualPath,
                    RealPath = path,
                    CorrectHashPath = correctHashPath
                });

                File.Delete(filePath);
            }
        }

        public static string GetFilePath(string virtualPath)
        {
            return Collection.FindById(virtualPath).RealPath;
        }

        public static FileReference UpdateReference(FileReference refr, string path)
        {
            refr.RealPath = path;
            return refr;
        }

        public static void DeleteReference(string referenceId)
        {
            FileReference reference = Collection.FindOne(referenceId);
            //Find other references pointing to the same file
            IEnumerable<FileReference> sameRealPath = Collection.Find(refr => refr.RealPath == reference.RealPath);

            //Only delete the physical file if there are no references left
            if (sameRealPath.Count() == 1)
            {
                //Find references in the same hash group
                IEnumerable<FileReference> hashGroup = Collection.Find(refr => refr.CorrectHashPath == reference.CorrectHashPath);

                if (hashGroup.Any())
                {
                    //Get max hash
                    string maxHash = null;
                    foreach (FileReference refr in hashGroup)
                    {
                        if (StringComparer.OrdinalIgnoreCase.Compare(refr.RealPath, maxHash) > 0)
                            maxHash = refr.RealPath;
                    }

                    //Replace max hash file with the deleted file
                    File.Copy(maxHash, reference.RealPath);
                    File.Delete(maxHash);

                    //Update references that uses maxHash to the new path
                    Collection.UpdateMany(refr => UpdateReference(refr, reference.RealPath), refr => refr.RealPath == maxHash );
                }
            }

            Collection.Delete(reference.VirtualPath);
        }
    }
}