using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Vakilaw.Models;
using Vakilaw.Models.Messages;
using Vakilaw.Services;
using Vakilaw.Views.Popups;

namespace Vakilaw.ViewModels;

public partial class MainPageVM : ObservableObject
{
    private readonly UserService _userService;
    private readonly SmsService _otpService;
    private readonly LawService _lawService;
    private readonly LawyerService _lawyerService;
    private readonly LicenseService _licenseService;

    [ObservableProperty] private ObservableCollection<LawItem> bookmarkedLaws;
    [ObservableProperty] private ObservableCollection<Lawyer> lawyers;
    [ObservableProperty] private ObservableCollection<Lawyer> allLawyers;
    [ObservableProperty] private ObservableCollection<string> cities;

    [ObservableProperty] private bool isLawyer;
    [ObservableProperty] private bool canRegisterLawyer;
    [ObservableProperty] private bool showRegisterLabel;
    [ObservableProperty] private bool lawyerLicenseVisibility;
    [ObservableProperty] private string lawyerFullName;
    [ObservableProperty] private string lawyerLicense;

    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 7;
    [ObservableProperty] private bool hasMorePages = true;
    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private string selectedCity;
    [ObservableProperty] private string searchQuery;

    [ObservableProperty] private bool isLawyersListVisible;
    [ObservableProperty] private bool isBookmarkVisible;
    [ObservableProperty] private bool isSettingsVisible;

    private CancellationTokenSource _searchCts;

    [ObservableProperty] private string selectedTab = "Home";
    [ObservableProperty] private bool isLawyerSubscriptionActive;
    [ObservableProperty] private bool isTrialActive;
    [ObservableProperty] private DateTime trialEndDate;

    /// <summary>
    /// فقط وقتی وکیل ثبت‌نام کرده و اشتراک فعال داره → آیتم‌های ویژه رو فعال کن
    /// </summary>
    public bool CanUseLawyerFeatures => IsLawyer && IsLawyerSubscriptionActive;

