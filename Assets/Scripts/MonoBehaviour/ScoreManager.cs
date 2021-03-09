using ArcCore.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.MonoBehaviours
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public float MAX_VALUE = 10_000_000f;

        [HideInInspector] public int maxCombo;

        public unsafe int*
            maxPureCount,
            latePureCount,
            earlyPureCount,
            lateFarCount,
            earlyFarCount,
            lostCount;

        [HideInInspector] public float currentScore;

        public Text textUI;

        unsafe void Awake()
        {
            Instance = this;

            maxPureCount = Unsafe.New<int>();
            latePureCount = Unsafe.New<int>();
            earlyPureCount = Unsafe.New<int>();
            lateFarCount = Unsafe.New<int>();
            earlyFarCount = Unsafe.New<int>();
            lostCount = Unsafe.New<int>();
        }

        //call later
        public void UpdateScore()
        {
            unsafe
            {
                currentScore =
                    (*maxPureCount + *latePureCount + *earlyPureCount) * MAX_VALUE / maxCombo +
                    (*lateFarCount + *earlyFarCount) * MAX_VALUE / maxCombo / 2 +
                    *maxPureCount;
            }
            textUI.text = $"{(int)currentScore:D8}";
        }
    }
}