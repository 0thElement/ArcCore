using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArcCore.UI.Result;

namespace ArcCore.UI.Result
{
    [DefaultExecutionOrder(1)]
    public class ResultDisplay : MonoBehaviour
    {
        [Header("Manager")]
        public ResultManager resultManager;

        [Header("Canvas")]
        public Text songNameText;
        public Text songComposerText;

        public Image characterImage;

        public Text maxRecallValue;
        public Text difficultyText; // or Image difficultyImage
        public Text resultTitleText; // or Image resultTitleImage

        public Text resultScoreText;
        public Text highScoreText;
        public Text highScoreDifferenceText;

        public Text resultRankText; // or Image resultRankImage
        public Text pureValue;
        public Text farValue;
        public Text lostValue;

        public Text maxPureText;
        public Text farTypeText;

        public Image songCoverImage;

        private void Start()
        {
            songNameText.text = "Debug_Song"; // resultManager.songInfoOverride.name // get song name
            songComposerText.text = "Debug_Composer"; // resultManager.songInfoOverride.artist // get composer name

            // characterImage.sprite = /* user character image */

            maxRecallValue.text = resultManager.longestCombo.ToString();
            difficultyText.text = resultManager.chartInfo.diffType.fullName; // + " " + difficultyNumber;
            resultTitleText.text = ResultTitleLabel(resultManager.JudgeTitle());

            resultScoreText.text = resultManager.resultScore.ToString("00,000,000").Replace(",", "'");
            highScoreText.text = resultManager.highScore.ToString("00,000,000").Replace(",", "'");
            highScoreDifferenceText.text = ((resultManager.DifferenceFromHighScore() < 0) ? "-" : "+") + resultManager.DifferenceFromHighScore().ToString("00,000,000").Replace(",", "'");

            resultRankText.text = resultManager.JudgeRank();
            pureValue.text = resultManager.SumOfPure().ToString();
            farValue.text = resultManager.SumOfFar().ToString();
            lostValue.text = resultManager.lostCount.ToString();

            maxPureText.text = "+" + resultManager.maxPureCount.ToString();
            farTypeText.text = "L" + resultManager.lateFarCount.ToString() + " E" + resultManager.earlyFarCount.ToString();
        }

        // Update is called once per frame
        private void Update()
        {

        }
        
        private string ResultTitleLabel(string title) // or Image ResultTitleImage(string title)
        {
            switch (title)
            {
                case "P":
                    return "PURE MEMORY";
                case "F":
                    return "FULL RECALL";
                case "C":
                    return "TRACK COMPLETE";
                default:
                    return "TRACK LOST";
            }
        }
    }
}