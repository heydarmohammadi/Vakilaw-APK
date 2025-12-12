using Mopups.Pages;
using Mopups.Services;
using Vakilaw.Models;
using Vakilaw.ViewModels;

namespace Vakilaw.Views.Popups;

public partial class ReminderPopup : PopupPage
{
    public event Action<ReminderModel>? OnSaved;

    private DateTime? _selectedDate;

    public ReminderPopup(ReminderViewModel vm)
    {
        InitializeComponent();
     
        // انیمیشن ورود
        this.Opacity = 0;
        this.Scale = 0.8;

        this.FadeTo(1, 200, Easing.CubicOut);
        this.ScaleTo(1, 250, Easing.SpringOut);

        BindingContext = vm;
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await CloseSmooth();
    } 

    private async Task CloseSmooth()
    {
        await this.ScaleTo(0.8, 200, Easing.CubicIn);
        await this.FadeTo(0, 150);
        await MopupService.Instance.PopAsync();
    }
}