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

        public static string IntegerToRoman(int num)
        {
            return num < romanNumerals.Count() ? romanNumerals[num - 1] : num.ToString();
        }
    }
}
