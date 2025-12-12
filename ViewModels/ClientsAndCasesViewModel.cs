using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using System.Collections.ObjectModel;
using Vakilaw.Models;
using Vakilaw.Views.Popups;

namespace Vakilaw.ViewModels
{
    public partial class ClientsAndCasesViewModel : ObservableObject
    {
        [ObservableProperty] private bool isClientsVisible = true;
        [ObservableProperty] private bool isCasesVisible = false;
        [ObservableProperty] private string countsLabel;

        private readonly ClientService _clientService;
        private readonly CaseService _caseService;

        private System.Timers.Timer _debounceTimerClients;
        private System.Timers.Timer _debounceTimerCases;

        public ClientsAndCasesViewModel(ClientService clientService, CaseService caseService)
        {
            _clientService = clientService;
            _caseService = caseService;

            _ = LoadCountsAsync();

            SearchClients();
            LoadClientsWithCases();
        }

        #region Properties
        [ObservableProperty] private ObservableCollection<ClientWithCasesViewModel> clientsWithCases = new();
        [ObservableProperty] private ObservableCollection<Client> clients = new();
        [ObservableProperty] private Client selectedClient;

        [ObservableProperty] private string clientSearchText;
        [ObservableProperty] private string caseSearchText;

        [ObservableProperty] private ObservableCollection<Case> cases = new();
        [ObservableProperty] private Case selectedCase;

        [ObservableProperty] private int clientId;
        [ObservableProperty] private string fullName;
        [ObservableProperty] private string nationalCode;
        [ObservableProperty] private string phoneNumber;
        [ObservableProperty] private string address;
        [ObservableProperty] private string clientDescription;

        [ObservableProperty] private string title;
        [ObservableProperty] private string caseNumber;
        [ObservableProperty] private string courtName;
        [ObservableProperty] private string judgeName;
        [ObservableProperty] private string startDate;
        [ObservableProperty] private string? endDate;
        [ObservableProperty] private string status;
        [ObservableProperty] private string caseDescription;
        #endregion

        #region ShowClients / ShowCases
        [RelayCommand]
        private async Task ShowClients()
        {
            IsClientsVisible = true;
            IsCasesVisible = false;
            CountsLabel = $"تعداد موکل‌ها: {await _clientService.GetClientsCount()}";
        }

        [RelayCommand]
        public async Task ShowCases()
        {
            IsClientsVisible = false;
            IsCasesVisible = true;
            CountsLabel = $"تعداد پرونده‌ها: {await _caseService.GetCasesCount()}";
        }

        public async Task LoadCountsAsync()
        {
            var count = await _clientService.GetClientsCount();
            CountsLabel = $"تعداد موکل‌ها: {count}";
        }

        public async Task LoadCaseCountsAsync()
        {
            var count = await _caseService.GetCasesCount();
            CountsLabel = $"تعداد پرونده‌ها: {count}";
        }
        #endregion

        #region Clients
        public void LoadClientsWithCases()
        {
            ClientsWithCases.Clear();
            var clientsList = _clientService.GetClients();
            foreach (var c in clientsList)
            {
                var wrapper = new ClientWithCasesViewModel(c, _clientService, _caseService, this);
                ClientsWithCases.Add(wrapper);
            }
        }

        [RelayCommand]
        private void SearchClients()
        {
            ClientsWithCases.Clear();
            var filteredClients = _clientService.SearchClients(ClientSearchText);

            foreach (var client in filteredClients)
            {
                var wrapper = new ClientWithCasesViewModel(client, _clientService, _caseService, this);
                ClientsWithCases.Add(wrapper);
            }
        }

        partial void OnClientSearchTextChanged(string value)
        {
            _debounceTimerClients?.Stop();
            _debounceTimerClients = new System.Timers.Timer(400) { AutoReset = false };
            _debounceTimerClients.Elapsed += (s, e) => SearchClients();
            _debounceTimerClients.Start();
        }

        [RelayCommand]
        private void AddClient(Client newClient)
        {
            if (newClient == null) return;
            var wrapper = new ClientWithCasesViewModel(newClient, _clientService, _caseService, this);
            ClientsWithCases.Add(wrapper);
            FullName = NationalCode = PhoneNumber = Address = ClientDescription = string.Empty;           
        }

        [RelayCommand]
        private async Task UpdateClient()
        {
            if (SelectedClient == null) return;
            await _clientService.UpdateClient(SelectedClient);
            SearchClients();
            LoadClientsWithCases();
        }

