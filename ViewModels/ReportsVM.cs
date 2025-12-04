using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using Vakilaw.Services;

namespace Vakilaw.ViewModels;

public partial class ReportsVM : ObservableObject
{
    public enum ReportType
    {
        Clients,
        Cases,
        Transaction
    }

    private readonly ClientService _clientService;
    private readonly CaseService _caseService;
    private readonly TransactionService _transactionService;
    private readonly IPrinterService _printerService;

    [ObservableProperty]
    private string reportText;

    public ReportsVM(
        ClientService clientService,
        CaseService caseService,
        TransactionService transactionService,
        IPrinterService printerService)
    {
        _clientService = clientService;
        _caseService = caseService;
        _transactionService = transactionService;
        _printerService = printerService;
    }

    // ---------------------------
    // بارگذاری گزارش
    // ---------------------------
    public async Task LoadReport(ReportType type)
    {
        var sb = new System.Text.StringBuilder();

        string shamsiNow = DatabaseHelper.ConvertGregorianToShamsi(DateTime.Now);

        //sb.AppendLine("📄 گزارش");
        sb.AppendLine("──────────────────────────");
        sb.AppendLine($"📅 تاریخ تولید گزارش: {shamsiNow}");
        sb.AppendLine("");

        switch (type)
        {
            case ReportType.Clients:
                LoadClientsReport(sb);
                break;

            case ReportType.Cases:
                LoadCasesReport(sb);
                break;

            case ReportType.Transaction:
                await LoadTransactionReport(sb);
                break;
        }

        ReportText = sb.ToString();
    }

    // -----------------------------
    // گزارش موکل‌ها
    // -----------------------------
    private void LoadClientsReport(System.Text.StringBuilder sb)
    {
        var clients = _clientService.GetClients();
        if (clients == null || clients.Count == 0)
        {
            sb.AppendLine("هیچ موکلی ثبت نشده است.");
            return;
        }

        sb.Insert(0, "📄 گزارش موکل‌ها\n");

        int index = 1;
        foreach (var c in clients)
        {
            sb.AppendLine($"{index++}. {c.FullName}");
            sb.AppendLine($"◾ کد ملی: {c.NationalCode}");
            sb.AppendLine($"📱 موبایل: {c.PhoneNumber}");
            sb.AppendLine($"📌 آدرس: {c.Address}");
            sb.AppendLine($"📝 توضیحات: {c.Description}");
            sb.AppendLine("──────────────────────────");
        }
    }

    // -----------------------------
    // گزارش پرونده‌ها
    // -----------------------------
    private void LoadCasesReport(System.Text.StringBuilder sb)
    {
        var cases = _caseService.GetAllCases();
        if (cases == null || cases.Count == 0)
        {
            sb.AppendLine("هیچ پرونده‌ای ثبت نشده است.");
            return;
        }

        sb.Insert(0, "📄 گزارش پرونده‌ها\n");

        int index = 1;
        foreach (var c in cases)
        {
            sb.AppendLine($"{index++}. {c.Title}");
            sb.AppendLine($"⚖ شماره پرونده: {c.CaseNumber}");
            sb.AppendLine($"👤 موکل: {c.Client?.FullName ?? "-"}");
            sb.AppendLine($"🏛 دادگاه و شعبه رسیدگی: {c.CourtName}");
            sb.AppendLine($"⚖ قاضی: {c.JudgeName}");
            sb.AppendLine($"📅 تاریخ شروع: {c.StartDate ?? "-"}");
            sb.AppendLine($"⚖ وضعیت پرونده: {c.Status}");
            sb.AppendLine($"📅 تاریخ اختتام: {c.EndDate ?? "-"}");
            sb.AppendLine($"📝 توضیحات: {c.Description}");
            sb.AppendLine("──────────────────────────");
        }
    }

    // -----------------------------
    // گزارش تراکنش‌ها
    // -----------------------------
    private async Task LoadTransactionReport(System.Text.StringBuilder sb)
    {
        var trx = await _transactionService.GetAll();

        if (trx == null || trx.Count == 0)
        {
            sb.AppendLine("هیچ تراکنشی ثبت نشده است.");
            return;
        }

        sb.Insert(0, "📄 گزارش تراکنش‌های مالی\n");

        int index = 1;
        foreach (var t in trx)
        {
            sb.AppendLine($"{index++}. {t.Title}");
            sb.AppendLine($"💰 مبلغ: {t.Amount:N0} تومان");
            string transactionTyoe = t.IsIncome ? "درآمد" : "هزینه";
            sb.AppendLine($"💰 نوع: {transactionTyoe}");

            if (t.Date.HasValue)
                sb.AppendLine($"📅 تاریخ: {DatabaseHelper.ConvertGregorianToShamsi(t.Date.Value)}");
            else
                sb.AppendLine("📅 تاریخ: نامشخص");

            sb.AppendLine($"📝 توضیحات: {t.Description}");
            sb.AppendLine("──────────────────────────");
        }
    }

    // -----------------------------
    // چاپ گزارش
    // -----------------------------
    [RelayCommand]
    private async Task PrintReportAsync()
    {
        if (string.IsNullOrWhiteSpace(ReportText))
        {
            await Toast.Make("گزارشی برای چاپ وجود ندارد").Show();
            return;
        }

        await _printerService.PrintTextAsync(ReportText, "⚖️ اپلیکیشن حقوقی وکیلاو");
    }

    // -----------------------------
    // فرمان‌های اختصاصی UI برای هر گزارش
    // -----------------------------
    [RelayCommand]
    public async Task PrintClientsReportAsync()
    {
        await LoadReport(ReportType.Clients);
        await PrintReportAsync();
    }

    [RelayCommand]
    public async Task PrintCasesReportAsync()
    {
        await LoadReport(ReportType.Cases);
        await PrintReportAsync();
    }

    [RelayCommand]
    public async Task PrintTransactionsReportAsync()
    {
        await LoadReport(ReportType.Transaction);
        await PrintReportAsync();
    }
}