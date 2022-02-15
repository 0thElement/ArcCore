using ArcCore.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class ScoreHandler : MonoBehaviour
    {
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
        public Text scoreFirst5TextUI;
        public Text scoreLast3TextUI;

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
        public void Update()
        {
            currentScore = tracker.Score;
            currentScoreDisplay += math.ceil((currentScore - currentScoreDisplay) * Time.deltaTime * 10);

            int first2digits = (int)currentScoreDisplay / 1000000;
            int middle3digits = ((int)currentScoreDisplay % 1000000) / 1000;
            int last3digits = (int)currentScoreDisplay % 1000;

            comboTextUI.text = $"{tracker.combo}";
            scoreFirst5TextUI.text = $"{first2digits:D2}'{middle3digits:D3}";
            scoreLast3TextUI.text = $"'{last3digits:D3}";
        }
    }
}