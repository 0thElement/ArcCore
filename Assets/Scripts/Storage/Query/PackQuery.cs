using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace ArcCore.Storage.Data
{
    public static class PackQuery
    {
        private static ILiteCollection<Pack> collection;
        private static ILiteCollection<Pack> Collection => Database.Current.GetCollection<Pack>();

        public static Pack Get(int id)
        {
            return Collection.FindById("Id");
        }

        public static IEnumerable<Pack> List()
        {
            return Collection.FindAll();
        }

        public static void Clear()
        {
            Collection.DeleteAll();
        }
    }
}