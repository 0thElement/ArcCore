using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;

namespace ArcCore.UI.SongSelection
{
    public class SongListDisplay : MonoBehaviour
    {
        [SerializeField] private HierarchyScrollRect scrollRect;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject folderPrefab;
        //Image selectedJacket
        //Text selectedTitle
        //Text selectedArtist
        //Text selectedBpm
        //Text selectedScore
        //Text selectedGrade
        //Text selectedDifficulty

        private ISongListDisplayMethod displayMethod = new SortByDifficultyDisplayMethod();

        public void DisplaySongs(string pack = "")
        {
            var levelsData = MainMenu.Instance.levelsData;
            var packsData  = MainMenu.Instance.packsData;

            List<LevelInfoInternal> toDisplay;

            if (pack == "")
            {
                toDisplay = levelsData.Values.ToList();
            }
            else
            {
                foreach (string song in packsData.songList)
                {
                    toDisplay.Add(levelsData[song]);
                }
            }

            List<CellDataBase> displayCells = displayMethod.FromSongList(toDisplay);
            scrollRect.SetData(displayCells);
        }

        public void SetSelectedSong(CellDataBase selectedCell)
        {
            SongCellData songData = selectedCell as SongCellData;

            //set song info to song info panel

            PlayerPrefs.SetString("LastSelectedSong", songData.chartInfo.songInfo.name);
        }
    }
}