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

        public static int[] GeneratePatternJumpMap(string searchTerm)
        {
            //throw new NotImplementedException("This code is wrong !");

            var mapLength = Math.Min(256, searchTerm.Length);
            var patternJumpMap = new int[mapLength];

            //traiter le dernier caractère différemment
            var lastChar = searchTerm.Last();
            int lastCharJump = 0;
            for (var i = searchTerm.Length - 1; i >= 0 && searchTerm[i] == lastChar; i--)
                lastCharJump++;
            patternJumpMap[0] = lastCharJump;


            int? lastPrefixLength = null;

            for (int i = 1; i < mapLength; i++)
            {
                var mismatchChar = searchTerm[searchTerm.Length - i - 1];
                var pattern = searchTerm.Substring(searchTerm.Length - i);

                var jumpCandidates = BoyerMooreSearch(searchTerm, pattern, false)
                                         .Where(p => p == 0 || searchTerm[p - 1] != mismatchChar)
                                         .ToArray();


                if (jumpCandidates.Any())
                {
                    patternJumpMap[i] = searchTerm.Length - jumpCandidates.Max() - pattern.Length;
                    if (jumpCandidates.Contains(0))
                    {
                        lastPrefixLength = i;
                    }
                }
                else
                {
                    patternJumpMap[i] = searchTerm.Length - (lastPrefixLength ?? 0);
                }
            }

            return patternJumpMap;
        }
    }
}
