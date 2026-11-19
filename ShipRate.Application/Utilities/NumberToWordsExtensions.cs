namespace ShipRate.Application.Utilities
{
    public static class NumberToWordsExtensions
    {
        private static readonly string[] Ones =
        {
            "صفر", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه",
            "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده"
        };

        private static readonly string[] Tens =
        {
            "", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود"
        };

        private static readonly string[] Hundreds =
        {
            "", "یکصد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد"
        };

        private static readonly string[] Scale =
        {
            "", "هزار", "میلیون", "میلیارد", "تریلیون", "کادریلیون"
        };

        /// <summary>
        /// تبدیل عدد صحیح (long یا int) به حروف فارسی
        /// </summary>
        public static string ToPersianWords(this long number)
        {
            if (number == 0)
                return Ones[0];

            if (number < 0)
                return "منفی " + ToPersianWords(-number);

            string result = "";
            int scaleIndex = 0;

            while (number > 0)
            {
                int chunk = (int)(number % 1000);
                if (chunk > 0)
                {
                    string chunkWords = ConvertChunk(chunk);
                    string scaleName = Scale[scaleIndex];

                    string separator = string.IsNullOrEmpty(result) ? "" : " و ";
                    result = chunkWords + (string.IsNullOrEmpty(scaleName) ? "" : " " + scaleName) + separator + result;
                }

                number /= 1000;
                scaleIndex++;
            }

            // پاکسازی واوهای اضافه احتمالی در انتها
            result = result.Trim();
            if (result.EndsWith(" و"))
            {
                result = result.Substring(0, result.Length - 2).Trim();
            }

            return result;
        }

        public static string ToPersianWords(this int number) => ((long)number).ToPersianWords();
        public static string ToPersianWords(this decimal number) => ((long)Math.Floor(number)).ToPersianWords();

        /// <summary>
        /// تبدیل عدد به حروف همراه با واحد پول (مثلا ریال یا تومان)
        /// </summary>
        public static string ToPersianCurrencyWords(this long number, string currencyUnit = "ریال")
        {
            if (number == 0)
                return "صفر " + currencyUnit;

            return number.ToPersianWords() + " " + currencyUnit;
        }

        private static string ConvertChunk(int chunk)
        {
            string result = "";

            int hundreds = chunk / 100;
            int remainder = chunk % 100;

            if (hundreds > 0)
            {
                result += Hundreds[hundreds];
            }

            if (remainder > 0)
            {
                if (!string.IsNullOrEmpty(result))
                    result += " و ";

                if (remainder < 20)
                {
                    result += Ones[remainder];
                }
                else
                {
                    int tens = remainder / 10;
                    int ones = remainder % 10;

                    result += Tens[tens];
                    if (ones > 0)
                    {
                        result += " و " + Ones[ones];
                    }
                }
            }

            return result;
        }
    }
}
