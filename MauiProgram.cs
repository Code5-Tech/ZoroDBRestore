using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ZoroDBRestore.Services;
using ZoroDBRestore.ViewModels;
using ZoroDBRestore.Views;

namespace ZoroDBRestore;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services (singleton so settings persist across tabs)
        builder.Services.AddSingleton<IToolExtractorService, ToolExtractorService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IMongoConnectionService, MongoConnectionService>();
        builder.Services.AddSingleton<ICliToolService, CliToolService>();

        // ViewModels
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<BackupViewModel>();
        builder.Services.AddTransient<RestoreViewModel>();
        builder.Services.AddTransient<ConnectionsViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<BackupPage>();
        builder.Services.AddTransient<RestorePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ConnectionsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Extract bundled tools then load settings (async fire-and-forget at startup)
        Task.Run(async () =>
        {
            var extractor = app.Services.GetRequiredService<IToolExtractorService>();
            await extractor.EnsureToolsExtractedAsync();

            var settingsService = app.Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();
        }).GetAwaiter().GetResult();

        return app;
    }
}
