using Microsoft.Maui.Controls;
using Vakilaw.Views;
using Vakilaw.Services;

namespace Vakilaw;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnStart()
    {
        base.OnStart();

        // نمایش نوتیفیکیشن خوش‌آمدگویی
        Task.Run(async () =>
        {
            var notification = new Plugin.LocalNotification.NotificationRequest
            {
                NotificationId = 1000,
                Title = "خوش آمد گویی",
                Description = "به اپلیکیشن حقوقی وکیلاو خوش آمدید",
                Schedule = new Plugin.LocalNotification.NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(10)
                }
            };

            await Plugin.LocalNotification.LocalNotificationCenter.Current.Show(notification);
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}