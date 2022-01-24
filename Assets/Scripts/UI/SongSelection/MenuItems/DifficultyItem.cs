using ArcCore.UI.Data;
using ArcCore.Utilities;

namespace ArcCore.UI.SongSelection
{
    public class DifficultyItem
    {
        public int internalDiff;
        public bool isPlus; 
        public Difficulty displayDiff;
        public DifficultyGroup diffGroup;
        public string Text => (displayDiff == null ? internalDiff.ToString() : displayDiff.Name);
        public bool IsPlus => (displayDiff == null ? isPlus : displayDiff.IsPlus);

        public DifficultyItem(Chart chart)
        {
            (internalDiff, isPlus) = CcToDifficulty.Convert(chart.Constant);
            displayDiff = chart.Difficulty;
            diffGroup = chart.DifficultyGroup;
        }
    }
}