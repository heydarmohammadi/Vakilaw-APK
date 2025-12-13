using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using Plugin.LocalNotification;
using System.Runtime.InteropServices;
using Vakilaw.Services;
using Vakilaw.ViewModels;
using Vakilaw.Views;
using Vakilaw.Views.Popups;
#if ANDROID
using Vakilaw.Platforms.Android;
#endif
#if IOS
using Vakilaw.Platforms.iOS;
#endif


namespace Vakilaw;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureMopups()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("IRANSansWeb Persian.ttf", "IRANSansWeb");
                fonts.AddFont("Sahel.ttf", "Sahel");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // مسیر دیتابیس
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vakilaw.db");

        // -------------------- سرویس‌ها --------------------
        builder.Services.AddSingleton(s => new DatabaseService(dbPath));
        builder.Services.AddSingleton<LawService>(s => new LawService(s.GetRequiredService<DatabaseService>()));
        builder.Services.AddSingleton<LawImporter>(s => new LawImporter(s.GetRequiredService<LawService>()));
        builder.Services.AddSingleton<UserService>();       
        builder.Services.AddSingleton<SmsService>();
        builder.Services.AddSingleton<ReminderService>();
        builder.Services.AddSingleton<TransactionService>();
        builder.Services.AddSingleton(sp =>
        new LicenseService(sp.GetRequiredService<DatabaseService>(), "<PUBLIC_KEY_BASE64_HERE>"));
        builder.Services.AddSingleton<LawyerService>(s => new LawyerService(s.GetRequiredService<DatabaseService>()));

        builder.Services.AddSingleton<ClientService>();
        builder.Services.AddSingleton<CaseService>();

        // -------------------- ویومدل‌ها --------------------
        builder.Services.AddSingleton<MainPageVM>(); // Singleton برای حفظ داده‌ها
        builder.Services.AddSingleton<LawBankVM>();

        builder.Services.AddTransient<LawyerSubmitVM>();
        builder.Services.AddTransient<SubscriptionPopupVM>();

        builder.Services.AddTransient<ClientsAndCasesViewModel>();
        builder.Services.AddTransient<DocumentsViewModel>();
        builder.Services.AddTransient<SmsPanelVM>();
        builder.Services.AddTransient<ReminderViewModel>();
        builder.Services.AddTransient<TransactionsVM>();       
        builder.Services.AddTransient<ReportsVM>();

        // -------------------- صفحات --------------------
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LawBankPage>();
        builder.Services.AddTransient<LawyerSubmitPopup>();
        builder.Services.AddTransient<SubscriptionPopup>();

        builder.Services.AddTransient<ClientsAndCasesPage>();
        builder.Services.AddTransient<DocumentsPage>();
        builder.Services.AddTransient<SMSPanelPage>();
        builder.Services.AddTransient<TransactionsPage>();
        builder.Services.AddTransient<ReportsPage>();

#if ANDROID
        builder.Services.AddSingleton<IPrinterService, Vakilaw.Platforms.Android.PrinterService>();
#endif
#if IOS
        builder.Services.AddSingleton<IPrinterService, Vakilaw.Platforms.iOS.PrinterService>();
#endif

        // -------------------- خود App --------------------
        builder.Services.AddSingleton<App>();

        return builder.Build();
    }
}