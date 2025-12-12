using Vakilaw.Services;
using Vakilaw.ViewModels;

namespace Vakilaw.Views;

public partial class SMSPanelPage : ContentPage
{
	public SMSPanelPage(ClientService clientService, SmsService smsService, ReminderService reminderService)
	{
		InitializeComponent();
        BindingContext = new SmsPanelVM(clientService, smsService, reminderService);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (SMSPanel.IsVisible)
            await SMSPanel.FadeTo(1, 400, Easing.CubicIn);
        if (ReminderPanel.IsVisible)
            await ReminderPanel.FadeTo(1, 400, Easing.CubicIn);
        //if (WelcomePanel.IsVisible)
        //    await WelcomePanel.FadeTo(1, 400, Easing.CubicIn);
    }

    double lastScrollY = 0;
    bool headerElevated = false;

    private async void NotesCollection_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        if (e.VerticalOffset > 5 && !headerElevated)
        {
            headerElevated = true;

            // انیمیشن ظاهر شدن سایه
            await Task.WhenAll(
                HeaderGrid.TranslateTo(0, 0, 150, Easing.CubicOut),
                HeaderGrid.FadeTo(1, 150, Easing.CubicInOut)
            );

            HeaderGrid.Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.2f,
                Radius = 5,
                Offset = new Point(0, 2)
            };
        }
        else if (e.VerticalOffset <= 5 && headerElevated)
        {
            headerElevated = false;

            // حذف سایه با انیمیشن نرم
            HeaderGrid.Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0f,
                Radius = 0,
                Offset = new Point(0, 0)
            };
        }
        lastScrollY = e.VerticalOffset;
    }
}