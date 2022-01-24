namespace ArcCore.Utilities
{
    public class Conversion
    {
        ///<summary>
        ///<returns>Int: the difficulty number. Bool: whether it contains the plus icon</returns>
        ///</summary>
        public static (int, bool) CcToDifficulty(float cc)
        {
            int roundDown = (int)cc;

            bool isPlus = roundDown >= 9 && (cc - roundDown) >= 0.7;

            return (roundDown, isPlus);
        }
        public static string ScoreDisplay(int score)
        {
            int last3 = score % 1000;
            int middle3 = (score / 1000) % 1000;
            int remain = (score / 1_000_000);
            return $"{remain:d2}'{middle3:d3}'{last3:d3}";
        }
    }
}