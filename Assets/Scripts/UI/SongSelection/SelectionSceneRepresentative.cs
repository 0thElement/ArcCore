using ArcCore.Scenes;

namespace ArcCore.UI.SongSelection
{
    public class SelectionSceneRepresentative : SceneRepresentative
    {
        public override void OnSceneLoad()
        {
            SelectionMenu.Instance.Display();
        }
    }
}