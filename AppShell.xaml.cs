using Vakilaw.Views;

namespace Vakilaw
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("LawBankPage", typeof(LawBankPage));
            Routing.RegisterRoute("ClientsAndCasesPage", typeof(ClientsAndCasesPage));
            Routing.RegisterRoute("DocumentsPage", typeof(DocumentsPage));
            Routing.RegisterRoute("SMSPanelPage", typeof(SMSPanelPage));
            Routing.RegisterRoute("TransactionsPage", typeof(TransactionsPage));
            Routing.RegisterRoute("ReportsPage", typeof(ReportsPage));
        }    
    }
}