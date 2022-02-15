using ArcCore.Scenes;
using ArcCore.Storage.Data;
using UnityEngine;

namespace ArcCore.Gameplay
{
    public class PlaySceneRepresentative : SceneRepresentative
    {
        public void LoadChart(Level level, Chart chart)
        {
            StartCoroutine(EndOfFrame(() => PlayManager.ApplyChart(level, chart)));
        }

        protected override void OnShutterOpen()
        {
            PlayManager.ReadyToPlay = true;
        }

        protected override void OnNoBootScene()
        {
            PlayManager.LoadDefaultChart();
            PlayManager.PlayMusic();
        }
    }
}