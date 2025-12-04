using Mopups.Pages;
using Vakilaw.Models;
using Vakilaw.ViewModels;

namespace Vakilaw.Views.Popups;

public partial class AddClientPopup : PopupPage
{
    private readonly ClientService _clientService;
    public AddClientPopup(ClientService clientService, ClientsAndCasesViewModel clientsAndCasesViewModel)
    {
        InitializeComponent();
        _clientService = clientService;
        BindingContext = new AddClientPopupViewModel(this, clientsAndCasesViewModel, _clientService);
    }

    // Event برای خروجی موکل جدید
    public event Action<Client> ClientCreated;

    public void RaiseClientCreated(Client newClient)
    {
        ClientCreated?.Invoke(newClient);
    }
}