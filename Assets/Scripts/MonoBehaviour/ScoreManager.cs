using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.MonoBehaviours
{
    public struct JudgeCounts {

        public int maxPureCount;
        public int latePureCount;
        public int earlyPureCount;
        public int lateFarCount;
        public int earlyFarCount;
        public int lostCount;

        public JudgeCounts(int maxPureCount, int latePureCount, int earlyPureCount, int lateFarCount, int earlyFarCount, int lostCount)
        {
            this.maxPureCount = maxPureCount;
            this.latePureCount = latePureCount;
            this.earlyPureCount = earlyPureCount;
            this.lateFarCount = lateFarCount;
            this.earlyFarCount = earlyFarCount;
            this.lostCount = lostCount;
        }
    }
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public float MAX_VALUE = 10_000_000f;

        [HideInInspector] public int maxCombo;

        public unsafe JudgeCounts* judgeCounts;

        [HideInInspector] public float currentScore;

        public Text textUI;

        void Awake()
        {
            Instance = this;
            unsafe
            {
                JudgeCounts judgeCountsData = new JudgeCounts();
                judgeCounts = &judgeCountsData;
            }
        }

        //call later
        public void UpdateScore()
        {
            unsafe
            {
                currentScore =
                    (judgeCounts->maxPureCount + judgeCounts->latePureCount + judgeCounts->earlyPureCount) * MAX_VALUE / maxCombo +
                    (judgeCounts->lateFarCount + judgeCounts->earlyFarCount) * MAX_VALUE / maxCombo / 2 +
                    judgeCounts->maxPureCount;
            }
            textUI.text = $"{(int)currentScore:D8}";
        }
    }
}