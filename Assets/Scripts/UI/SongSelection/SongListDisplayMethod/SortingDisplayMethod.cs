namespace ArcCore.UI.SongSelection
{
    public class SortingDisplayMethod : ISongListDisplayMethod
    {
        private GameObject folderPrefab;
        protected SongFolderData CreateFolder(int diff, bool isPlus)
        {
            return new SongFolderData
            {
                title = diff.ToString() + (isPlus ? "+" : ""),
                children = new List<CellDataBase>(),
                prefab = folderPrefab
            };
        }

        public List<CellDataBase> Convert(List<Level> toDisplay, GameObject folderPrefab, GameObject cellPrefab, Difficulty selectedDiff)
        {
            this.folderPrefab = folderPrefab;

            List<SongCellData> difficultyMatchSongCells = new List<SongCellData>();
            List<SongCellData> otherSongCells = new List<SongCellData>();

            if (toDisplay.Count == 0) return null;

            foreach (Level level in toDisplay)
            {
                List<Difficulty> diffList = new List<Difficulty>();

                Chart matchingChart = null;    
                foreach (Chart chart in level.Charts)
                {
                    (int diff, bool isPlus) = CcToDifficulty.Convert(chart.Constant);

                    diffList.Add(new DifficultyItem
                    {
                        difficulty = diff,
                        isPlus = isPlus,
                        diffType = chart.Difficulty
                    });

                    if (chart.Difficulty.Precedence == selectedDiff.Precedence) {
                        matchingChart = chart;
                    }
                }

                if (matchingChart != null)
                {
                    difficultyMatchSongCells.Add(new SongCellData
                    {
                        prefab = cellPrefab,
                        level = level,
                        chartInfo = matchingChart,
                        diffList = diffList
                    });
                } else {
                    otherSongCells.Add(new SongCellData
                    {
                        prefab = cellPrefab,
                        level = level,
                        chartInfo = level.GetClosestDifficulty(selectedDiff),
                        diffList = diffList
                    });
                }
            }

            List<CellDataBase> result = SortCells(difficultyMatchSongCells);
            List<CellDataBase> otherDifficulties = SortCells(otherSongCells);

            SongFolderData otherDifficultiesFolder = new SongFolderData
            {
                title = "Other Difficulties",
                children = otherDifficulties,
                prefab = folderPrefab
            };
            result.Add(otherDifficultiesFolder);


            return result;
        }
    }
}