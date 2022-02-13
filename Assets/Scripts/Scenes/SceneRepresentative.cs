using UnityEngine;

namespace ArcCore.Scenes
{
    public abstract class SceneRepresentative : MonoBehaviour
    {
        private void Awake()
        {
            SceneTransitionManager.Instance.LoadSceneComplete(this);
            SceneTransitionManager.Instance.OnShutterOpen += OnShutterOpen;
        }

        public virtual void OnUnloadScene() {}
        protected virtual void OnShutterOpen() {}
    }
}
