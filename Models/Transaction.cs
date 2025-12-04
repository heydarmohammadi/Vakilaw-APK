using System.Globalization;

namespace Vakilaw.Models;

public class Transaction
{
    public int Id { get; set; }             // کلید اصلی
    public string Title { get; set; }       // عنوان تراکنش
    public decimal Amount { get; set; }      // مبلغ
    public bool IsIncome { get; set; }      // درآمد یا هزینه
    public string DateString { get; set; }
    public DateTime? Date { get; set; }      // تاریخ
    public string? Description { get; set; } // توضیحات

    // تاریخ شمسی فقط برای نمایش
    public string DateShamsi => ToShamsi(Date, true);

    private static string ToShamsi(DateTime? dt, bool showTime = false)
    {
        if (dt == null) return " -";

        var local = dt.Value.Kind == DateTimeKind.Utc ? dt.Value.ToLocalTime() : dt.Value;
        var pc = new PersianCalendar();

        int y = pc.GetYear(local);
        int m = pc.GetMonth(local);
        int d = pc.GetDayOfMonth(local);

        string datePart = $"{y:0000}/{m:00}/{d:00}";

        if (!showTime)
            return datePart;

        int hh = local.Hour;
        int mm = local.Minute;
        return $"{datePart}";
    }
}