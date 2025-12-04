using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vakilaw.Models;
using Vakilaw.Services;

namespace Vakilaw.ViewModels;

public partial class AddTransactionPopupVM : ObservableObject
{
    private readonly TransactionService _transactionService;
    private readonly TransactionsVM _transactionsVM;
    private readonly Func<Task> _onTransactionAdded;

    [ObservableProperty] private string title;

    // مقدار واقعی عددی
    [ObservableProperty] private decimal amount;

    // متن نمایش در Entry (فرمت‌شده با هزارگان)
    [ObservableProperty] private string amountText;

    // RadioButton برای انتخاب نوع تراکنش
    [ObservableProperty] private bool isIncome = true;
    [ObservableProperty] private bool isExpense = false;

    [ObservableProperty] private DateTime date = DateTime.Now;
    [ObservableProperty] private string? description;

    public AddTransactionPopupVM(TransactionService transactionService, TransactionsVM transactionsVM, Func<Task> onTransactionAdded)
    {
        _transactionService = transactionService;
        _transactionsVM = transactionsVM;
        _onTransactionAdded = onTransactionAdded;        
    }

    // وقتی IsIncome تغییر کرد، IsExpense اتوماتیک false شود
    partial void OnIsIncomeChanged(bool value)
    {
        if (value) IsExpense = false;
    }

    // وقتی IsExpense تغییر کرد، IsIncome اتوماتیک false شود
    partial void OnIsExpenseChanged(bool value)
    {
        if (value) IsIncome = false;
    }

    [ObservableProperty]
    private Transaction transaction = new Transaction();
  
    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title) || Amount <= 0 || string.IsNullOrWhiteSpace(Date.ToString()) || (!IsIncome && !IsExpense))
        {
            await Toast.Make("لطفاً فیلدهای ضروری را پر کنید!", ToastDuration.Short).Show();
            return;
        }
           
        // بررسی و تبدیل تاریخ و ساعت یادآوری
        if (!string.IsNullOrWhiteSpace(Transaction.DateString))
        {
            var (convertedDate, errorMessage) = DatabaseHelper.ConvertShamsiToGregorian(Transaction.DateString);
            if (convertedDate == null)
            {
                await Toast.Make(errorMessage, ToastDuration.Short, 14).Show();
                return;
            }
            Transaction.Date = convertedDate;
        }
        else
        {
            await Toast.Make(LocalizationService.Instance["EnterDateToast"], ToastDuration.Short, 14).Show();
            return;
        }

        Transaction.Title = Title;
        Transaction.Amount = Amount;
        Transaction.IsIncome = IsIncome;
        Transaction.Description = Description;

        await _transactionService.Add(Transaction);
        await _transactionsVM.LoadAmountsAsync();

        if (_onTransactionAdded != null)
            await _onTransactionAdded.Invoke();
    }
}