using ArcCore.Scenes;

namespace ArcCore.UI.SongSelection
{
    public class SelectionSceneRepresentative : SceneRepresentative
    {
        protected override void OnSceneLoad()
        {
            SelectionMenu.Instance.Display();
        }
    }
}