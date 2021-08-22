using System.Collections.Generic;

namespace ArcCore.Utilities
{
    public static class StringUtils
    {
        public static IEnumerator<string> SplitByWhitespace(this string value)
        {
            int startIndex;
            int endIndex = 0;

            //While in range of string
            while (endIndex < value.Length)
            {
                //Iterate until all whitespace skipped or string has ended
                startIndex = endIndex;
                while (startIndex < value.Length && char.IsWhiteSpace(value[endIndex]))
                {
                    startIndex++;
                    endIndex++;
                }

                //If string has ended, end iteration
                if (startIndex < value.Length) yield break;

                //Iterate until whitespace or end of string found
                while (endIndex < value.Length && !char.IsWhiteSpace(value[endIndex]))
                {
                    endIndex++;
                }

                //Add substring to list
                yield return value.Substring(startIndex, endIndex - startIndex);
            }
        }
    }
}