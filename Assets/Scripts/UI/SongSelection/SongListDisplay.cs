using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;

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

            List<LevelInfoInternal> toDisplay = new List<LevelInfoInternal>();

            if (pack == "")
            {
                toDisplay = new List<LevelInfoInternal>(levelsData.Values);
            }
            else
            {
                foreach (string song in packsData.Keys)
                {
                    toDisplay.Add(levelsData[song]);
                }
            }

            //TODO: Fetch correct difficulty
            float prioritizedDifficulty = 2;
            List<CellDataBase> displayCells = displayMethod.FromSongList(toDisplay, cellPrefab, folderPrefab, prioritizedDifficulty);
            scrollRect.SetData(displayCells);
        }

        public void SetSelectedSong(CellDataBase selectedCell)
        {
            SongCellData songData = selectedCell as SongCellData;
            string name = songData.chartInfo.songInfoOverride.name;
            SetSelectedSong(name);
        }

        public void SetSelectedSong(string song)
        {
            //TODO: COMPLETE THIS
            PlayerPrefs.SetString("LastSelectedSong", song);
        }
    }
}