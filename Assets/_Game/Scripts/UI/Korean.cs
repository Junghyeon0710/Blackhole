using System.Globalization;

namespace Blackhole
{
    public static class Korean
    {
        /// <summary>받침에 맞춰 "으로/로"를 붙인다 (달로, 운석으로).</summary>
        public static string WithRo(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;
            char last = word[word.Length - 1];
            if (last < 0xAC00 || last > 0xD7A3) return word + "(으)로";
            int final = (last - 0xAC00) % 28;
            return word + (final == 0 || final == 8 ? "로" : "으로"); // 8 = ㄹ
        }

        /// <summary>1,234 처럼 세 자리마다 쉼표.</summary>
        public static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
