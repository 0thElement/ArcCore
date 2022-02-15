using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.Gameplay.Behaviours
{
    public class PlayPauseHandler : MonoBehaviour
    {
        public const int MaximumResumeDelayHint = 10000;

        private bool hintAvailable;

        public RectTransform resumeProgressBar;
        public RectTransform hintProgressBar;
        public Text resumeProgressText;
        public Canvas pauseCanvas;

        private float maxProgressSize;

        private void Awake()
        {
            hintAvailable = true;
            maxProgressSize = resumeProgressBar.sizeDelta.x;
        }

        private void SetResumeProgress(float value, float duration)
        {
        }

        public void OnPause()
        {
            pauseCanvas.gameObject.SetActive(true);
            PlayManager.Pause();
        }

        public void OnResume()
        {
            float delay = (float)Conductor.StartPlayOffset;
            SetResumeProgress(0, delay);

            resumeProgressBar.gameObject.SetActive(true);
            resumeProgressText.gameObject.SetActive(true);
            hintProgressBar.gameObject.SetActive(true);

            resumeProgressBar.sizeDelta = new Vector2(0, resumeProgressBar.sizeDelta.y);
            LeanTween.size(resumeProgressBar, new Vector2(maxProgressSize, resumeProgressBar.sizeDelta.y), 0.2f)
                     .setOnComplete(() => {
                         LeanTween.size(resumeProgressBar, new Vector2(0, resumeProgressBar.sizeDelta.y), delay - 0.2f)
                            .setOnComplete(() => resumeProgressBar.gameObject.SetActive(false));
                     });

            LeanTween.value(gameObject, 0, delay, delay)
                .setOnUpdate((float value) => { resumeProgressText.text = $"{(delay - value):F1}"; })
                .setOnComplete(() => resumeProgressText.gameObject.SetActive(false));

            if (hintAvailable)
            {
                int timing = PlayManager.GetResumeTimingHint();
                if (timing < MaximumResumeDelayHint)
                {
                    timing = System.Math.Max(0, timing);

                    float hintDelay = delay + timing / 1000f;
                    float size = maxProgressSize * hintDelay / delay;

                    hintProgressBar.sizeDelta = new Vector2(0, hintProgressBar.sizeDelta.y);
                    LeanTween.size(hintProgressBar, new Vector2(size, hintProgressBar.sizeDelta.y), 0.2f)
                            .setOnComplete(() => {
                                LeanTween.size(hintProgressBar, new Vector2(0, hintProgressBar.sizeDelta.y), hintDelay - 0.2f)
                                    .setOnComplete(() => hintProgressBar.gameObject.SetActive(false));
                            });

                    // hintAvailable = false;
                }
            }

            pauseCanvas.gameObject.SetActive(false);
            PlayManager.Resume();
        }

        public void OnRestart()
        {
            pauseCanvas.gameObject.SetActive(false);
            PlayManager.Restart();
        }

        public void OnQuit()
        {
            PlayManager.Quit();
        }
    }
}