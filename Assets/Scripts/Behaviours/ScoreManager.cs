using ArcCore.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.Behaviours
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public const float MaxScore = 10_000_000f;
        public float MaxScoreDyn => MaxScore + maxCombo;

        [HideInInspector] public int maxCombo;
        [HideInInspector] public int
            maxPureCount,
            latePureCount,
            earlyPureCount,
            lateFarCount,
            earlyFarCount,
            lostCount,
            currentCombo;
        [HideInInspector] public float currentScore;

        public Text textUI;

        void Awake()
        {
            Instance = this;
        }

        //call later
        public void UpdateScore()
        {
            currentScore =
                (maxPureCount + latePureCount + earlyPureCount) * MaxScore / maxCombo +
                (lateFarCount + earlyFarCount) * MaxScore / maxCombo / 2 +
                 maxPureCount;
            textUI.text = $"{(int)currentScore:D8}";
        }
    }
}