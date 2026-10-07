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

        static readonly string[] Ones = { "", "한", "두", "세", "네", "다섯", "여섯", "일곱", "여덟", "아홉" };
        static readonly string[] Tens = { "", "열", "스물", "서른", "마흔", "쉰", "예순", "일흔", "여든", "아흔" };

        /// <summary>1 → "첫 번째", 2 → "두 번째", 11 → "열한 번째", 20 → "스무 번째". 100 이상은 "100번째".</summary>
        public static string Ordinal(int n)
        {
            if (n == 1) return "첫 번째";
            if (n < 1 || n > 99) return n + "번째";
            string word = n == 20 ? "스무" : Tens[n / 10] + Ones[n % 10];
            return word + " 번째";
        }
    }
}
