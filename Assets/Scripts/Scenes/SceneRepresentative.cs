using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcCore.Scenes
{
    public class SceneRepresentative : MonoBehaviour
    {
        private void Awake()
        {
            OnSceneLoad();
            SceneTransitionManager.Instance.LoadSceneComplete(this);
            SceneTransitionManager.Instance.OnShutterOpen += OnShutterOpen;
        }

        public virtual void OnUnloadScene() {}
        protected virtual void OnShutterOpen() {}
        public virtual void OnSceneLoad() {}
    }
}
