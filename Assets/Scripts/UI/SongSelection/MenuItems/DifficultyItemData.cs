using ArcCore.UI.Data;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class DifficultyItemData
    {
        private int internalDiff;
        private bool isPlus; 
        private Difficulty displayDiff;
        private DifficultyGroup diffGroup;
        public string Text => (displayDiff == null ? internalDiff.ToString() : displayDiff.Name);
        public bool IsPlus => (displayDiff == null ? isPlus : displayDiff.IsPlus);
        public Color Color => diffGroup.Color;

        public DifficultyItemData(Chart chart)
        {
            (internalDiff, isPlus) = Conversion.CcToDifficulty(chart.Constant);
            displayDiff = chart.Difficulty;
            diffGroup = chart.DifficultyGroup;
        }
    }
}