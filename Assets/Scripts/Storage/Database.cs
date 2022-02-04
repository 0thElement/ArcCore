using LiteDB;
using UnityEngine;
using ArcCore.Utilities;
using System.IO;

namespace ArcCore.Storage
{
    public class Database
    {
        public static LiteDatabase Current { get; private set; }

        public static void Initialize(string path = null)
        {
            BsonMapper.Global.RegisterType<Color>(
                serialize: (color) => color.ToHexcode(),
                deserialize: (value) => ((string)value).ToColor()
            );
            path = path ?? FileStatics.DatabasePath;
            if (Current == null)
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                Current = new LiteDatabase(path);
            }
        }

        public static void Dispose()
        {
            Database.Current?.Dispose();
            Current = null;
        } 

        public static void Clear()
        {
            foreach (string colNames in Current.GetCollectionNames())
            {
                Current.DropCollection(colNames);
            }
        }
    }
}