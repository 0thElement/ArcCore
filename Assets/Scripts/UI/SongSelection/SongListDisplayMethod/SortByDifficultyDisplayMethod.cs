using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.UI;
using System.Linq;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByDifficultyDisplayMethod : SortingDisplayMethod
    {
        protected override List<CellDataBase> SortCells(List<LevelCellData> songCellDataList)
        {
            List<CellDataBase> folderCellDataList = new List<CellDataBase>();

            songCellDataList = songCellDataList
                        .OrderBy(cell =>
                        {
                            float cc = cell.chart.Constant;
                            (int diff, bool isPlus) = CcToDifficulty.Convert(cc);
                            return (isPlus ? diff + 0.1f : diff);
                        })
                        .ThenBy(cell => cell.chart.Name)
                        .ToList();
            
            //Sort to folders
            (int cdiff, bool cisPlus) = CcToDifficulty.Convert(songCellDataList[0].chart.Constant);
            folderCellDataList.Add(CreateFolder(cdiff, cisPlus));

            foreach (LevelCellData song in songCellDataList)
            {
                (int diff, bool isPlus) = CcToDifficulty.Convert(songCellDataList[0].chart.Constant);
                if (diff != cdiff || cisPlus != isPlus)
                {
                    folderCellDataList.Add(CreateFolder(diff, isPlus));
                }
                folderCellDataList[folderCellDataList.Count - 1].children.Add(song);
            }
            return folderCellDataList;
        }
    }
}