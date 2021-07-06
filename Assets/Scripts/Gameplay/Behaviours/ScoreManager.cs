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
        /// The tracker responsible for tracking judgement.
        /// </summary>
        [HideInInspector]
        public JudgeTracker tracker = default;

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

        /// <summary>
        /// Update the score information.
        /// </summary>
        public void UpdateScore()
        {
            currentScore = tracker.Score;
            currentScoreDisplay += math.ceil((currentScore - currentScoreDisplay) / 1.8f);

            comboTextUI.text = $"{tracker.combo}";
            scoreTextUI.text = $"{(int)currentScoreDisplay:D8}";
        }
    }
}