using System.IO;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

namespace ArcCore.Storage.Data
{
    public class Pack : IArccoreInfo
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public List<string> FileReferences { get; set; }
        [BsonIgnore] public IList<FileReference> FileMapping { get; set; }
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string NameRomanized { get; set; }

        public List<string> TryApplyReferences(List<string> availableAssets, out string missing)
        {
            if (!availableAssets.Contains(ImagePath)) { missing = ImagePath; return null; }
            missing = null;
            List<string> list = new List<string>{ImagePath};
            FileReferences = list;
            return list;
        }
        public string VirtualPathPrefix()
        {
            return Path.Combine(FileStatics.Pack, Id.ToString());
        }

        public string ExternalIdentifier()
        {
            return ExternalId;
        }

        public int Insert()
        {
            return Database.Current.GetCollection<Pack>().Insert(this);
        }

        public void Delete()
        {
            foreach (string refr in FileReferences)
                FileStorage.DeleteReference(refr);
            Database.Current.GetCollection<Pack>().Delete(Id);
        }

        public int Update(IArccoreInfo info)
        {
            Pack newLevel = info as Pack;
            newLevel.Id = Id;
            Delete();
            newLevel.Insert();
            return Id;
        }

        public List<IArccoreInfo> ConflictingExternalIdentifier()
        {
            return Database.Current.GetCollection<Pack>().Find(l => l.ExternalId == this.ExternalId)
                                   .ToList<IArccoreInfo>();
        }
    }
}