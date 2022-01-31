using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.Storage.Data
{
    public static class LevelQuery
    {
        private static ILiteCollection<Level> collection;
        private static ILiteCollection<Level> Collection 
        {
            get
            {
                if (collection == null) Database.Current.GetCollection<Level>();
                return collection;
            }
        }

        public static Level Get(int id)
        {
            return Collection.FindById("Id");
        }

        public static IEnumerable<Level> List()
        {
            IEnumerable<Level> levels = Collection.FindAll();
            foreach(Level level in levels)
            {
                level.Pack = PackQuery.Get(level.PackId);
            }
            return levels;
        }

        public static void Import(this Level level)
        {
            //Find levels with conflicting external id
            IEnumerable<Level> conflictingLevels = Collection.Find(Query.EQ("ExternalId", level.ExternalId));

            //Conflict found
            if (conflictingLevels.Any())
            {
                //TODO: Notify users
                // ConflictNotifier.Notify(conflictingLevels, Update, Insert);
            }
            else
                Collection.Insert(level);
        }

        public static void Update(this Level level, Level newLevel)
        {
            newLevel.Id = level.Id;
            Collection.Update(newLevel);
        }

        public static void Delete(this Level level)
        {
            foreach (string refr in level.FileReferences)
                FileStorage.DeleteReference(refr);

            Collection.Delete(level.Id);
        }
    }
}