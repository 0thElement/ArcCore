using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ArcCore.Gameplay;

namespace ArcCore.UI
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private RectTransform shutterLeft;
        [SerializeField] private RectTransform shutterRight;

        [SerializeField] private Text loadingProgessText;

        [SerializeField] private float transitionDuration;
        [SerializeField] private LeanTweenType inTransitionType;
        [SerializeField] private LeanTweenType outTransitionType;

        [SerializeField] private Vector3 shutterLeftCornerPos;
        [SerializeField] private Vector3 shutterLeftClosedPos;
        [SerializeField] private Vector3 shutterLeftOpenPos;

        [SerializeField] private Vector3 shutterRightCornerPos;
        [SerializeField] private Vector3 shutterRightClosedPos;
        [SerializeField] private Vector3 shutterRightOpenPos;

        [SerializeField] private string startupScene;
        [SerializeField] private string selectionScene;
        [SerializeField] private string playScene;
        [SerializeField] private string songInfoScene;
        [SerializeField] private string resultScene;

        [SerializeField] private float minimumPlaySceneTransitionTime = 5f;

        private void Awake()
        {
            Instance = this;
            SceneManager.LoadScene(startupScene, LoadSceneMode.Additive);
            previousScene = startupScene;

            shutterLeft.anchoredPosition = shutterLeftOpenPos;
            shutterRight.anchoredPosition = shutterRightOpenPos;
            previousShutterState = ShutterState.Open;
        }

        private string previousScene;
        private ShutterState previousShutterState;

        private enum ShutterState
        {
            Open,
            Corner,
            Closed
        }

        #region Animations
        private bool isShutterMoving;

        private void MoveShutter(ShutterState shutterState)
        {
            isShutterMoving = true;
            shutterLeft.gameObject.SetActive(true);
            shutterRight.gameObject.SetActive(true);

            Vector3[] leftShutterPos = new Vector3[] {shutterLeftOpenPos, shutterLeftCornerPos, shutterLeftClosedPos};
            Vector3[] rightShutterPos = new Vector3[] {shutterRightOpenPos, shutterRightCornerPos, shutterRightClosedPos};

            Vector3 leftTo = leftShutterPos[(int)shutterState];
            Vector3 rightTo = rightShutterPos[(int)shutterState];

            float duration = (shutterState == previousShutterState ? 0 : transitionDuration);

            LeanTweenType transitionType = (shutterState == ShutterState.Open ? inTransitionType : outTransitionType);
            
            LeanTween.move(shutterLeft, leftTo, duration).setEase(transitionType);
            LeanTween.move(shutterRight, rightTo, duration).setEase(transitionType).setOnComplete(() => {
                if (shutterState == ShutterState.Open)
                {
                    shutterLeft.gameObject.SetActive(false);
                    shutterRight.gameObject.SetActive(false);
                }

                isShutterMoving = false;
            });

            previousShutterState = shutterState;
        }
        #endregion

        #region LoadingProgressText
        private void FadeInProgressText()
        {
            SetLoadingText("Loading");
            loadingProgessText.gameObject.SetActive(true);

            LeanTween.value(loadingProgessText.gameObject, (Color val) => loadingProgessText.color = val, new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), transitionDuration * 2)
                    .setEase(inTransitionType);
        }

        private void FadeOutProgressText()
        {
            LeanTween.value(loadingProgessText.gameObject, (Color val) => loadingProgessText.color = val, new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), transitionDuration)
                    .setEase(outTransitionType)
                    .setOnComplete(() => loadingProgessText.gameObject.SetActive(false));
        }

        public void SetLoadingText(string text)
        {
            loadingProgessText.text = text;
        }
        #endregion

        #region SceneNavigationCoroutine
        private IEnumerator ToSelectionSceneCoroutine()
        {
            MoveShutter(ShutterState.Closed);
            FadeInProgressText();
            while (isShutterMoving) yield return null;

            SetLoadingText("Loading scene");
            AsyncOperation loadSelectionScene = SceneManager.LoadSceneAsync(selectionScene, LoadSceneMode.Additive);
            AsyncOperation unloadStartupScene = SceneManager.UnloadSceneAsync(startupScene);
            while (!loadSelectionScene.isDone || !unloadStartupScene.isDone) yield return null;

            SetLoadingText("Loading song selection");
            //Selection manager setup

            MoveShutter(ShutterState.Corner);
            FadeOutProgressText();
        }

        private bool MinimumPlaySceneTransitionTimeComplete;
        private IEnumerator MinimumPlaySceneTransitionCoroutine(float waitDuration)
        {
            MinimumPlaySceneTransitionTimeComplete = false;
            yield return new WaitForSeconds(waitDuration);
            MinimumPlaySceneTransitionTimeComplete = true;
        }

        private IEnumerator ToPlaySceneCoroutine(string songInfo)
        {
            StartCoroutine(MinimumPlaySceneTransitionCoroutine(minimumPlaySceneTransitionTime));

            AsyncOperation loadSongInfoScene = SceneManager.LoadSceneAsync(songInfoScene, LoadSceneMode.Additive);
            AsyncOperation unloadSelectionScene = SceneManager.UnloadSceneAsync(selectionScene);
            while(!loadSongInfoScene.isDone || !unloadSelectionScene.isDone) yield return null;

            // PrePlayDisplay prePlayDisplay = GameObject.Find("PrePlayDisplay").GetComponent<PrePlayDisplay>();
            // prePlayDisplay.Setup(songInfo);

            MoveShutter(ShutterState.Closed);
            FadeInProgressText();
            // prePlayDisplay.FadeIn();
            while (isShutterMoving) yield return null;

            SetLoadingText("Loading scene");
            AsyncOperation loadPlayScene = SceneManager.LoadSceneAsync(playScene, LoadSceneMode.Additive);
            while (!loadPlayScene.isDone) yield return null;

            SetLoadingText("Loading chart file");
            // PlayManager playManager = GameObject.Find("PlayManager").GetComponent<PlayManager>();
            // playManager.LoadSong(songInfo);

            SetLoadingText("Ready! Click to start");
            while (!MinimumPlaySceneTransitionTimeComplete) yield return null;

            MoveShutter(ShutterState.Open);
            FadeOutProgressText();
            // prePlayDisplay.FadeOut();
            while (isShutterMoving) yield return null;

            AsyncOperation unloadSongInfoScene = SceneManager.UnloadSceneAsync(songInfoScene);
            while (!unloadSongInfoScene.isDone) yield return null;

            // playManager.StartSong();
        }

        private IEnumerator ToResultSceneCoroutine(string songInfo, string scoreTracker)
        {
            AsyncOperation loadResultScene = SceneManager.LoadSceneAsync(resultScene, LoadSceneMode.Additive);
            while (!loadResultScene.isDone) yield return null;

            MoveShutter(ShutterState.Closed);
            while (isShutterMoving) yield return null;

            // ResultDisplay resultDisplay = GameObject.Find("ResultDisplay").GetComponent<ResultDisplay>();
            // resultDisplay.FadeIn();
        }

        private IEnumerator CloseResultSceneToSelectionSceneCoroutine()
        {
            FadeInProgressText();

            SetLoadingText("Loading scene");
            AsyncOperation unloadPlayScene = SceneManager.UnloadSceneAsync(playScene);
            AsyncOperation loadSelectionScene = SceneManager.LoadSceneAsync(selectionScene, LoadSceneMode.Additive);
            while (!loadSelectionScene.isDone) yield return null;

            //Selection scene setup

            MoveShutter(ShutterState.Corner);
            FadeOutProgressText();
            // ResultDisplay resultDisplay = GameObject.Find("ResultDisplay").GetComponent<ResultDisplay>();
            // resultDisplay.FadeOut();
            while (isShutterMoving) yield return null;

            AsyncOperation unloadResultScene = SceneManager.UnloadSceneAsync(resultScene);
        }

        private IEnumerator RetryFromResultSceneCoroutine()
        {
            FadeInProgressText();

            SetLoadingText("Reloading chart file");
            // PlayManager playManager = GameObject.Find("PlayManager").GetComponent<PlayManager>();
            // playManager.Restart();

            MoveShutter(ShutterState.Open);
            FadeOutProgressText();
            // ResultDisplay resultDisplay = GameObject.Find("ResultDisplay").GetComponent<ResultDisplay>();
            // resultDisplay.FadeOut();
            while (isShutterMoving) yield return null;

            AsyncOperation unloadResultScene = SceneManager.UnloadSceneAsync(resultScene);

            // playManager.StartSong();
        }

        private IEnumerator RetryFromPlaySceneCoroutine()
        {
            FadeInProgressText();

            MoveShutter(ShutterState.Closed);
            while (isShutterMoving) yield return null;

            SetLoadingText("Reloading chart file");
            // PlayManager playManager = GameObject.Find("PlayManager").GetComponent<PlayManager>();
            // playManager.Restart();

            MoveShutter(ShutterState.Open);
            FadeOutProgressText();
            while (isShutterMoving) yield return null;

            // playManager.StartSong();
        }
        #endregion

        #region PublicMethods
        public void ToSelectionScene()
            => StartCoroutine(ToSelectionSceneCoroutine());

        public void ToPlayScene(string songInfo)
            => StartCoroutine(ToPlaySceneCoroutine(songInfo));

        public void ToResultScene(string songInfo, string scoreTracker)
            => StartCoroutine(ToResultSceneCoroutine(songInfo, scoreTracker));

        public void CloseResultSceneToSelectionScene()
            => StartCoroutine(CloseResultSceneToSelectionSceneCoroutine());

        public void RetryFromResultScene()
            => StartCoroutine(RetryFromResultSceneCoroutine());

        public void RetryFromPlayScene()
            => StartCoroutine(RetryFromPlaySceneCoroutine());

        public void SkipPlaySceneTransitionWaitTime()
            => MinimumPlaySceneTransitionTimeComplete = true;
        #endregion
    }
}