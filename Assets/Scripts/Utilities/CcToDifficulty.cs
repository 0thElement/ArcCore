namespace ArcCore.Utitlities
{
    public class CcToDifficulty
    {
        ///<summary>
        ///<returns>Int: the difficulty number. Bool: whether it contains the plus icon</returns>
        ///</summary>
        public static (int, bool) Convert(ushort cc)
        {
            int roundDown = (int)cc;

            bool isPlus = roundDown >= 90 && (cc - roundDown) >= 7;

            return (roundDown, isPlus);
        }

        public static (int, bool) Convert(float cc) {
            return Convert((ushort)(cc * 10));
        }
    }
}