using LiteDB;

namespace ArcCore.Storage.Data
{
    public class FileReference
    {
        [BsonId] public string VirtualPath { get; set; }
        public string RealPath { get; set; }
        public string CorrectHashPath { get; set; }
    }
}