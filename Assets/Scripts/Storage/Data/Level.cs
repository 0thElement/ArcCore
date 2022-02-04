using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

namespace ArcCore.Storage.Data
{
    public class Level : IArccoreInfo
    {
        public int Id { get; set; }

        public string ExternalId { get; set; }
        public List<string> FileReferences { get; set; }

        public Chart[] Charts { get; set; }
        public int PackId { get; set; }
        [BsonIgnore] public string PackExternalId { get; set; }
        [BsonIgnore] public Pack Pack { get; set; }

        public Chart GetClosestChart(DifficultyGroup difficultyGroup)
        {
            Chart result = null;
            float closestDifference = float.PositiveInfinity;

            foreach (Chart chart in Charts)
            {
                float diff = Math.Abs(chart.DifficultyGroup.Precedence - difficultyGroup.Precedence);
                if (diff < closestDifference)
                {
                    result = chart;
                    closestDifference = diff;
                }
            }

            return result;
        }

        public Chart GetExactChart(DifficultyGroup difficultyGroup)
        {
            foreach (Chart chart in Charts)
            {
                if (chart.DifficultyGroup.Precedence == difficultyGroup.Precedence)
                    return chart;
            }
            return null;
        }

        public List<string> TryApplyReferences(List<string> availableAssets, out string missing)
        {
            foreach (Chart chart in Charts)
            {
                chart.TryApplyReferences(availableAssets, out string chartMissing);
                if (chartMissing != null) { missing = chartMissing; return null; }
            }
            FileReferences = availableAssets;
            missing = null;
            return FileReferences;
        }

        public string VirtualPathPrefix()
        {
            return Path.Combine(FileStatics.Level, Id.ToString());
        }

        public string ExternalIdentifier()
        {
            return ExternalId;
        }

        public int Insert()
        {
            return Id = Database.Current.GetCollection<Level>().Insert(this);
        }

        public void Delete()
        {
            foreach (string refr in FileReferences)
                FileStorage.DeleteReference(Path.Combine(VirtualPathPrefix(), refr));
            Database.Current.GetCollection<Level>().Delete(Id);
        }

        public int Update(IArccoreInfo info)
        {
            UnityEngine.Debug.Log(Id);
            foreach (string refr in FileReferences)
                FileStorage.DeleteReference(Path.Combine(VirtualPathPrefix(), refr));
            Level newLevel = info as Level;
            Database.Current.GetCollection<Level>().Update(Id, newLevel);
            return Id;
        }

        public List<IArccoreInfo> ConflictingExternalIdentifier()
        {
            return Database.Current.GetCollection<Level>().Find(l => l.ExternalId == this.ExternalId)
                                   .ToList<IArccoreInfo>();
        }
    }
}