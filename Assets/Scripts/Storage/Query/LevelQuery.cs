using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace ArcCore.Storage.Data
{
    public static class PackQuery
    {
        private static ILiteCollection<Pack> collection;
        private static ILiteCollection<Pack> Collection 
        {
            get
            {
                if (collection == null) Database.Current.GetCollection<Pack>();
                return collection;
            }
        }

        public static Pack Get(int id)
        {
            return Collection.FindById("Id");
        }

        public static IEnumerable<Pack> List()
        {
            return Collection.FindAll();
        }

        public static void Import(this Pack Pack)
        {
            //Find Packs with conflicting external id
            IEnumerable<Pack> conflictingPacks = Collection.Find(Query.EQ("ExternalId", Pack.ExternalId));

            //Conflict found
            if (conflictingPacks.Any())
            {
                //TODO: Notify users
                // ConflictNotifier.Notify(conflictingPacks, Update, Insert);
            }
            else
                Collection.Insert(Pack);
        }

        public static void Update(this Pack Pack, Pack newPack)
        {
            newPack.Id = Pack.Id;
            Collection.Update(newPack);
        }

        public static void Insert(this Pack Pack)
        {
            Collection.Insert(Pack);
        }
        public static void Delete(this Pack pack)
        {
            foreach (string refr in pack.FileReferences)
                FileStorage.DeleteReference(refr);

            Collection.Delete(pack.Id);
        }
    }
}