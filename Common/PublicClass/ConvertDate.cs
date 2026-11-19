using System.Globalization;

namespace ShipRate.Common.PublicClass
{
    using System;
    using System.Globalization;

    // ۱. اضافه شدن کلمه static به کلاس
    public static class ConvertDate
    {
        private static readonly PersianCalendar _persianCalendar = new PersianCalendar();

        public static DateTime? ToGregorianDateTime(this string? persianDate)
        {
            if (string.IsNullOrWhiteSpace(persianDate))
                return null;

            try
            {
                // استانداردسازی اعداد فارسی و عربی به انگلیسی
                var normalized = NormalizeDigits(persianDate.Trim());

                // پشتیبانی از هر دو جداکننده خط تیره (-) و اسلش (/)
                var parts = normalized.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 3)
                    return null;

                int year = int.Parse(parts[0]);
                int month = int.Parse(parts[1]);
                int day = int.Parse(parts[2]);

                // بررسی محدوده ماه و روز قبل از تبدیل
                if (month < 1 || month > 12 || day < 1 || day > 31)
                    return null;

                return _persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            }
            catch
            {
                return null;
            }
        }

        // اضافه کردن this برای این که این هم Extension Method شود
        public static string? ToPersianDate(this DateTime? date)
        {
            if (!date.HasValue)
                return null;

            var value = date.Value;

            // ۲. استفاده از همان _persianCalendar به جای ساخت new PersianCalendar()
            return $"{_persianCalendar.GetYear(value):0000}-" +
                   $"{_persianCalendar.GetMonth(value):00}-" +
                   $"{_persianCalendar.GetDayOfMonth(value):00}";
        }

        // متد کمکی برای تبدیل اعداد فارسی/عربی به انگلیسی
        private static string NormalizeDigits(string input)
        {
            return input
                .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2").Replace("۳", "3").Replace("۴", "4")
                .Replace("۵", "5").Replace("۶", "6").Replace("۷", "7").Replace("۸", "8").Replace("۹", "9")
                .Replace("٠", "0").Replace("١", "1").Replace("٢", "2").Replace("٣", "3").Replace("٤", "4")
                .Replace("٥", "5").Replace("٦", "6").Replace("٧", "7").Replace("٨", "8").Replace("٩", "9");
        }

    }

}