    public MainPageVM(UserService userService, SmsService otpService, LawService lawService, LawyerService lawyerService, LicenseService licenseService)
    {
        _userService = userService;
        _otpService = otpService;
        _lawService = lawService;
        _lawyerService = lawyerService;
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));

        BookmarkedLaws = new ObservableCollection<LawItem>();
        Lawyers = new ObservableCollection<Lawyer>();
        AllLawyers = new ObservableCollection<Lawyer>();
        Cities = new ObservableCollection<string>();

        Task.Run(async () => await InitializeLawyersAsync());
        Task.Run(async () => await LoadBookmarksAsync());

        LoadUserState();


        WeakReferenceMessenger.Default.Register<BookmarkChangedMessage>(this, async (r, m) =>
        {
            var law = await _lawService.GetLawByIdAsync(m.LawId);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (m.IsBookmarked)
                {
                    if (law != null && !BookmarkedLaws.Any(x => x.Id == m.LawId))
                        BookmarkedLaws.Add(law);
                }
                else
                {
                    var item = BookmarkedLaws.FirstOrDefault(x => x.Id == m.LawId);
                    if (item != null)
                        BookmarkedLaws.Remove(item);
                }
            });
        });

        WeakReferenceMessenger.Default.Register<LawyerRegisteredMessage>(this, async (r, m) =>
        {
            IsLawyer = true;
            CanRegisterLawyer = false;
            ShowRegisterLabel = false;
            LawyerLicenseVisibility = true;
            LawyerFullName = m.Value;
            LawyerLicense = m.LicenseNumber;

            CurrentPage = 1;
            Lawyers.Clear();
            await LoadNotesAsync();
        });

        WeakReferenceMessenger.Default.Register<LicenseActivatedMessage>(this, async (r, m) =>
        {
            if (m.IsActivated)
            {
                await CheckLicenseAsync();
                OnPropertyChanged(nameof(CanUseLawyerFeatures));
            }
        });

        // بررسی وضعیت اشتراک هنگام لود شدن
        Task.Run(async () => await CheckLicenseAsync());
    }

    #region License & Trial
    private async Task CheckLicenseAsync()
    {
        if (!IsLawyer) return;

        var deviceId = DeviceHelper.GetDeviceId();
        var license = await _licenseService.GetActiveLicenseAsync(deviceId);

        bool isValid = license != null && license.EndDate > DateTime.Now;

        IsLawyerSubscriptionActive = isValid;
        Preferences.Set("IsSubscriptionActive", isValid);

        if (isValid)
        {
            TrialEndDate = license!.EndDate;
            IsTrialActive = license.SubscriptionType == "Trial";
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var popup = new SubscriptionPopup(_licenseService);
                await MopupService.Instance.PushAsync(popup);
            });
        }

        OnPropertyChanged(nameof(CanUseLawyerFeatures));
    }

    private async Task ShowSubscriptionPopupAsync()
    {
        var popup = new SubscriptionPopup(_licenseService);
        await MopupService.Instance.PushAsync(popup);
    }

    [RelayCommand]
    public async Task CheckTrialAndShowPopupAsync()
    {
        string deviceId = DeviceHelper.GetDeviceId();
        var license = await _licenseService.GetActiveLicenseAsync(deviceId);

        if (license == null || license.EndDate < DateTime.Now)
        {
            await ShowSubscriptionPopupAsync();
        }
        else
        {
            TrialEndDate = license.EndDate;
            IsTrialActive = license.SubscriptionType == "Trial";
        }

        OnPropertyChanged(nameof(CanUseLawyerFeatures));
    }
    #endregion

    #region Navigation
    [RelayCommand]
    public async Task OpenLawyerPopupAsync()
    {
        var isRegistered = Preferences.Get("IsLawyerRegistered", false);
        var deviceId = DeviceHelper.GetDeviceId();

        if (!isRegistered)
        {
            // پاپ‌آپ ثبت نام
            var popup = new LawyerSubmitPopup(_userService, _otpService, _licenseService, deviceId);
            await MopupService.Instance.PushAsync(popup);
        }
        else
        {
            // بررسی وضعیت اشتراک
            if (!isLawyerSubscriptionActive)
            {
                // پاپ‌آپ خرید/تمدید اشتراک
                var subscriptionPopup = new SubscriptionPopup(_licenseService);
                await MopupService.Instance.PushAsync(subscriptionPopup);
            }
            else
            {
                // پاپ‌آپ نمایش اطلاعات کاربری و اشتراک
                var popup = new LawyerInfoPopup();
                await MopupService.Instance.PushAsync(popup);
            }
        }
    }

    [RelayCommand]
    public async Task ShowDetailsAsync(Lawyer lawyer)
    {
        if (lawyer == null) return;
        var popup = new LawyerDetailsPopup(lawyer.FullName, lawyer.PhoneNumber, lawyer.Address);
        await MopupService.Instance.PushAsync(popup);
    }

    [RelayCommand] public async Task LawBankPageAsync() => await Shell.Current.GoToAsync("LawBankPage");
    [RelayCommand] public async Task ClientsAndCasesPageAsync() => await Shell.Current.GoToAsync("ClientsAndCasesPage");
    [RelayCommand] public async Task DocumentsPageAsync() => await Shell.Current.GoToAsync("DocumentsPage");
    [RelayCommand] public async Task SMSPanelPageAsync() => await Shell.Current.GoToAsync("SMSPanelPage");
    //[RelayCommand] public async Task SMSPanelPageAsync() => await Toast.Make("برای فعال سازی این بخش با توسعه دهنده در ارتباط باشید", ToastDuration.Long).Show(); /*await Shell.Current.GoToAsync("SMSPanelPage");*/
    [RelayCommand] public async Task TransactionsPageAsync() => await Shell.Current.GoToAsync("TransactionsPage");
    [RelayCommand] public async Task ReportsPageAsync() => await Shell.Current.GoToAsync("ReportsPage");
    [RelayCommand] public async Task OpenAdlIranSiteAsync() => await Launcher.OpenAsync("https://adliran.ir/");
    [RelayCommand] private async Task HamiVakilAsync() => await Launcher.OpenAsync("https://search-hamivakil.ir/");
    #endregion

    #region Helpers
    private void LoadUserState()
    {
        var role = Preferences.Get("UserRole", "Unknown");
        var isRegistered = Preferences.Get("IsLawyerRegistered", false);

        if (role == "Unknown")
        {
            IsLawyer = false;
            CanRegisterLawyer = true;
            ShowRegisterLabel = true;
            LawyerLicenseVisibility = false;
        }
        else
        {
            IsLawyer = role == "Lawyer" && isRegistered;
            CanRegisterLawyer = !isRegistered;
            ShowRegisterLabel = !isRegistered;
            LawyerLicenseVisibility = isRegistered;
            LawyerFullName = Preferences.Get("LawyerFullName", string.Empty);
            LawyerLicense = Preferences.Get("LawyerLicense", string.Empty);

            IsLawyerSubscriptionActive = Preferences.Get("IsSubscriptionActive", false);

            if (IsLawyer) CheckLicenseAsync().SafeFireAndForget();
        }

        OnPropertyChanged(nameof(CanUseLawyerFeatures));
    }

    public async Task InitializeAsync()
    {
        await InitializeLawyersAsync();
        await LoadBookmarksAsync();
        await LoadNotesAsync();
    }
    #endregion

    #region Toggle Panels
    [RelayCommand]
    private async Task SelectTab(string tabName)
    {
        SelectedTab = tabName;

        switch (tabName)
        {
            case "Home": await ToggleHomeAsync(); break;
            case "AdlIran": await OpenAdlIranSiteAsync(); break;
            case "Bookmarks": await ToggleBookmarkPanelAsync(); break;
            case "Settings": await ToggleSettingsPanelAsync(); break;
        }
    }

    private async Task CloseAllPanelsAsync()
    {
        if (IsLawyersListVisible)
        {
            await SlideOutPanel(LawyersListPanelRef);
            IsLawyersListVisible = false;
        }
        if (IsBookmarkVisible)
        {
            await SlideOutPanel(BookmarkPanelRef);
            IsBookmarkVisible = false;
        }
        if (IsSettingsVisible)
        {
            await SlideOutPanel(SettingsPanelRef);
            IsSettingsVisible = false;
        }
        // هر پنل دیگری هم اضافه شود
    }

    [RelayCommand]
    public async Task ToggleHomeAsync()
    {
        await CloseAllPanelsAsync();
    }

    [RelayCommand]
    public async Task ToggleLawyersListAsync()
    {
        if (IsLawyersListVisible)
            await SlideOutPanel(LawyersListPanelRef);
        else
            await SlideInPanel(LawyersListPanelRef);

        IsLawyersListVisible = !IsLawyersListVisible;
    }

    [RelayCommand]
    public async Task ToggleBookmarkPanelAsync()
    {
        if (IsBookmarkVisible)
            await SlideOutPanel(BookmarkPanelRef);
        else
            await SlideInPanel(BookmarkPanelRef);
        if (IsSettingsVisible)
            await SlideOutPanel(SettingsPanelRef);

        IsBookmarkVisible = !IsBookmarkVisible;
    }

    [RelayCommand]
    public async Task ToggleSettingsPanelAsync()
    {
        if (IsSettingsVisible)
            await SlideOutPanel(SettingsPanelRef);
        else
            await SlideInPanel(SettingsPanelRef);
        if (IsBookmarkVisible)
            await SlideOutPanel(BookmarkPanelRef);

        IsSettingsVisible = !IsSettingsVisible;
    }
    #endregion

    #region Animation Helpers
    // مقادیر Ref باید در Code-behind تنظیم شود (با DI یا BindingContext)
    public Grid LawyersListPanelRef { get; set; }
    public Grid BookmarkPanelRef { get; set; }
    public Grid SettingsPanelRef { get; set; }

    private async Task SlideInPanel(VisualElement panel)
    {
        if (panel == null) return;

        // مطمئن می‌شیم که پنل قابل مشاهده باشه
        panel.IsVisible = true;

        // مقدار اولیه TranslationX رو خارج از صفحه می‌بریم
        var width = Application.Current.MainPage?.Width > 0
                    ? Application.Current.MainPage.Width
                    : panel.Width;

        panel.TranslationX = width;

        // حالا انیمیشن ورود
        await panel.TranslateTo(0, 0, 400, Easing.CubicOut);
    }

    private async Task SlideOutPanel(VisualElement panel)
    {
        if (panel == null) return;

        var width = Application.Current.MainPage?.Width > 0
                    ? Application.Current.MainPage.Width
                    : panel.Width;

        // انیمیشن خروج
        await panel.TranslateTo(width, 0, 400, Easing.CubicIn);

        // بعد از خروج، نامرئی کنیم
        panel.IsVisible = false;
    }

    #endregion

    #region Lawyers & Notes
    private async Task InitializeLawyersAsync()
    {
        string jsonPath = Path.Combine(FileSystem.AppDataDirectory, "Lawyers.json");
        if (!File.Exists(jsonPath))
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("Lawyers.json");
            using var fileStream = File.Create(jsonPath);
            await stream.CopyToAsync(fileStream);
        }

        await _lawyerService.SeedDataFromJsonAsync(jsonPath);
        var allLawyersList = await _lawyerService.GetAllLawyersAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AllLawyers.Clear();
            foreach (var l in allLawyersList) AllLawyers.Add(l);

            var cityList = allLawyersList
                .Select(l => l.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            cityList.Insert(0, "همه");

            Cities.Clear();
            foreach (var c in cityList) Cities.Add(c);
        });

        await LoadNotesAsync();
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        await Task.Yield();

        try
        {
            IEnumerable<Lawyer> filtered = AllLawyers;

            if (!string.IsNullOrWhiteSpace(SelectedCity) && SelectedCity != "همه")
                filtered = filtered.Where(l => string.Equals(l.City, SelectedCity, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchQuery))
                filtered = filtered.Where(l => (!string.IsNullOrEmpty(l.FullName) && l.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                              (!string.IsNullOrEmpty(l.PhoneNumber) && l.PhoneNumber.Contains(SearchQuery)));

            var filteredList = filtered.ToList();
            var pagedLawyers = filteredList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

            if (CurrentPage == 1) Lawyers.Clear();

            foreach (var lawyer in pagedLawyers)
                if (!Lawyers.Any(x => x.Id == lawyer.Id)) Lawyers.Add(lawyer);

            HasMorePages = (CurrentPage * PageSize) < filteredList.Count;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadNotesAsync Error: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] public async Task LoadNextPageAsync() { if (!HasMorePages || IsBusy) return; CurrentPage++; await LoadNotesAsync(); }

    partial void OnSelectedCityChanged(string value) { CurrentPage = 1; Lawyers.Clear(); LoadNotesAsync().SafeFireAndForget(); }
    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        Task.Run(async () =>
        {
            await Task.Delay(200, ct);
            if (!ct.IsCancellationRequested)
            {
                CurrentPage = 1;
                Lawyers.Clear();
                await LoadNotesAsync();
            }
        }, ct);
    }
    #endregion

    #region Bookmarks
    private async Task LoadBookmarksAsync()
    {
        try
        {
            var items = await _lawService.GetBookmarkedLawsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BookmarkedLaws.Clear();
                foreach (var law in items) BookmarkedLaws.Add(law);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadBookmarksAsync Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleBookmark(LawItem law)
    {
        if (law == null) return;
        law.IsBookmarked = !law.IsBookmarked;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!law.IsBookmarked)
            {
                var item = BookmarkedLaws.FirstOrDefault(x => x.Id == law.Id);
                if (item != null) BookmarkedLaws.Remove(item);
            }
        });

        WeakReferenceMessenger.Default.Send(new BookmarkChangedMessage(law.Id, law.IsBookmarked));
    }

    [RelayCommand]
    private async Task OpenArticleAsync(LawItem law)
    {
        if (law == null) return;
        await App.Current.MainPage.DisplayAlert($"تبصره: {law.Title}", law.NotesText, "برگشت", FlowDirection.RightToLeft);
    }
    #endregion

    #region SettingsPanel
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vakilaw.db");
            string fileName = $"vakilawBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db";

            using var sourceStream = File.OpenRead(dbPath);

            var fileResult = await FileSaver.Default.SaveAsync(fileName, sourceStream);

            if (fileResult != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    LocalizationService.Instance["BackupSaved"],
                    LocalizationService.Instance["BackupSavedSuccess"],
                    LocalizationService.Instance["OK"]);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                LocalizationService.Instance["Error"],
                ex.Message,
                LocalizationService.Instance["OK"]);
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        try
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.iOS, new[] { "public.database" } },
            { DevicePlatform.Android, new[] { "application/octet-stream" } },
            { DevicePlatform.WinUI, new[] { ".db" } },
            { DevicePlatform.MacCatalyst, new[] { "public.database" } }
        });

            var pickOptions = new PickOptions
            {
                PickerTitle = LocalizationService.Instance["SelectBackupFile"],
                FileTypes = fileTypes
            };


            var fileResult = await FilePicker.Default.PickAsync(pickOptions);
            if (fileResult == null)
                return;

            // مسیر دیتابیس اپلیکیشن
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vakilaw.db");

            using var sourceStream = await fileResult.OpenReadAsync();
            using var destStream = File.Open(dbPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destStream);

            await Application.Current.MainPage.DisplayAlert(
                LocalizationService.Instance["RestoreSuccessTitle"],
                LocalizationService.Instance["RestoreSuccessMessage"],
                LocalizationService.Instance["OK"]);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                LocalizationService.Instance["Error"],
                ex.Message,
                LocalizationService.Instance["OK"]);
        }
    }
    #endregion
}