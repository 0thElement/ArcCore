using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.Storage.Data
{
    public static class LevelQuery
    {
        private static ILiteCollection<Level> collection;
        private static ILiteCollection<Level> Collection => Database.Current.GetCollection<Level>();

        public static Level Get(int id)
        {
            Level level = Collection.FindById(id);
            if (level.PackId == -1)
                level.Pack = null;
            else
                level.Pack = PackQuery.Get(level.PackId);
            return level;
        }

        public static IEnumerable<Level> List()
        {
            IEnumerable<Level> levels = Collection.FindAll();
            foreach(Level level in levels)
            {
                if (level.PackId == -1)
                    level.Pack = null;
                else
                    level.Pack = PackQuery.Get(level.PackId);
            }
            return levels;
        }

        public static void Clear()
        {
            Collection.DeleteAll();
        }
    }
}