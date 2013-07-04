using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Solution
{
    public static void Chocolate(String[] args)
    {
        var nbInputs = int.Parse(Console.ReadLine());
        for (int i = 0; i < nbInputs; i++)
        {
            var nbColleagues = int.Parse(Console.ReadLine());
            Console.WriteLine(ProcessChocolateInput(nbColleagues, Console.ReadLine()));
        }
    }

    public static int ProcessChocolateInput(int nbColleagues, string input)
    {
        var startupSituation = input.Split(' ').Select(i => int.Parse(i)).ToArray();

        int plus1 = 0;
        int plus2 = 0;
        int plus5 = 0;
        while (Levelize(startupSituation, ref plus1, ref plus2, ref plus5))
        { /* Do nothing */ }

        var total = 5 * plus5 + 2 * plus2 + plus1;
        return total / 5
               + (total % 5) / 2
               + (total % 5) % 2;
    }

    public static bool Levelize(int[] currentSituation, ref int plus1, ref int plus2, ref int plus5)
    {
        if (currentSituation.Distinct().Count() == 1)
            return false;

        var min = currentSituation.Min();
        var nextLevel = currentSituation.Where(i => i > min).Min();
        var diff = nextLevel - min;
        int increment;
        if (diff >= 5)
        {
            increment = 5;
            plus5++;
        }
        else if (diff >= 2)
        {
            increment = 2;
            plus2++;
        }
        else
        {
            increment = 1;
            plus1++;
        }
        var chosenFound = false;
        for (int i = 0; i < currentSituation.Length; i++)
        {
            if (chosenFound || currentSituation[i] != nextLevel)
                currentSituation[i] += increment;
            else
                chosenFound = true;
        }

        return true;
    }

    public static void Restaurant(String[] args)
    {
        var nbInputs = int.Parse(Console.ReadLine());
        for (int i = 0; i < nbInputs; i++)
        {
            Console.WriteLine(ProcessRestaurantInput(Console.ReadLine()));
        }
    }

    public static int ProcessRestaurantInput(string input)
    {
        var splittedInput = input.Split(' ');
        var longueur = int.Parse(splittedInput[0]);
        var largueur = int.Parse(splittedInput[1]);
        var pgcd = PGCD(longueur, largueur);
        return longueur / pgcd * largueur / pgcd;
    }

    private static int PGCD(int first, int second)
    {
        var result = 1;
        for (var i = 2; i <= first && i <= second; i++)
        {
            if (first % i == 0
                && second % i == 0)
            {
                result = i;
            }
        }
        return result;
    }

    public static void ChildStrings(String[] args)
    {
        var a = "WEWOUCUIDGCGTRMEZEPXZFEJWISRSBBSYXAYDFEJJDLEBVHHKS";
        var b = "FDAGCXGKCTKWNECHMRXZWMLRYUCOCZHJRRJBOAJOQJZZVUYXIC";

        var charOrder = new Dictionary<char, List<int>>();
        for (int i = 0; i < a.Length; i++)
        {
            List<int> orders;
            var curChar = a[i];
            if (!charOrder.TryGetValue(curChar, out orders))
            {
                orders = new List<int>();
                charOrder.Add(curChar, orders);
            }
            orders.Add(i);
        }

        var maxLength = 0;
        string champion = "";
        for (int i = 0; i < b.Length; i++)
        {
            var curLength = 0;
            var curIndex = -1;
            var curBuilder = new StringBuilder(maxLength);
            for (int j = i; j < b.Length; j++)
            {
                var curChar = b[j];
                if (!charOrder.ContainsKey(curChar))
                    continue;
                var validIndexes = charOrder[curChar].Where(o => o >= curIndex).ToArray();
                if (validIndexes.Any())
                {
                    curLength++;
                    curIndex = validIndexes.Min();
                    curBuilder.Append(curChar);
                }
            }
            if (curLength > maxLength)
            {
                maxLength = curLength;
                champion = curBuilder.ToString();
            }
        }

        Console.WriteLine(maxLength);
    }

    public static void Test2(String[] args)
    {
        var a = "WEWOUCUIDGCGTRMEZEPXZFEJWISRSBBSYXAYDFEJJDLEBVHHKS";
        var b = "FDAGCXGKCTKWNECHMRXZWMLRYUCOCZHJRRJBOAJOQJZZVUYXIC";
        var maxStringA = new string(a.Where(b.Contains).ToArray());
        var maxStringB = new string(b.Where(a.Contains).ToArray());

        if (maxStringA.Length == 0 || maxStringB.Length == 0)
            Console.WriteLine(0.ToString());
        else
        {
            var SubstringA = GenerateAllSubstrings(maxStringA);
            var SubstringB = GenerateAllSubstrings(maxStringB);

            Console.WriteLine(SubstringA.Intersect(SubstringB).Max(s => s.Length));
        }
    }

    private static List<string> GenerateAllSubstrings(string baseString)
    {
        ConcurrentBag<string> results = new ConcurrentBag<string>();

        Parallel.For(0, baseString.Length, i =>
            Parallel.For(1, baseString.Length - i, j =>
                GenerateSubstrings(baseString, i, j, "", results)));

        return results.ToList();
    }

    private static void GenerateSubstrings(string baseString, int startIndex, int maxLength, string buffer, ConcurrentBag<string> results)
    {
        var currentBuffer = buffer + baseString.Substring(startIndex, 1);

        if (maxLength == 1 || startIndex == baseString.Length - 1)
            results.Add(currentBuffer);
        else
            for (var i = startIndex + 1; i < baseString.Length; i++)
                GenerateSubstrings(baseString, i, maxLength - 1, currentBuffer, results);
    }
}