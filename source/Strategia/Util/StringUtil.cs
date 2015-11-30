using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;

namespace Strategia
{
    public static class StringUtil
    {
        static string[] romanNumerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV" };
        static char[] vowels = { 'a', 'e', 'i', 'o', 'u' };

        public static string IntegerToRoman(int num)
        {
            return num < romanNumerals.Count() ? romanNumerals[num - 1] : num.ToString();
        }

        public static string ATrait(string trait)
        {
            return (vowels.Contains(trait.ToLower().First()) ? "an " : "a ") + trait.ToLower();
        }
    }
}
