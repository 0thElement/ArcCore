using LiteDB;
using UnityEngine;
using ArcCore.Utilities;

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
            Current = Current ?? new LiteDatabase(path ?? FileStatics.DatabasePath);
        }

        public static void Dispose()
        {
            Database.Current?.Dispose();
        } 
    }
}