        [RelayCommand]
        private async Task DeleteClient()
        {
            if (SelectedClient == null) return;
            await _clientService.DeleteClient(SelectedClient.Id);
            SearchClients();
            LoadClientsWithCases();
        }

        [RelayCommand]
        private async Task ShowAddClientPopup()
        {
            var popup = new AddClientPopup(_clientService , this);
            popup.ClientCreated += newClient =>
            {
                var wrapper = new ClientWithCasesViewModel(newClient, _clientService, _caseService, this);
                ClientsWithCases.Add(wrapper);
                SearchClients();
            };
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        private async Task ShowClientDetailsAsync(Client client)
        {
            if (client == null) return;
            var popup = new ClientDetailsPopup(client, _clientService);
            await MopupService.Instance.PushAsync(popup);
        }
        #endregion

        #region Cases
        partial void OnCaseSearchTextChanged(string value)
        {
            _debounceTimerCases?.Stop();
            _debounceTimerCases = new System.Timers.Timer(400) { AutoReset = false };
            _debounceTimerCases.Elapsed += async (s, e) => await SearchCases();
            _debounceTimerCases.Start();
        }

        [RelayCommand]
        private async Task SearchCases()
        {
            Cases.Clear();
            var list = _caseService.SearchCases(CaseSearchText);
            foreach (var c in list) Cases.Add(c);
        }

        [RelayCommand]
        private async Task AddCase(Case caseItem)
        {
            if (SelectedClient == null || caseItem == null) return;

            // ✅ ذخیره پرونده و Attachments فقط اینجا
            await _caseService.AddCase(caseItem);
            
            // اضافه کردن به Wrapper فقط برای UI
            var wrapper = ClientsWithCases.FirstOrDefault(w => w.Client.Id == caseItem.ClientId);
            if (wrapper != null)
            {
                await wrapper.AddCaseToListAsync(caseItem);
            }

            // ریست فرم
            Title = CaseNumber = CourtName = JudgeName = Status = CaseDescription = string.Empty;
            EndDate = null;
         
            await SearchCases();
        }

        [RelayCommand]
        private async Task UpdateCase()
        {
            if (SelectedCase == null) return;

            // ✅ بروزرسانی دیتابیس
            await _caseService.UpdateCase(SelectedCase);

            // ✅ به جای Refresh کل لیست، همون آیتم انتخابی رو بروزرسانی کن
            var wrapper = ClientsWithCases.FirstOrDefault(w => w.Client.Id == SelectedCase.ClientId);
            if (wrapper != null)
            {
                var index = wrapper.Cases.IndexOf(wrapper.Cases.FirstOrDefault(c => c.Id == SelectedCase.Id));
                if (index >= 0)
                {
                    wrapper.Cases[index] = SelectedCase; // جایگزینی با نسخه جدید
                }
            }
        }

        [RelayCommand]
        private async Task DeleteCase()
        {
            if (SelectedCase == null) return;
            await _caseService.DeleteCase(SelectedCase.Id);

            // حذف از Wrapper
            var wrapper = ClientsWithCases.FirstOrDefault(w => w.Client.Id == SelectedCase.ClientId);
            wrapper?.RemoveCaseFromList(SelectedCase.Id);

            await SearchCases();
        }

        [RelayCommand]
        private async Task ShowAddCasePopup()
        {
            if (SelectedClient == null) return;

            var popup = new AddCasePopup(SelectedClient , this); // نیازی به CaseService نداریم چون ViewModel آن داخل Popup ساخته می‌شود
            popup.CaseCreated += async newCase =>
            {
                // ذخیره پرونده در DB
                await _caseService.AddCase(newCase);

                // ذخیره Attachments
                if (newCase.CaseAttachments != null)
                {
                    foreach (var att in newCase.CaseAttachments)
                        await _caseService.AddAttachment(att);
                }

                // اضافه کردن به Wrapper
                var wrapper = ClientsWithCases.FirstOrDefault(w => w.Client.Id == newCase.ClientId);
                if (wrapper != null)
                {
                   await wrapper.AddCaseToListAsync(newCase);
                }

                await SearchCases();
            };

            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        private async Task ShowCaseDetailsAsync(Case caseItem)
        {
            if (caseItem == null) return;

            var popup = new CaseDetailsPopup(caseItem, _caseService); // ✅ اضافه کردن CaseService
            await MopupService.Instance.PushAsync(popup);
        }

        #endregion
    }
}