using ArcCore.Utilities;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityCoroutineUtils;

namespace ArcCore.UI
{

    public class TransitionEffect : MonoBehaviour
    {
        public static TransitionEffect Instance { get; private set; }

        public static event Action OnStartTransitionIn;
        public static event Action OnEndTransitionIn;
        public static event Action OnStartTransitionOut;
        public static event Action OnEndTransitionOut;

        public static event Action<Timer> OnMidTransitionIn;
        public static event Action<Timer> OnMidTransitionOut;

        public static bool StartTransitionIn(float timeSec = BaseTimeOfTransition)
        {
            if (InTransitionMode || IsMidTransition)
                return false;

            Instance.gameObject.SetActive(true);

            Instance.autoContinue = false;
            Instance.CTransitionIn(timeSec).Start(Instance);

            return true;
        }
        public static bool StartMiddleCoroutine(IEnumerator coroutine)
        {
            if (!InTransitionMode || IsMidTransition)
                return false;

            Instance.gameObject.SetActive(true);

            Instance.autoContinue = false;
            coroutine.Start(Instance);

            return true;
        }
        public static bool StartTransitionOut(float timeSec = BaseTimeOfTransition)
        {
            if (!InTransitionMode || IsMidTransition)
                return false;

            Instance.gameObject.SetActive(true);

            Instance.autoContinue = false;
            Instance.CTransitionOut(timeSec).Start(Instance);

            return true;
        }

        public static bool AutoTransition(IEnumerator middleRoutine = null, float timeSec = BaseTimeOfTransition)
        {
            if (InTransitionMode || IsMidTransition)
                return false;

            Instance.gameObject.SetActive(true);

            Instance.autoContinue = true;
            Instance.autoRoutine = middleRoutine;
            Instance.CTransitionIn(timeSec).Start(Instance);

            return true;
        }
        public static bool AutoTransition(Action middleRoutine, float timeSec = BaseTimeOfTransition)
            => AutoTransition(Co.action(middleRoutine), timeSec);

        public static bool IsMidTransition => Instance._isMidTransition;
        public static bool InTransitionMode => Instance._inTransitionMode;

        private bool _isMidTransition;
        private bool _inTransitionMode;

        public const float BaseTimeOfTransition = 0.5f;

        [SerializeField] private Image img;

        private IEnumerator autoRoutine;
        private bool autoContinue;

        public void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            gameObject.SetActive(false);
        }

        public void OnDestroy()
        {
            Instance = null;
        }

        private IEnumerator CTransitionIn(float timeSec) 
        => Co.sequence
        (
            Co.action(() => 
            {
                _isMidTransition = true;

                OnStartTransitionIn?.Invoke();
            }),

            Co.procedure(TimeSpan.FromSeconds(timeSec), timer => 
            {
                //Default behaviour
                img.color = new Color(img.color.r, img.color.g, img.color.b, timer.PercentComplete);
                img.enabled = true;

                //Custom behaviour
                OnMidTransitionIn?.Invoke(timer);
            }),

            Co.action(() =>
            {
                img.color = Color.black;

                _inTransitionMode = true;
                _isMidTransition = false;

                OnEndTransitionIn?.Invoke();

                if (autoContinue)
                {
                    Co.sequence
                    (
                        autoRoutine.or_none(),

                        Co.action(() => 
                        { 
                            autoRoutine = null; 
                        }),

                        CTransitionOut(timeSec)
                    )
                    .Start(this);
                }
            })
        );

        private IEnumerator CTransitionOut(float timeSec) 
        => Co.sequence
        (
            Co.action(() =>
            {
                _isMidTransition = true;
                _inTransitionMode = false;

                OnStartTransitionOut?.Invoke();
            }),

            Co.procedure(TimeSpan.FromSeconds(timeSec), timer => 
            { 
                //Default behaviour
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1 - timer.PercentComplete);
                img.enabled = true;

                //Custom behaviour
                OnMidTransitionOut?.Invoke(timer);
            }),

            Co.action(() =>
            {
                img.color = Color.clear;

                _isMidTransition = false;

                gameObject.SetActive(false);

                OnEndTransitionOut?.Invoke();
            })
        );
    }
}