using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;
using System.Linq;
using ArcCore.Utitlities;

namespace ArcCore.UI.SongSelection
{
    public class SortByDifficultyDisplayMethod : ISongListDisplayMethod
    {
        public List<CellDataBase> FromSongList(List<LevelInfoInternal> toDisplay, GameObject folderPrefab, GameObject cellPrefab, float prioritizedDifficulty)
        {
            List<SongCellData> cellDataList = new List<SongCellData>();
            List<DifficultyItem> diffList = new List<DifficultyItem>();

            foreach (ChartInfo chart in charts)
            {
                (int diff, bool isPlus) = CcToDifficulty.Convert(chart.cc);

                diffList.Add(new DifficultyItem {
                    difficulty = diff,
                    isPlus = isPlus,
                    diffType = chart.diffType
                });
            }

            foreach (LevelInfoInternal level in sorted)
            {
                cellDataList.Add(new SongCellData {
                    cellPrefab = cellPrefab,
                    chartInfo = level.GetClosestDifficulty(prioritizedDifficulty),
                    diffList = diffList
                });
            }

            //yes
            return cellDataList
                        .OrderBy(cell => {
                            float cc = cell.chartInfo.cc;
                            (int diff, bool isPlus) = CcToDifficulty.Convert(chart.cc);
                            return (isPlus ? diff + 0.1f : diff);
                        })
                        .ThenBy(cell => cell.chartInfo.songInfo.name)
                        .ToList().Cast<CellDataBase>();
        }
    }
}