using Mopups.Pages;
using Mopups.Services;
using Vakilaw.Models;
using Vakilaw.Services;


namespace Vakilaw.Views.Popups
{
    public partial class ShowDetails : PopupPage
    {
        public ShowDetails(ReminderModel notemodel)
        {
            InitializeComponent();
            BindingContext = notemodel;

            PopupContainer.Opacity = 0;
            PopupContainer.Scale = 0.85;

            LocalizationService.Instance.UpdateFlowDirection(this);
            LocalizationService.Instance.LanguageChanged += () =>
            {
                LocalizationService.Instance.UpdateFlowDirection(this);
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await Task.Delay(10);

            // اجرای انیمیشن نرم‌تر و سبک‌تر
            await PopupContainer.FadeTo(1, 200, Easing.CubicOut);
            await PopupContainer.ScaleTo(1, 200, Easing.SinOut);
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            // بستن سریع‌تر و روان‌تر
            await PopupContainer.ScaleTo(0.85, 150, Easing.SinIn);
            await PopupContainer.FadeTo(0, 150, Easing.CubicIn);
        }

        private async void ClosePopup(object sender, EventArgs e)
        {
            await MopupService.Instance.PopAsync();
        }
    }
}