using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;

namespace ArcCore.UI.SongSelection
{
    public class PackListDisplay : MonoBehaviour
    {
        private HierarchyScrollRect scrollRect;

        public void DisplayPacks()
        {
            var packsData = MainMenu.Instance.packsData;

            // List<CellDataBase> packCells = new List<CellDataBase>();
            // foreach (PackInfo packInfo in packsData.Values)
            // {
            //     packCells.Add(new PackCellData {
                    //COMPLETE THIS
            //     });
            // }
            // scrollRect.SetData(packCells);
        }

        public void SetSelectedPack(CellDataBase selectedCell)
        {
            PackCellData packData = selectedCell as PackCellData;
            //get pack name from cell

            //COMPLETE THIS
            MainMenu.Instance.songList.DisplaySongs("");
            PlayerPrefs.SetString("LastSelectedPack", "");
        }
    }
}