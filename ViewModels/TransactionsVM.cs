using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using System.Collections.ObjectModel;
using Vakilaw.Models;
using Vakilaw.Services;
using Vakilaw.Views.Popups;

namespace Vakilaw.ViewModels;

public partial class TransactionsVM : ObservableObject
{
    private readonly TransactionService _transactionService;
    private readonly MainPageVM _mainPageVM;

    // لیست واقعی تراکنش‌ها
    [ObservableProperty]
    private ObservableCollection<Transaction> transactions = new();

    [ObservableProperty] private bool isDetailsVisible;
    [ObservableProperty] private string detailsText;

    #region Footer
    [ObservableProperty] private decimal incomeAmount;
    [ObservableProperty] private decimal expenseAmount;
    [ObservableProperty] private decimal balanceAmount;

    public async Task LoadAmountsAsync()
    {
        IncomeAmount = await _transactionService.GetTotalIncome();
        ExpenseAmount = await _transactionService.GetTotalExpense();
        BalanceAmount = await _transactionService.GetBalance();
    }
    #endregion

    public TransactionsVM(TransactionService transactionService, MainPageVM mainPageVM)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));

        // بارگذاری اولیه (فقط فراخوانی، نتیجه در UI thread اعمال می‌شود)
        LoadTransactions().SafeFireAndForget();
        _ = LoadAmountsAsync();
        _mainPageVM = mainPageVM;
    }

    // 📌 بارگذاری تراکنش‌ها
    private async Task LoadTransactions()
    {
        var list = await _transactionService.GetAll();

        // حتماً تغییرات Collection را در ترد UI انجام بدهیم
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Transactions.Clear();
            foreach (var t in list)
                Transactions.Add(t);
        });
    }

    // سرچ و debounce
    [ObservableProperty] private string searchText;
    private System.Timers.Timer _debounceTimerTransactions;

    private async Task SearchTransactions()
    {
        List<Transaction> filtered;

        if (string.IsNullOrWhiteSpace(SearchText))
            filtered = await _transactionService.GetAll();
        else
            filtered = _transactionService.SearchTransactions(SearchText);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Transactions.Clear();
            foreach (var t in filtered)
                Transactions.Add(t);
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimerTransactions?.Stop();
        _debounceTimerTransactions?.Dispose();

        _debounceTimerTransactions = new System.Timers.Timer(400) { AutoReset = false };
        _debounceTimerTransactions.Elapsed += (s, e) =>
        {
            // چون Timer در threadpool اجرا میشه، فراخوانی SearchTransactions را در UI thread یا ساده صدا بزن
            SearchTransactions();
        };
        _debounceTimerTransactions.Start();
    }

    [ObservableProperty] private string fromDateShamsi;
    [ObservableProperty] private string toDateShamsi;

    [RelayCommand]   
    private async Task SearchByRange()
    {
        // اگر هیچ فیلدی پر نشده => همه تراکنش‌ها رو لود کن
        if (string.IsNullOrWhiteSpace(FromDateShamsi) && string.IsNullOrWhiteSpace(ToDateShamsi))
        {
            var all = await _transactionService.GetAll();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Transactions.Clear();
                foreach (var t in all)
                    Transactions.Add(t);
            });
            return;
        }

        // اگر یکی خالی است می‌تونی تصمیم بگیری (درخواست قبلیت: return)
        if (string.IsNullOrWhiteSpace(FromDateShamsi) || string.IsNullOrWhiteSpace(ToDateShamsi))
            return;

        var results = _transactionService.SearchTransactionsByDateRange(FromDateShamsi, ToDateShamsi);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Transactions.Clear();
            foreach (var t in results)
                Transactions.Add(t);
        });
    }


    // اگر می‌خواهی وقتی فیلدها خالی شدند بصورت زنده همه لیست ری‌لود شود:
    partial void OnFromDateShamsiChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(FromDateShamsi) && string.IsNullOrWhiteSpace(ToDateShamsi))
        {
            _ = SearchByRange();
        }
    }

    partial void OnToDateShamsiChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(FromDateShamsi) && string.IsNullOrWhiteSpace(ToDateShamsi))
        {
            _ = SearchByRange();
        }
    }

    // 📌 نمایش پاپ‌آپ افزودن تراکنش
    [RelayCommand]
    private async Task ShowAddTransactionPopup()
    {
        if (!await _mainPageVM.CanUseLawyerFeaturesAsync())
            return;

        var popup = new AddTransactionPopup(_transactionService, this, async () =>
        {
            await LoadTransactions(); // بعد از ثبت، لیست آپدیت میشه
            await LoadAmountsAsync(); // و مقادیر فوتر هم آپدیت شود
        });

        await MopupService.Instance.PushAsync(popup);
    }

    // 📌 حذف تراکنش
    [RelayCommand]
    private async Task DeleteTransaction(Transaction transaction)
    {
        if (transaction == null) return;

        await _transactionService.Delete(transaction.Id);
        await LoadTransactions();
        await LoadAmountsAsync();
    }

    [RelayCommand]
    private async Task ShowDetails(int id)
    {
        var tran = Transactions.FirstOrDefault(x => x.Id == id);
        if (tran == null) return;

        DetailsText = tran.Description;
        IsDetailsVisible = true;
    }

    [RelayCommand] private void CloseDetails() => IsDetailsVisible = false;
}