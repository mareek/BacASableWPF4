using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BacASableWPF4
{
    public static class BoyerMoore
    {
        public static bool BoyerMooreContains(this string text, string searchTerm)
        {
            return BoyerMooreSearch(text, searchTerm).Any();
        }

        public static IEnumerable<int> BoyerMooreSearch(this string text, string searchTerm)
        {
            return BoyerMooreSearch(text, searchTerm, false);
        }
        private static IEnumerable<int> BoyerMooreSearch(string text, string searchTerm, bool usePatternJump)
        {
            var charJumpMap = GenerateCharJumpMap(searchTerm);
            var patternJumpMap = usePatternJump ? GeneratePatternJumpMap(searchTerm) : null;

            var searchPos = 0;
            var increment = searchTerm.Length - 1;
            while ((searchPos + increment) < text.Length)
            {
                int termPosition = increment;
                while (termPosition >= 0 && searchTerm[termPosition] == text[searchPos + termPosition])
                    termPosition--;

                int jump = searchTerm.Length;
                int charJump;

                if (termPosition < 0)
                    yield return searchPos;
                else if (charJumpMap.TryGetValue(text[searchPos + termPosition], out charJump))
                {
                    var patternIndex = increment - termPosition;

                    if (usePatternJump
                        && patternIndex < patternJumpMap.Length
                        && patternJumpMap[patternIndex] > charJump)
                        jump = patternJumpMap[patternIndex];
                    else
                        jump = charJump;
                }

                searchPos += jump;
            }
        }

        private static Dictionary<char, int> GenerateCharJumpMap(string searchTerm)
        {
            var jumpMap = new Dictionary<char, int>();

            for (var i = 0; i < searchTerm.Length - 1; i++)
                jumpMap[searchTerm[i]] = searchTerm.Length - i - 1;

            return jumpMap;
        }

        private static int[] GeneratePatternJumpMap(string searchTerm)
        {
            throw new NotImplementedException("This code is wrong !");

            var mapLength = Math.Min(256, searchTerm.Length);
            var patternJumpMap = new int[mapLength];

            for (int i = 0; i < mapLength; i++)
            {
                var pattern = searchTerm.Substring(searchTerm.Length - i - 1);
                var patternOccurences = BoyerMooreSearch(searchTerm, pattern, false);

                if (patternOccurences.Any())
                    patternJumpMap[i] = searchTerm.Length - 1 - patternOccurences.Max();
                else
                    patternJumpMap[i] = searchTerm.Length;
            }

            return patternJumpMap;
        }
    }
}
