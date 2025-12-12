using AsyncAwaitBestPractices;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Json;
using Vakilaw.Models;
using Vakilaw.Services;
using Vakilaw.Views.Popups;

namespace Vakilaw.ViewModels
{
    public partial class SmsPanelVM : ObservableObject
    {
        private readonly ClientService _clientService;
        private readonly SmsService _smsService;

        [ObservableProperty]
        private ObservableCollection<Client> clients = new();

        [ObservableProperty]
        private Client selectedClient;

        [ObservableProperty]
        private string singleMessage;

        [ObservableProperty]
        private string groupMessage;

        // ============================
        // گروهی
        // ============================
        [ObservableProperty]
        private ObservableCollection<SelectableClient> filteredClients = new();

        [ObservableProperty]
        private string groupSearchText;

        [ObservableProperty]
        private bool isAllSelected;

        partial void OnIsAllSelectedChanged(bool value)
        {
            if (FilteredClients == null) return;

            // فقط وقتی خود "انتخاب همه" تغییر کرد، همه آپدیت بشن
            foreach (var c in FilteredClients)
                c.IsSelected = value;
        }

        // ============================
        // تاریخچه
        // ============================
        [ObservableProperty]
        private ObservableCollection<SmsHistoryItem> smsHistory = new();

        // Popup ها
        [ObservableProperty] private bool isSingleDetailsVisible;
        [ObservableProperty] private bool isGroupDetailsVisible;
        [ObservableProperty] private string singleDetailsText;
        [ObservableProperty] private string groupDetailsText;

        #region Panels
        private readonly ReminderService _reminderService;
        private CancellationTokenSource _searchCts;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int pageSize = 8;

        [ObservableProperty]
        private bool hasMorePages = true;

        //[ObservableProperty]
        //private string selectedTheme = Preferences.Get("AppTheme", "Dark");

        [ObservableProperty]
        public ObservableCollection<ReminderModel> items = new ObservableCollection<ReminderModel>();

        [ObservableProperty]
        private string searchQuery;
        #endregion

        #region SMS
        [ObservableProperty] private bool isContractsVisible = true;
        [ObservableProperty] private bool isBillsVisible;
        //[ObservableProperty] private bool isWelcomeVisible = true;

        [ObservableProperty] private string selectedTab = "SMSPanel"; // "SMSPanel", "ReminderPanel"

        [ObservableProperty] private string contractContent;      
        #endregion

        #region Reminders
        [ObservableProperty] private List<string> billTitles;
        [ObservableProperty] private string selectedBillTitle;
        [ObservableProperty] private string billContent;

        #endregion

        [RelayCommand]
        private async Task ShowSMSPanels()
        {
            SelectedTab = "SMSPanel";
            ResetPanels();
            IsContractsVisible = true;        
            await AnimatePanel("SMSPanel");
        }

        [RelayCommand]
        private async Task ShowReminders()
        {
            SelectedTab = "Reminder";
            ResetPanels();
            IsBillsVisible = true;
            await AnimatePanel("ReminderPanel");
        }

        private void ResetPanels()
        {
            IsContractsVisible = false;
            IsBillsVisible = false;
            //IsWelcomeVisible = false;
        }

        private async Task AnimatePanel(string panelName)
        {
            // اینجا بعداً در CodeBehind می‌تونیم انیمیشن FadeIn/Slide بذاریم
            await Task.Delay(50);
        }

        public SmsPanelVM(ClientService clientService, SmsService smsService , ReminderService reminderService)
        {
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            LoadClients();
            LoadHistory();

            _reminderService = reminderService ?? throw new ArgumentNullException(nameof(reminderService));
            //vm.ApplyTheme(selectedTheme);
            WeakReferenceMessenger.Default.Register<NoteAddedMessage>(this, async (r, m) =>
            {
                await Task.Run(() => LoadNotesCommand.Execute(null));
            });
            Task.Run(() => LoadNotesCommand.Execute(null));
        }

