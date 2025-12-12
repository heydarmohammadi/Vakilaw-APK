using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace Vakilaw.Models;

public partial class ReminderModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty] private string title;
    [ObservableProperty] private string description;
    [ObservableProperty] private string category;
    [ObservableProperty] private string priority;

    [ObservableProperty] private string reminderDateString;
    [ObservableProperty] private DateTime? reminderDate;

    [ObservableProperty] private bool isReminderSet;
    public string IsReminderSetText => IsReminderSet ? "دارد" : "ندارد";

    [ObservableProperty] private bool isReminderDone;
    [ObservableProperty] private DateTime createdAt;

    /// <summary>
    /// نمایش تاریخ یادآوری به شمسی (در صورت نبودن مقدار: "-")
    /// مثال خروجی: 1403/05/25 14:30
    /// </summary>
    public string ReminderDateShamsi => ToShamsi(ReminderDate, true);

    /// <summary>
    /// نمایش تاریخ ساخت به شمسی
    /// </summary>
    public string CreatedAtShamsi => ToShamsi(CreatedAt);

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
        return $"{datePart} {hh:00}:{mm:00}";
    }

    partial void OnReminderDateChanged(DateTime? value)
        => OnPropertyChanged(nameof(ReminderDateShamsi));

    partial void OnCreatedAtChanged(DateTime value)
        => OnPropertyChanged(nameof(CreatedAtShamsi));

    partial void OnIsReminderSetChanged(bool value)
        => OnPropertyChanged(nameof(IsReminderSetText));
}