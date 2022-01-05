using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.UI;
using System.Linq;
using ArcCore.Utitlities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByDifficultyDisplayMethod : SortingDisplayMethod
    {
        public List<CellDataBase> SortCells(List<SongCellData> songCellDataList)
        {
            List<CellDataBase> folderCellDataList = new List<CellDataBase>();

            songCellDataList = songCellDataList
                        .OrderBy(cell =>
                        {
                            float cc = cell.chartInfo.cc;
                            (int diff, bool isPlus) = CcToDifficulty.Convert(cc);
                            return (isPlus ? diff + 0.1f : diff);
                        })
                        .ThenBy(cell => cell.chartInfo.songInfoOverride.name)
                        .ToList();
            
            //Sort to folders
            (int cdiff, bool cisPlus) = CcToDifficulty.Convert(songCellDataList[0].chartInfo.Constant);
            folderCellDataList.Add(CreateFolder(cdiff, cisPlus, folderPrefab));

            foreach (SongCellData song in songCellDataList)
            {
                (int diff, bool isPlus) = CcToDifficulty.Convert(songCellDataList[0].chartInfo.cc);
                if (diff != cdiff || cisPlus != isPlus)
                {
                    folderCellDataList.Add(CreateFolder(diff, isPlus, folderPrefab));
                }
                folderCellDataList[folderCellDataList.Count - 1].children.Add(song);
            }
            return folderCellDataList;
        }
    }
}