        private void LoadClients()
        {
            var list = _clientService.GetClients();
            Clients = new ObservableCollection<Client>(list);

            FilteredClients = new ObservableCollection<SelectableClient>(
                Clients.Select(c => new SelectableClient
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    PhoneNumber = c.PhoneNumber
                })
            );

            foreach (var c in FilteredClients)
            {
                c.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectableClient.IsSelected))
                    {
                        // فقط وضعیت IsAllSelected رو آپدیت کن، نه چیز دیگه
                        if (FilteredClients.All(x => x.IsSelected))
                            isAllSelected = true;   // 👈 دقت کن: اینجا مستقیم فیلد رو ست می‌کنیم
                        else
                            isAllSelected = false;  // نه setter پراپرتی (IsAllSelected)
                    }
                };
            }
        }

        private void UpdateIsAllSelected()
        {
            if (FilteredClients == null || FilteredClients.Count == 0)
            {
                IsAllSelected = false;
                return;
            }

            // اگر همه انتخاب بودن → true
            // اگر حتی یکی انتخاب نشده بود → false
            IsAllSelected = FilteredClients.All(c => c.IsSelected);
        }

        private async void LoadHistory()
        {
            var list = await _smsService.GetHistoryAsync();
            SmsHistory = new ObservableCollection<SmsHistoryItem>(list);
        }

        // ============================
        // ارسال تکی
        // ============================
        [RelayCommand]
        private async Task SendSingleSms()
        {
            if (SelectedClient == null)
            {
                await Toast.Make("لطفاً یک موکل انتخاب کنید", ToastDuration.Short).Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(SingleMessage))
            {
                await Toast.Make("متن پیام خالی است", ToastDuration.Short).Show();
                return;
            }

            try
            {
                await _smsService.SendSingleAsync(
                    SelectedClient.PhoneNumber,
                    SingleMessage,
                    SelectedClient.FullName // 📌 نام موکل برای ذخیره تاریخچه
                );

                // ری‌لود تاریخچه
                LoadHistory();

                SingleMessage = string.Empty;
            }
            catch (Exception ex)
            {
                await Toast.Make($"{ex.Message} خطا در ارسال پیامک", ToastDuration.Short).Show();
            }
        }

        // ============================
        // ارسال گروهی
        // ============================
        [RelayCommand]
        private async Task SendGroupSms()
        {
            var selected = FilteredClients.Where(c => c.IsSelected).ToList();

            if (!selected.Any())
            {
                await Toast.Make("هیچ موکلی انتخاب نشده است", ToastDuration.Short).Show();
                return;
            }

            // ✅ حداقل دو موکل باید انتخاب شوند
            if (selected.Count < 2)
            {
                await Toast.Make("حداقل دو موکل باید انتخاب شوند", ToastDuration.Short).Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(GroupMessage))
            {
                await Toast.Make("متن پیام خالی است", ToastDuration.Short).Show();
                return;
            }

            var phoneNumbers = selected.Select(c => c.PhoneNumber).ToList();
            await _smsService.SendGroupAsync(phoneNumbers, GroupMessage);

            // ری‌لود تاریخچه
            LoadHistory();

            GroupMessage = string.Empty;
            foreach (var c in FilteredClients) c.IsSelected = false;
            IsAllSelected = false;
        }

        // ============================
        // سرچ گروهی
        // ============================
        partial void OnGroupSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredClients = new ObservableCollection<SelectableClient>(
                    Clients.Select(c => new SelectableClient
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        PhoneNumber = c.PhoneNumber
                    })
                );
            }
            else
            {
                var query = Clients.Where(c =>
                    (!string.IsNullOrEmpty(c.FullName) && c.FullName.Contains(value)) ||
                    (!string.IsNullOrEmpty(c.PhoneNumber) && c.PhoneNumber.Contains(value))
                );

                FilteredClients = new ObservableCollection<SelectableClient>(
                    query.Select(c => new SelectableClient
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        PhoneNumber = c.PhoneNumber
                    })
                );
            }
        }

        // ============================
        // نمایش جزئیات پیام
        // ============================
        [RelayCommand]
        private async Task ShowSmsDetails(int id)
        {
            var sms = SmsHistory.FirstOrDefault(x => x.Id == id);
            if (sms == null) return;

            if (sms.IsGroup)
            {
                GroupDetailsText = sms.Message;
                IsGroupDetailsVisible = true;
            }
            else
            {
                SingleDetailsText = sms.Message;
                IsSingleDetailsVisible = true;
            }
        }

        [RelayCommand] private void CloseSingleDetails() => IsSingleDetailsVisible = false;
        [RelayCommand] private void CloseGroupDetails() => IsGroupDetailsVisible = false;


        //Panels

        [RelayCommand]
        private async Task OpenReminderAsync()
        {
            var vm = new ReminderViewModel(_reminderService);
            var popup = new ReminderPopup(vm);

            popup.OnSaved += reminder =>
            {
                // اینجا ذخیره در SQLite یا ارسال نوتیفیکیشن یا...
                Debug.WriteLine("Reminder Saved:");
                Debug.WriteLine(reminder.Title);
            };

            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        public async Task LoadNotes()
        {
            var pagedNotes = await _reminderService.GetNotesPagedAsync(CurrentPage, PageSize);
            Items.Clear();
            foreach (var note in pagedNotes)
            {
                Items.Add(note);
            }

            // بررسی اینکه آیا صفحه بعدی وجود دارد
            HasMorePages = pagedNotes.Count >= PageSize;
        }

        [RelayCommand]
        public async Task LoadNextPageAsync()
        {
            if (!HasMorePages) return;
            CurrentPage++;
            await LoadNotes();
        }

        [RelayCommand]
        public async Task LoadPreviousPageAsync()
        {
            if (CurrentPage <= 1) return;
            CurrentPage--;
            await LoadNotes();
        }

        [ObservableProperty]
        private bool isBusy; // برای ActivityIndicator

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsBusy = true; // فوراً نمایش Indicator

            await Task.Yield();

            CurrentPage = 1;
            await LoadNotes();

            await Task.Delay(1000); // فقط برای تست تأخیر

            IsBusy = false; // پنهان کردن Indicator
        }

        partial void OnSearchQueryChanged(string value)
        {
            SearchAsync().SafeFireAndForget();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            try
            {
                var q = SearchQuery?.Trim();
                if (string.IsNullOrEmpty(q))
                {
                    await LoadNotes();
                    return;
                }

                // debounce کوتاه (300ms)
                await Task.Delay(150, ct);

                var result = await Task.Run(() => _reminderService.SearchNotes(q), ct);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Items = new ObservableCollection<ReminderModel>(result);
                });
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {

            }
        }

        // حذف با تأیید کاربر و اجرای حذف در بک‌گراند
        [RelayCommand]
        private async Task DeleteNoteAsync(ReminderModel note)
        {
            if (note == null) return;

            bool confirmed = await Application.Current.MainPage.DisplayAlert(
            LocalizationService.Instance["DeleteNoteWarningAlert"],
            LocalizationService.Instance["DeleteNoteAlertContent"],
            LocalizationService.Instance["DeleteNoteOK"],
            LocalizationService.Instance["DeleteNoteCancel"]);

            if (!confirmed) return;

            try
            {
                await Task.Run(() => _reminderService.DeleteNote(note.Id));
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Items.Remove(note);
                });
            }
            catch (Exception ex)
            {

            }
        }

        // کش کردن Popup
        private ShowDetails _popupDetail;

        // نمایش جزئیات (Popup)
        [RelayCommand]
        private async Task ShowDetailsAsync(ReminderModel note)
        {
            if (note == null) return;

            if (_popupDetail == null)
                _popupDetail = new ShowDetails(note);
            else
            {
                _popupDetail.BindingContext = note;
            }

            // نمایش
            if (!Mopups.Services.MopupService.Instance.PopupStack.Contains(_popupDetail))
                await Mopups.Services.MopupService.Instance.PushAsync(_popupDetail);
        }
    }
}