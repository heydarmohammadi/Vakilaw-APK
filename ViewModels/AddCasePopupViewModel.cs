using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using System.Collections.ObjectModel;
using Vakilaw.Models;
using Vakilaw.Views.Popups;

namespace Vakilaw.ViewModels
{
    public partial class AddCasePopupViewModel : ObservableObject
    {
        private readonly AddCasePopup _popup;
        private readonly ClientsAndCasesViewModel _clientsAndCasesViewModel;
        private readonly Client _client;   

        [ObservableProperty] private string clientName;
        [ObservableProperty] private ObservableCollection<CaseAttachment> attachments = new();

        [ObservableProperty] private bool endDateIsEnabled = false;
        [ObservableProperty] private string title;
        [ObservableProperty] private string caseNumber;
        [ObservableProperty] private string courtName;
        [ObservableProperty] private string judgeName;
        [ObservableProperty] private string startDate;
        [ObservableProperty] private string? endDate;
        [ObservableProperty] private string status;
        [ObservableProperty] private string description;

        public AddCasePopupViewModel(AddCasePopup popup, ClientsAndCasesViewModel clientsAndCasesViewModel, Client client)
        {
            _popup = popup;
            _clientsAndCasesViewModel = clientsAndCasesViewModel;
            _client = client;
            ClientName = client.FullName;
        }

        partial void OnStatusChanged(string value)
        {
            EndDateIsEnabled = value == "مختومه";
        }

        public int ClientId => _client.Id;

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Title)  ||
            string.IsNullOrWhiteSpace(CaseNumber) ||
            string.IsNullOrWhiteSpace(StartDate)  ||
            string.IsNullOrWhiteSpace(Status))          
            {
                await Toast.Make("لطفاً فیلدهای ضروری را پر کنید!", ToastDuration.Short).Show();                
                return;
            }

            var casenumberLength = CaseNumber.Trim().Length;

            if (casenumberLength > 18 || casenumberLength < 16)
            {
                await Toast.Make("شماره پرونده نامعتبر است!", ToastDuration.Short).Show();              
                return;
            }

            // فقط یک Case جدید بساز، بدون ذخیره در دیتابیس
            var newCase = new Case
            {
                Title = Title,
                CaseNumber = CaseNumber,
                CourtName = CourtName,
                JudgeName = JudgeName,
                StartDate = StartDate,
                EndDate = EndDate,
                Status = Status,
                Description = Description,
                ClientId = _client.Id,
                Client = _client,
                CaseAttachments = Attachments.ToList() // فقط پاس دادن لیست
            };

            _popup.RaiseCaseCreated(newCase);
       
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task AddAttachment()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/pdf", "image/*" } },
                { DevicePlatform.WinUI, new[] { ".pdf", ".jpg", ".jpeg", ".png" } },
                { DevicePlatform.iOS, new[] { "com.adobe.pdf", "public.image" } },
                { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf", "public.image" } }
            });

                var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "انتخاب فایل‌های پیوست",
                    FileTypes = customFileType
                });

                if (result != null)
                {
                    foreach (var file in result)
                    {
                        Attachments.Add(new CaseAttachment
                        {
                            FileName = Path.GetFileName(file.FullPath),
                            FilePath = file.FullPath,
                            FileType = Path.GetExtension(file.FullPath).ToLower()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("خطا", $"انتخاب فایل با مشکل مواجه شد:\n{ex.Message}", "باشه");
            }
        }

        [RelayCommand]
        private void RemoveAttachment(CaseAttachment attachment)
        {
            if (attachment != null && Attachments.Contains(attachment))
                Attachments.Remove(attachment);
        }
    }
}