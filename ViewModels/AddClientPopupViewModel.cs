using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using Vakilaw.Models;
using Vakilaw.Services; // فرض می‌کنیم ClientService اینجا هست
using Vakilaw.Views.Popups; 

namespace Vakilaw.ViewModels;

public partial class AddClientPopupViewModel : ObservableObject
{
    private readonly AddClientPopup _popup;
    private readonly ClientService _clientService;
    private readonly ClientsAndCasesViewModel _clientsAndCasesViewModel;

    public AddClientPopupViewModel(AddClientPopup popup, ClientsAndCasesViewModel clientsAndCasesViewModel, ClientService clientService)
    {
        _popup = popup;
        _clientsAndCasesViewModel = clientsAndCasesViewModel;
        _clientService = clientService;
    }

    [ObservableProperty] private string fullName;
    [ObservableProperty] private string nationalCode;
    [ObservableProperty] private string phoneNumber;
    [ObservableProperty] private string address;
    [ObservableProperty] private string description;

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(FullName) ||
           string.IsNullOrWhiteSpace(NationalCode) ||
           string.IsNullOrWhiteSpace(PhoneNumber))         
        {
            await Toast.Make("لطفاً فیلدهای ضروری را پر کنید!", ToastDuration.Short).Show();
            return;
        }

        var nationalcodeLength = NationalCode.Trim().Length;
        var phonenumberLength = PhoneNumber.Trim().Length;
        
        if (nationalcodeLength > 10 || nationalcodeLength < 10)
        {
            await Toast.Make("کد ملی نامعتبر است!", ToastDuration.Short).Show();
            return;
        }

        if (phonenumberLength > 11 || phonenumberLength < 11)
        {
            await Toast.Make("تلفن همراه نامعتبر است!", ToastDuration.Short).Show();
            return;
        }

        // 1️⃣ ایجاد موکل جدید
        var newClient = new Client
        {
            FullName = FullName,
            NationalCode = NationalCode,
            PhoneNumber = PhoneNumber,
            Address = Address,
            Description = Description
        };

        // 2️⃣ اضافه کردن مستقیم به سرویس / دیتابیس
        await _clientService.AddClient(newClient);

        await _clientsAndCasesViewModel.LoadCountsAsync();
        // 3️⃣ اطلاع دادن به صفحه اصلی (wrapper اضافه شود)
        _popup.RaiseClientCreated(newClient);


        // 4️⃣ بستن پاپ‌آپ
        await MopupService.Instance.PopAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await MopupService.Instance.PopAsync();
    }
}