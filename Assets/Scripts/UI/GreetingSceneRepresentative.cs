using ArcCore.Scenes;

namespace ArcCore.UI
{
    public class GreetingSceneRepresentative : SceneRepresentative
    {
        public void StartGame()
        {
            SceneTransitionManager.Instance.SwitchScene(SceneNames.selectionScene);
        }
    }
}