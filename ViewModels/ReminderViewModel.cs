using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mopups.Services;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using Vakilaw.Models;
using Vakilaw.Services;

namespace Vakilaw.ViewModels
{
    public partial class ReminderViewModel : ObservableObject
    {
        private readonly ReminderService _reminderService;

        [ObservableProperty]
        private ReminderModel selectedNote = new ReminderModel();
     
        [ObservableProperty]
        private ObservableCollection<string> categories;

        public ReminderViewModel(ReminderService reminderService)
        {
            _reminderService = reminderService;

            LoadLocalizedItems();
            
            selectedNote.Category = Categories.FirstOrDefault();

            LocalizationService.Instance.LanguageChanged += () =>
            {
                LoadLocalizedItems();           
                OnPropertyChanged(nameof(Categories));
            };
        }

        private void LoadLocalizedItems()
        {         
            Categories = new ObservableCollection<string>
            {
                LocalizationService.Instance["Personal"],
                LocalizationService.Instance["Work"],
                LocalizationService.Instance["Educational"],
                LocalizationService.Instance["Financial"]
            };
        }

        [ObservableProperty]
        private bool isReminderSet;

        [ObservableProperty]
        private DateTime? reminderDate;

        [ObservableProperty]
        private bool isReminderDone;

        [ObservableProperty]
        private string selectedDateLabelText = string.Empty;

        [ObservableProperty]
        private bool isDateSelected = false;

        [RelayCommand]
        public async Task Save()
        {
            if (string.IsNullOrWhiteSpace(SelectedNote.Title) ||
                string.IsNullOrWhiteSpace(SelectedNote.Description) ||
                string.IsNullOrWhiteSpace(SelectedNote.Category) ||
                string.IsNullOrWhiteSpace(SelectedNote.ReminderDateString))             
            {
                await Toast.Make(LocalizationService.Instance["AddNoteRequiredToast"], ToastDuration.Short, 14).Show();
                return;
            }

            // بررسی و تبدیل تاریخ و ساعت یادآوری
            if (!string.IsNullOrWhiteSpace(SelectedNote.ReminderDateString))
            {
                var (convertedDate, errorMessage) = DatabaseHelper.ConvertShamsiToGregorian1(SelectedNote.ReminderDateString);
                if (convertedDate == null)
                {
                    await Toast.Make(errorMessage, ToastDuration.Short, 14).Show();
                    return;
                }
                SelectedNote.ReminderDate = convertedDate;
            }
            else
            {
                await Toast.Make(LocalizationService.Instance["EnterDateAndTimeToast"], ToastDuration.Short, 14).Show();
                return;
            }

            SelectedNote.CreatedAt = DateTime.UtcNow;
            selectedNote.IsReminderSet = true;
            SelectedNote.IsReminderDone = false;

            var newNoteId = await Task.Run(() => _reminderService.AddNoteAsync(SelectedNote));
            SelectedNote.Id = newNoteId;

            // ثبت نوتیفیکیشن
            if (SelectedNote.ReminderDate.HasValue)
            {
                var notifyTime = SelectedNote.ReminderDate.Value;

                if (notifyTime <= DateTime.Now)
                    notifyTime = DateTime.Now.AddSeconds(10);

                var request = new NotificationRequest
                {
                    NotificationId = SelectedNote.Id,
                    Title = "⚖️ یادآوری جلسه",
                    Description = SelectedNote.Title,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = notifyTime,
                        RepeatType = NotificationRepeat.No
                    }
                };
                await _reminderService.MarkReminderAsDoneAsync(selectedNote.Id);

#if ANDROID
        var granted = await Plugin.LocalNotification.LocalNotificationCenter.Current.RequestNotificationPermission();
        if (!granted)
        {
            await Shell.Current.DisplayAlert("دسترسی اعلان", "لطفاً مجوز ارسال اعلان را فعال کنید.", "باشه");
            return;
        }
#endif
                await Plugin.LocalNotification.LocalNotificationCenter.Current.Show(request);
            }

            await Toast.Make(LocalizationService.Instance["SuccessAddMeetingToast"], ToastDuration.Short, 14).Show();
            SelectedNote.Title = string.Empty;
            SelectedNote.Description = string.Empty;
            SelectedNote.Category = string.Empty;
           
            // بکاپ دیتابیس
            //_ = Task.Run(async () => await DatabaseHelper.ExportDatabaseToDownloadAsync());

            WeakReferenceMessenger.Default.Send(new NoteAddedMessage());
            await MopupService.Instance.PopAsync();
            //await Shell.Current.GoToAsync("..");
        }       
    }
    public class NoteAddedMessage { }
}