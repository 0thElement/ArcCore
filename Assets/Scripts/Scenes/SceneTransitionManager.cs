using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using ArcCore.Storage.Data;

namespace ArcCore.Scenes
{
    public enum TransitionState
    {
        Opening,
        Closing,
        ReadyToOpen,
        Waiting,
    }

    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private GameObject shutterCanvasObject;
        [SerializeField] private Animator shutterAnimator;
        [SerializeField] private Text loadingProgress;

        // Durations
        [SerializeField] private float shutterInDuration = 0.5f;
        [SerializeField] private float shutterOutDuration = 0.5f;
        [SerializeField] private float afterShutterInMinimumDuration = 0.5f;
        [SerializeField] private float afterShutterInInfoMinimumDuration = 4f;
        [SerializeField] private float loadingProgressFadeInDelay = 1f;
        [SerializeField] private float loadingProgressFadeInInfoDelay = 2f;

        // Shutter info
        [SerializeField] private Image jacketArt;
        [SerializeField] private Text title;
        [SerializeField] private Text artist;
        [SerializeField] private Text illustrator;
        [SerializeField] private GameObject illustratorLabel;
        [SerializeField] private Text charter;
        [SerializeField] private GameObject charterLabel;

        #region Delegates
        public delegate void OnShutterCloseDelegate();
        public OnShutterCloseDelegate OnShutterClose;
        public delegate void OnShutterOpenDelegate();
        public OnShutterOpenDelegate OnShutterOpen;

        private void OnShutterCloseCallback()
        {
            OnShutterClose?.Invoke();
            OnShutterClose = null;
        }
        
        private void OnShutterOpenCallback()
        {
            OnShutterOpen?.Invoke();
            OnShutterOpen = null;
        }
        #endregion

        #region Shutter
        private TransitionState transitionState = TransitionState.Opening;

        private void FadeInProgressText(float delay)
        {
            loadingProgress.text = "Loading...";
            loadingProgress.gameObject.SetActive(true);
            loadingProgress.color = new Color(1, 1, 1, 0);

            LeanTween.value(loadingProgress.gameObject,
                            (Color val) => loadingProgress.color = val,
                            new Color(1, 1, 1, 0),
                            new Color(1, 1, 1, 1),
                            shutterInDuration)
                     .setDelay(delay);
        }

        private void FadeOutProgressText()
        {
            LeanTween.value(loadingProgress.gameObject,
                            (Color val) => loadingProgress.color = val,
                            loadingProgress.color,
                            new Color(1, 1, 1, 0),
                            shutterOutDuration)
                    .setOnComplete(() => loadingProgress.gameObject.SetActive(false));
        }

        private IEnumerator CloseShutterCoroutine(bool showInfo = false)
        {
            transitionState = TransitionState.Closing;
            shutterCanvasObject.SetActive(true);

            string clip = showInfo ? "Base Layer.ShutterInInfo" : "Base Layer.ShutterIn";
            float waitDuration = showInfo ? afterShutterInInfoMinimumDuration : afterShutterInMinimumDuration;

            shutterAnimator.Play(clip, 0, 0);
            FadeInProgressText(showInfo ? loadingProgressFadeInInfoDelay : loadingProgressFadeInDelay);
            yield return new WaitForSecondsRealtime(shutterInDuration);

            transitionState = TransitionState.Waiting;

            OnShutterCloseCallback();
            yield return new WaitForSecondsRealtime(waitDuration);

            StartCoroutine(OpenShutterCoroutine(showInfo));
        }

        private IEnumerator OpenShutterCoroutine(bool showInfo = false)
        {
            while (transitionState != TransitionState.ReadyToOpen) yield return null;
            transitionState = TransitionState.Opening;

            string clip = showInfo ? "Base Layer.ShutterOutInfo" : "Base Layer.ShutterOut";

            shutterAnimator.Play(clip, 0, 0);
            FadeOutProgressText();
            yield return new WaitForSecondsRealtime(shutterOutDuration);
            OnShutterOpenCallback();
            shutterCanvasObject.SetActive(false);
        }

        private void SetInfo(Chart chart)
        {
            title.text = chart.Name;
            artist.text = chart.Artist;
            illustrator.text = chart.Illustrator;
            charter.text = chart.Charter;

            illustratorLabel.SetActive(chart.Illustrator != null);
            charterLabel.SetActive(chart.Charter != null);
        }

        public void SkipWaiting()
        {
            if (transitionState != TransitionState.ReadyToOpen) return;
            StopAllCoroutines();
            StartCoroutine(OpenShutterCoroutine());
        }
        #endregion

        #region Scene Management
        private Action<SceneRepresentative> passDataToNewScene;
        private Action<PlayResult> onPlayResult;
        private SceneRepresentative currentSceneRepresentative;
        private string currentScene;
        private string loadingScene;

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!load.isDone)
            {
                loadingProgress.text = $"Loading Scene - {load.progress * 100:F2}%";
                yield return null;
            }
        }

        public void LoadSceneComplete(SceneRepresentative rep)
        {
            passDataToNewScene?.Invoke(rep);

            currentSceneRepresentative.OnUnloadScene();
            SceneManager.UnloadSceneAsync(currentScene);

            currentScene = loadingScene;
            currentSceneRepresentative = rep;

            transitionState = TransitionState.ReadyToOpen;
            loadingProgress.text = "$Click to start";
        }

        public void SwitchScene(string sceneName, Action<SceneRepresentative> passData = null)
        {
            this.passDataToNewScene = passData;
            this.loadingScene = sceneName;

            OnShutterClose = () => {
                StartCoroutine(LoadSceneCoroutine(sceneName));
            };
            StartCoroutine(CloseShutterCoroutine());
        }

        public void SwitchToPlayScene(Chart chart, Action<PlayResult> onPlayResult, Action<SceneRepresentative> passData = null)
        {

            this.passDataToNewScene = passData;
            this.loadingScene = SceneNames.playScene;
            this.onPlayResult = onPlayResult;

            OnShutterClose = () => {
                StartCoroutine(LoadSceneCoroutine(SceneNames.playScene));
            };

            SetInfo(chart);
            StartCoroutine(CloseShutterCoroutine(true));
        }

        public void ReturnFromResultScene(PlayResult result)
        {
            onPlayResult.Invoke(result);
        }
        #endregion
    }
}