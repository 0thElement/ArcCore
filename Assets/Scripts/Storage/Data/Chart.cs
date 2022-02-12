using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.Storage.Data
{
    public class Chart
    {
        public DifficultyGroup DifficultyGroup { get; set; }
        public Difficulty Difficulty { get; set; }

        public string SongPath { get; set; }
        public string ImagePath { get; set; }

        public string Name { get; set; }
        public string NameRomanized { get; set; }
        public string Artist { get; set; }
        public string ArtistRomanized { get; set; }

        public string Bpm { get; set; }
        public float Constant { get; set; }

        public int? PbScore { get; set; }
        public ScoreCategory PbGrade { get; set; }
        
        public string Illustrator { get; set; }
        public ChartSettings Settings { get; set; }

        public string Charter { get; set; }
        public string Illustrator { get; set; }

        public string Background { get; set; }
        public Style Style { get; set; }

        public string ChartPath { get; set; }
        public string Source { get; set; }
        public string SourceUrl { get; set; }
        public int PreviewStart { get; set; }
        public int PreviewEnd { get; set; }

        public void TryApplyReferences(List<string> availableAssets, out string missing)
        {
            if (!availableAssets.Contains(SongPath)) { missing = SongPath; return; }
            if (!availableAssets.Contains(ImagePath)) { missing = ImagePath; return; }
            if (!availableAssets.Contains(ChartPath)) { missing = ChartPath; return; }
            if (!availableAssets.Contains(Background)) { missing = Background; return; }
            missing = null;
        }
    }
}