using UnityEngine;
using UnityEngine.UI;
using ArcCore.Serialization;
using System.Collections.Generic;

namespace ArcCore.UI.SongSelection
{
    public class MainMenu : ScrollRect
    {
        public static MainMenu Instance;
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public SongListDisplay songList;
        public PackListDisplay packList;

        public Dictionary<string, LevelInfoInternal> levelsData;
        public Dictionary<string, PackInfo> packsData;

        //Load from prefence and display the pack at correct position
        public void Display()
        {
            //Get json to fill levelsData and packsData. For now from a temporary test file
            //TODO: COMPLETE THIS

            string lastLoadedPack = PlayerPrefs.GetString("LastLoadedPack");
            packList.SetSelectedPack(lastLoadedPack);

            string lastLoadedSong = PlayerPrefs.GetString("LastLoadedSong");
            songList.SetSelectedSong(lastLoadedSong);
        }
    } 
}