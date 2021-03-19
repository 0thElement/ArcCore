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

        public int
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

        public void AddJudge(JudgeManage.JudgeType type)
        {
            switch(type)
            {
                case JudgeManage.JudgeType.LOST:
                    lostCount++;
                    currentCombo = 0;
                    break;
                case JudgeManage.JudgeType.MAX_PURE:
                    maxPureCount++;
                    currentCombo++;
                    break;
                case JudgeManage.JudgeType.LATE_PURE:
                    latePureCount++;
                    currentCombo++;
                    break;
                case JudgeManage.JudgeType.EARLY_PURE:
                    earlyPureCount++;
                    currentCombo++;
                    break;
                case JudgeManage.JudgeType.LATE_FAR:
                    lateFarCount++;
                    currentCombo++;
                    break;
                case JudgeManage.JudgeType.EARLY_FAR:
                    earlyFarCount++;
                    currentCombo++;
                    break;
            }
        }

        //call later
        public void UpdateScore()
        {
            currentScore =
                (maxPureCount + latePureCount + earlyPureCount) * MAX_VALUE / maxCombo +
                (lateFarCount + earlyFarCount) * MAX_VALUE / maxCombo / 2 +
                 maxPureCount;
            textUI.text = $"{(int)currentScore:D8}";
        }
    }
}