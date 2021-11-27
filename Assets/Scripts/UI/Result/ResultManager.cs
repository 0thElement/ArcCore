using UnityEngine;
using UnityEngine.UI;
using ArcCore.Serialization;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Data;

namespace ArcCore.UI.Result
{
    [DefaultExecutionOrder(0)]
    public class ResultManager : MonoBehaviour
    {
        const bool IS_DEBUG = true;

        [HideInInspector]
        public ScoreHandler score { get; private set; }
        public ChartInfo chartInfo { get; private set; }
        public int resultScore { get; private set; }
        public int highScore { get; private set; } = 0;
        public int longestCombo { get; private set; }
        public int longestPureChain { get; private set; }
        public int longestMpureChain { get; private set; }
        public int maxPureCount { get; private set; }
        public int earlyPureCount { get; private set; }
        public int latePureCount { get; private set; }
        public int earlyFarCount { get; private set; }
        public int lateFarCount { get; private set; }
        public int lostCount { get; private set; }
        public int noteCount { get; private set; }
        //public int recollectionRate { get; private set; } // TODO: create recollectionRate in ScoreHandler if necessary

        public void Start()
        {
            if (IS_DEBUG)
            {
                // make a sample result score data and give it to LoadResult()
                ScoreHandler tempScoreHandler = new ScoreHandler();
                tempScoreHandler.currentScore = 9501234;
                tempScoreHandler.tracker.longestCombo = 789;
                tempScoreHandler.tracker.earlyFarCount = 6;
                tempScoreHandler.tracker.lateFarCount = 5;
                tempScoreHandler.tracker.earlyPureCount = 4;
                tempScoreHandler.tracker.latePureCount = 3;
                tempScoreHandler.tracker.maxPureCount = 2;
                tempScoreHandler.tracker.lostCount = 0;
                tempScoreHandler.tracker.noteCount = 1234;
                tempScoreHandler.tracker.longestPureChain = 11;
                tempScoreHandler.tracker.longestMpureChain = 12;

                ChartInfo tempChartInfo = new ChartInfo();
                tempChartInfo.diffType = DifficultyType.Future;
                //tempChartInfo.songInfoOverride.name = "Debug_Song";
                //tempChartInfo.songInfoOverride.artist = "Debug_Composer";

                LoadResult(tempScoreHandler, tempChartInfo);
            }
        }

        // Receive played score data and chart infomation from GamePlay Scene
        // This MUST be called when loaded this scene from GamePlay Scene
        public void LoadResult(ScoreHandler score, ChartInfo chartInfo)
        {
            this.score = score;
            this.chartInfo = chartInfo;

            this.resultScore = (int) score.currentScore;
            this.longestCombo = score.tracker.longestCombo;
            this.longestPureChain = score.tracker.longestPureChain;
            this.longestMpureChain = score.tracker.longestMpureChain;
            this.maxPureCount = score.tracker.maxPureCount;
            this.earlyPureCount = score.tracker.earlyPureCount;
            this.latePureCount = score.tracker.latePureCount;
            this.earlyFarCount = score.tracker.earlyFarCount;
            this.lateFarCount = score.tracker.lateFarCount;
            this.lostCount = score.tracker.lostCount;
            this.noteCount = score.tracker.noteCount;
            //this.recollectionRate = score.recollectionRate
        }

        #region Screen Processing
        public void FadeIn()
        {

        }
        public void FadeOut()
        {

        }
        #endregion

        #region Methods
        public string JudgeRank()
        {
            string resultRank;

            if (resultScore > 9_900_000)
                resultRank = "EX+";
            else if (resultScore > 9_800_000)
                resultRank = "EX";
            else if (resultScore > 9_500_000)
                resultRank = "AA";
            else if (resultScore > 9_200_000)
                resultRank = "A";
            else if (resultScore > 8_900_000)
                resultRank = "B";
            else if (resultScore > 8_600_000)
                resultRank = "C";
            else
                resultRank = "D";

            return resultRank;
        }

        public string JudgeTitle()
        {
            string resultTitle;
            
            // if (score.recollectionRate < 70)
            //     resultTitle = "L";
            // else
            if (lostCount == 0)
                if (earlyFarCount + lateFarCount == 0)
                    resultTitle = "P";
                else
                    resultTitle = "F";
            else
                resultTitle = "C";

            return resultTitle;
        }

        public int SumOfPure()
            => maxPureCount + earlyPureCount + latePureCount;

        public int SumOfFar()
            => earlyFarCount + lateFarCount;

        public int DifferenceFromHighScore()
            => resultScore - highScore;
        #endregion

        #region Data Processing
        // Load score data for read/write highscore data
        // TODO: create "highScore" "title" variable in ChartInfo or somewhere that holds save data
        private ChartInfo LoadScoreData()
        {
            return new ChartInfo();
        }

        // Save highscore and title data
        private void SaveScoreData(int newHighScore)
        {

        }
        #endregion
    }
}