using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        /// <summary>
        /// The maximum score, not accounting for max pures, which can be acheived legally for the current chart.
        /// </summary>
        public const float MaxScore = 10_000_000f;
        /// <summary>
        /// The maximum score, accounting for max pures, which can be acheived legally for the current chart.
        /// </summary>
        public float MaxScoreDyn => MaxScore + maxCombo;

        /// <summary>
        /// The count of max pures which have been hit so far.
        /// </summary>
        [HideInInspector]
        public int maxPureCount;
        /// <summary>
        /// The count of late pures which have been hit so far.
        /// </summary>
        [HideInInspector]
        public int latePureCount;
        /// <summary>
        /// The count of early pures which have been hit so far.
        /// </summary>
        [HideInInspector]
        public int earlyPureCount;
        /// <summary>
        /// The count of late fars which have been hit so far.
        /// </summary>
        [HideInInspector]
        public int lateFarCount;
        /// <summary>
        /// The count of early fars which have been hit so far.
        /// </summary>
        [HideInInspector] 
        public int earlyFarCount;
        /// <summary>
        /// The count of losts which have been hit so far.
        /// </summary>
        [HideInInspector]
        public int lostCount;
        /// <summary>
        /// The current combo.
        /// </summary>
        [HideInInspector]
        public int currentCombo;

        /// <summary>
        /// The current score, calculated every frame.
        /// </summary>
        [HideInInspector] 
        public float currentScore;
        /// <summary>
        /// The current value of the score to be displayed, eased by an exponential algorithm.
        /// </summary>
        [HideInInspector] 
        public float currentScoreDisplay;

        /// <summary>
        /// The maximum combo legally achievable for the current chart.
        /// </summary>
        [HideInInspector] 
        public int maxCombo;

        /// <summary>
        /// The text ui element responsible for displaying the combo.
        /// </summary>
        public Text comboTextUI;
        /// <summary>
        /// The text ui element responsible for displaying the score.
        /// </summary>
        public Text scoreTextUI;

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Reset all the score values.
        /// </summary>
        public void ResetScores()
        {
            currentScore = 0;
            currentScoreDisplay = 0;
        }

        //call later
        /// <summary>
        /// Update the score information.
        /// </summary>
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

        /// <summary>
        /// Manage a given judgement.
        /// </summary>
        public void AddJudge(JudgeType type, int cnt = 1)
        {
            switch (type)
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