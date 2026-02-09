using System.Globalization;
using Microsoft.Extensions.Logging;
using Microcharts.Maui;
using PortfelStudenta.Services;
using PortfelStudenta.ViewModels;
using PortfelStudenta.Views;
using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
namespace PortfelStudenta
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var polishCulture = new CultureInfo("pl-PL");
            CultureInfo.DefaultThreadCurrentCulture = polishCulture;
            CultureInfo.DefaultThreadCurrentUICulture = polishCulture;
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
#if WINDOWS
            EntryHandler.Mapper.AppendToMapping("WhiteFocusBorder", (handler, view) =>
            {
                if (handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
                {
                    var whiteBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                    textBox.Resources["TextControlBorderBrushFocused"] = whiteBrush;
                    textBox.Resources["TextControlBorderBrushPointerOver"] = whiteBrush;
                }
            });
#endif
            builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton(sp =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                    MaxConnectionsPerServer = 10
                };
                return handler;
            });
            builder.Services.AddSingleton<INbpApiService>(sp =>
            {
                var client = new HttpClient(sp.GetRequiredService<HttpClientHandler>(), disposeHandler: false)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };
                client.DefaultRequestHeaders.Add("User-Agent", "PortfelStudenta/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                return new NbpApiService(client, sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<IDatabaseService>());
            });
            builder.Services.AddSingleton<ICoinCapApiService>(sp =>
            {
                var client = new HttpClient(sp.GetRequiredService<HttpClientHandler>(), disposeHandler: false)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };
                client.DefaultRequestHeaders.Add("User-Agent", "PortfelStudenta/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                return new CoinCapApiService(client, sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<IDatabaseService>(), sp.GetRequiredService<INbpApiService>());
            });
            builder.Services.AddSingleton<IYahooFinanceApiService>(sp =>
            {
                var client = new HttpClient(sp.GetRequiredService<HttpClientHandler>(), disposeHandler: false)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };
                client.DefaultRequestHeaders.Add("User-Agent", "PortfelStudenta/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                return new YahooFinanceApiService(client, sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<IDatabaseService>(), sp.GetRequiredService<INbpApiService>());
            });
            builder.Services.AddSingleton<IPortfolioService, PortfolioService>();
            builder.Services.AddSingleton<ICsvService, CsvService>();
            builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<MarketsViewModel>();
            builder.Services.AddTransient<PortfolioViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AddTransactionViewModel>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<MarketsPage>();
            builder.Services.AddTransient<PortfolioPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AddTransactionPage>();
#if DEBUG
    		builder.Logging.AddDebug();
#endif
            var app = builder.Build();
            Task.Run(async () =>
            {
                var dbService = app.Services.GetRequiredService<IDatabaseService>();
                await dbService.InitializeAsync();
            });
            return app;
        }
    }
}