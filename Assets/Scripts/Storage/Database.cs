using LiteDB;
using UnityEngine;
using ArcCore.Utilities;

namespace ArcCore.Storage
{
    public class Database
    {
        public static LiteDatabase Current { get; private set; }

        public static void Initialize()
        {
            BsonMapper.Global.RegisterType<Color>(
                serialize: (color) => color.ToHexcode(),
                deserialize: (value) => ((string)value).ToColor()
            );
            Current = new LiteDatabase(FileStatics.DatabasePath);
        }
    }
}