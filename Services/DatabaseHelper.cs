#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Net;
#endif

using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using Vakilaw.Services;
public static class DatabaseHelper
{
    public static string ConnectionString { get; set; }

    //شمسی به میلادی
    public static (DateTime? dateTime, string errorMessage) ConvertShamsiToGregorian(string shamsiDate)
    {
        if (string.IsNullOrWhiteSpace(shamsiDate))
            return (null, LocalizationService.Instance["EnterDateAndTime"]);

        try
        {
            shamsiDate = ConvertToEnglishNumbers(shamsiDate).Trim();

            var dateParts = shamsiDate.Split('/');
            if (dateParts.Length != 3)
                return (null, LocalizationService.Instance["EnterFullDateErrorMessage"]);

            if (!int.TryParse(dateParts[0], out int year) ||
                !int.TryParse(dateParts[1], out int month) ||
                !int.TryParse(dateParts[2], out int day))
                return (null, LocalizationService.Instance["InvalidDateErrorMessage"]);

            // تبدیل به میلادی (ساعت 00:00)
            var persianCalendar = new System.Globalization.PersianCalendar();
            DateTime gregorianDate = persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);

            return (DateTime.SpecifyKind(gregorianDate, DateTimeKind.Local), null);
        }
        catch
        {
            return (null, LocalizationService.Instance["unRecognizedErrorMessage"]);
        }
    }


    //شمسی به میلادی با ساعت
    public static (DateTime? dateTime, string errorMessage) ConvertShamsiToGregorian1(string shamsiDateTime)
    {
        if (string.IsNullOrWhiteSpace(shamsiDateTime))
            return (null, LocalizationService.Instance["EnterDateAndTime"]);

        try
        {
            shamsiDateTime = ConvertToEnglishNumbers(shamsiDateTime);

            var parts = shamsiDateTime.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return (null, LocalizationService.Instance["EnterFullDateAndTimeErrorMessage"]);
            // ---- بررسی تاریخ ----
            var dateParts = parts[0].Split('/');
            if (dateParts.Length != 3)
                return (null, LocalizationService.Instance["EnterFullDateErrorMessage"]);

            if (!int.TryParse(dateParts[0], out int year) ||
                !int.TryParse(dateParts[1], out int month) ||
                !int.TryParse(dateParts[2], out int day))
                return (null, LocalizationService.Instance["InvalidDateErrorMessage"]);

            // ---- بررسی ساعت ----
            var timeParts = parts[1].Split(':');
            if (timeParts.Length != 2)
                return (null, LocalizationService.Instance["HourFormatErrorMessage"]);

            if (!int.TryParse(timeParts[0], out int hour) ||
                !int.TryParse(timeParts[1], out int minute))
                return (null, LocalizationService.Instance["InvalidTimeErrorMessage"]);

            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
                return (null, LocalizationService.Instance["ValidDateAndTime"]);

            // ---- تبدیل به میلادی ----
            var persianCalendar = new System.Globalization.PersianCalendar();
            DateTime gregorianDateTime = persianCalendar.ToDateTime(year, month, day, hour, minute, 0, 0);

            return (DateTime.SpecifyKind(gregorianDateTime, DateTimeKind.Local), null);
        }
        catch
        {
            return (null, LocalizationService.Instance["unRecognizedErrorMessage"]);
        }
    }

    //میلادی به شمسی
    public static string ConvertGregorianToShamsi(DateTime date)
    {
        try
        {
            var pc = new System.Globalization.PersianCalendar();

            int year = pc.GetYear(date);
            int month = pc.GetMonth(date);
            int day = pc.GetDayOfMonth(date);

            return $"{year:0000}/{month:00}/{day:00}";
        }
        catch
        {
            return LocalizationService.Instance["unRecognizedErrorMessage"];
        }
    }


    public static string ConvertToEnglishNumbers(string input)
    {
        return input
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2").Replace("۳", "3").Replace("۴", "4")
            .Replace("۵", "5").Replace("۶", "6").Replace("۷", "7").Replace("۸", "8").Replace("۹", "9");
    }    
}