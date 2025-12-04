using Vakilaw.ViewModels;

namespace Vakilaw.Views;

public partial class ReportsPage : ContentPage
{
	public ReportsPage(ReportsVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}