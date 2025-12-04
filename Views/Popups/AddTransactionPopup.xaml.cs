using Mopups.Pages;
using Mopups.Services;
using System.Globalization;
using Vakilaw.Services;
using Vakilaw.ViewModels;

namespace Vakilaw.Views.Popups;

public partial class AddTransactionPopup : PopupPage
{
    public AddTransactionPopup(TransactionService service, TransactionsVM transactionsVM, Func<Task> onAdded)
    {
        InitializeComponent();

        BindingContext = new AddTransactionPopupVM(service, transactionsVM, async () =>
        {
            await onAdded();
            await MopupService.Instance.PopAsync(); // بستن پاپ‌آپ بعد از ذخیره
        });
    }

    // اینجا متد Event Handler را اضافه می‌کنیم
    private void AmountEntry_Unfocused(object sender, FocusEventArgs e)
    {
        var vm = BindingContext as AddTransactionPopupVM;
        if (vm == null) return;

        if (double.TryParse(vm.AmountText?.Replace("تومان", "").Trim(),
                            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture,
                            out double val))
        {
            vm.AmountText = val.ToString("N0", CultureInfo.InvariantCulture) + " تومان";
            vm.Amount = (decimal)val; // مقدار واقعی برای ذخیره در دیتابیس
        }
        else
        {
            vm.AmountText = "";
            vm.Amount = 0;
        }
    }
}