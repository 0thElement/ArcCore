using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;
using System.Linq;
using ArcCore.Utitlities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByDifficultyDisplayMethod : ISongListDisplayMethod
    {
        public List<CellDataBase> FromSongList(List<LevelInfoInternal> toDisplay, GameObject folderPrefab, GameObject cellPrefab, float prioritizedDifficulty)
        {
            List<SongCellData> cellDataList = new List<SongCellData>();
            List<DifficultyItem> diffList = new List<DifficultyItem>();

            foreach (LevelInfoInternal level in toDisplay)
            {

                foreach (ChartInfo chart in level.charts)
                {
                    (int diff, bool isPlus) = CcToDifficulty.Convert(chart.cc);

                    diffList.Add(new DifficultyItem {
                        difficulty = diff,
                        isPlus = isPlus,
                        diffType = chart.diffType
                    });
                }

                cellDataList.Add(new SongCellData {
                    prefab = cellPrefab,
                    chartInfo = level.GetClosestDifficulty(prioritizedDifficulty),
                    diffList = diffList
                });
            }

            //yes
            return cellDataList
                        .OrderBy(cell => {
                            float cc = cell.chartInfo.cc;
                            (int diff, bool isPlus) = CcToDifficulty.Convert(cc);
                            return (isPlus ? diff + 0.1f : diff);
                        })
                        .ThenBy(cell => cell.chartInfo.songInfoOverride.name)
                        .Cast<CellDataBase>().ToList();
        }
    }
}