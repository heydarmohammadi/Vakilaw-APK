using Mopups.Pages;
using Vakilaw.Models;
using Vakilaw.Services;
using Vakilaw.ViewModels;

namespace Vakilaw.Views.Popups;

public partial class AddCasePopup : PopupPage
{
    public event Action<Case> CaseCreated;

    public AddCasePopup(Client client, ClientsAndCasesViewModel clientsAndCasesViewModel)
    {
        InitializeComponent();
        BindingContext = new AddCasePopupViewModel(this, clientsAndCasesViewModel, client);

        LocalizationService.Instance.UpdateFlowDirection(this);
        LocalizationService.Instance.LanguageChanged += () =>
        {
            LocalizationService.Instance.UpdateFlowDirection(this);
        };
    }

    public void RaiseCaseCreated(Case newCase)
    {
        CaseCreated?.Invoke(newCase);
    }
}