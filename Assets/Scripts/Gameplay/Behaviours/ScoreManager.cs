using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

//- Header for unity files -//
    using hidden = UnityEngine.HideInInspector;
    using serialized = UnityEngine.SerializeField;

namespace ArcCore.Gameplay.Behaviours
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public const float MaxScore = 10_000_000f;
        public float MaxScoreDyn => MaxScore + maxCombo;

        [hidden] public int
            maxPureCount,
            latePureCount,
            earlyPureCount,
            lateFarCount,
            earlyFarCount,
            lostCount,
            currentCombo;

        [hidden] public float currentScore, currentScoreDisplay;
        [hidden] public int maxCombo;

        public Text comboTextUI, scoreTextUI;

        void Awake()
        {
            Instance = this;
        }

        public void ResetScores()
        {
            currentScore = 0;
            currentScoreDisplay = 0;
        }

        //call later
        public void UpdateScore()
        {
            currentScore =
                (maxPureCount + latePureCount + earlyPureCount) * MaxScore / maxCombo +
                (lateFarCount + earlyFarCount) * MaxScore / maxCombo / 2 +
                 maxPureCount;
            currentScoreDisplay += math.ceil((currentScore - currentScoreDisplay) / 1.8f);

            comboTextUI.text = $"{currentCombo}";
            scoreTextUI.text = $"{(int)currentScoreDisplay:D8}";
        }

        public void AddJudge(JudgeType type, int cnt = 1)
        {
            switch(type)
            {
                case JudgeType.EarlyFar:
                    earlyFarCount += cnt;
                    currentCombo += cnt;
                    break;

                case JudgeType.EarlyPure:
                    earlyPureCount += cnt;
                    currentCombo += cnt;
                    break;

                case JudgeType.MaxPure:
                    maxPureCount += cnt;
                    currentCombo += cnt;
                    break;

                case JudgeType.LateFar:
                    lateFarCount += cnt;
                    currentCombo += cnt;
                    break;

                case JudgeType.LatePure:
                    latePureCount += cnt;
                    currentCombo += cnt;
                    break;

                case JudgeType.Lost:
                    lostCount += cnt;
                    currentCombo = 0;
                    break;
            }
        }
    }
}