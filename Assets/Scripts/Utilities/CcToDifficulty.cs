namespace ArcCore.Utitlities
{
    public class CcToDifficulty
    {
        ///<summary>
        ///<returns>String: the difficulty string. Bool: whether it contains the plus icon</returns>
        ///</summary>
        public static (int, bool) Convert(ushort cc)
        {
            int roundDown = (int)cc;

            bool isPlus = roundDown >= 9 && (cc - roundDown) >= 0.7;

            return (roundDown, isPlus);
        }
    }
}