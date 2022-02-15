using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace ArcCore.Scenes
{
    public class SceneRepresentative : MonoBehaviour
    {
        private void Awake()
        {
            StartCoroutine(EndOfFrame(OnSceneLoad));
            if (SceneTransitionManager.Instance == null)
            {
                StartCoroutine(EndOfFrame(OnNoBootScene));
                StartCoroutine(EndOfFrame(() => SceneManager.LoadSceneAsync(SceneNames.bootScene, LoadSceneMode.Additive)));
                SceneTransitionManager.StartBootSceneDev(this);
                return;
            }
            SceneTransitionManager.Instance.LoadSceneComplete(this);
            SceneTransitionManager.Instance.OnShutterOpen += OnShutterOpen;
        }

        protected IEnumerator EndOfFrame(Action action)
        {
            yield return new WaitForEndOfFrame();
            action.Invoke();
        }

        public virtual void OnUnloadScene() {}
        protected virtual void OnShutterOpen() {}
        protected virtual void OnSceneLoad() {}
        protected virtual void OnNoBootScene() {}
    }
}
