using Vakilaw.Services;
using Vakilaw.ViewModels;

namespace Vakilaw.Views
{
    public partial class MainPage : ContentPage
    {

        private readonly ClientsAndCasesViewModel _vm1;
        private readonly TransactionsVM _vm2;

        public MainPage(MainPageVM vm, ClientsAndCasesViewModel vm1 , TransactionsVM vm2)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm1 = vm1;
            _vm2 = vm2;
            vm.AddButtonRef = AddButton;

            // رجیستر پنل‌ها برای انیمیشن
            vm.LawyersListPanelRef = LawyersListPanel;
            vm.BookmarkPanelRef = BookmarkPanel;
            vm.SettingsPanelRef = SettingsPanel;

            LocalizationService.Instance.UpdateFlowDirection(this);
            LocalizationService.Instance.LanguageChanged += () =>
            {
                LocalizationService.Instance.UpdateFlowDirection(this);
            };
            _vm2 = vm2;
        }
        private async void Card_Tapped(object sender, EventArgs e)
        {
            if (sender is Grid grid && grid.Parent is Border border)
            {
                try
                {
                    // Scale animation
                    await grid.ScaleTo(0.95, 70, Easing.CubicIn);

                    // Change color temporarily
                    var originalColor = border.BackgroundColor;
                    border.BackgroundColor = Colors.DarkSlateBlue;

                    await Task.Delay(80);

                    // Revert
                    await grid.ScaleTo(1.0, 70, Easing.CubicOut);
                    border.BackgroundColor = originalColor;

                    // Execute command based on CommandParameter
                    if (BindingContext is MainPageVM vm && sender is Grid g && g.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap)
                    {
                        var param = tap.CommandParameter?.ToString();
                        switch (param)
                        {
                            case "Lawyers":
                                if (vm.ToggleLawyersListCommand.CanExecute(null))
                                    vm.ToggleLawyersListCommand.Execute(null);
                                break;
                            case "LawBank":
                                if (vm.LawBankPageCommand.CanExecute(null))
                                    vm.LawBankPageCommand.Execute(null);
                                break;
                            case "Clients":
                                if (vm.ClientsAndCasesPageCommand.CanExecute(null))
                                    vm.ClientsAndCasesPageCommand.Execute(null);
                                break;
                            case "Documents":
                                if (vm.DocumentsPageCommand.CanExecute(null))
                                    vm.DocumentsPageCommand.Execute(null);
                                break;
                            case "SMS":
                                if (vm.SMSPanelPageCommand.CanExecute(null))
                                    vm.SMSPanelPageCommand.Execute(null);
                                break;
                            case "Transactions":
                                if (vm.TransactionsPageCommand.CanExecute(null))
                                    vm.TransactionsPageCommand.Execute(null);
                                break;
                            case "Reports":
                                if (vm.ReportsPageCommand.CanExecute(null))
                                    vm.ReportsPageCommand.Execute(null);
                                break;
                            case "OpenReminder":
                                if (vm.OpenReminderCommand.CanExecute(null))
                                    vm.OpenReminderCommand.Execute(null);
                                break;
                            case "AddClient":
                                if (_vm1.ShowAddClientPopupCommand.CanExecute(null))
                                    _vm1.ShowAddClientPopupCommand.Execute(null);
                                break;
                            case "AddCase":
                                if (vm.ClientsAndCasesPageCommand.CanExecute(null))
                                    vm.ClientsAndCasesPageCommand.Execute(null);
                               
                                break;
                            case "AddTransaction":
                                if (_vm2.ShowAddTransactionPopupCommand.CanExecute(null))
                                    _vm2.ShowAddTransactionPopupCommand.Execute(null);
                                break;
                        }
                    }                  
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Card_Tapped Error: " + ex.Message);
                }
            }
        }   
    }
}