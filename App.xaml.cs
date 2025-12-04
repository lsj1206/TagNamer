using System.Data;
using System.Windows;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf;
using TagNamer.Services;
using TagNamer.ViewModels;

namespace TagNamer;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISortingService, SortingService>();
        services.AddSingleton<IFileService, FileService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ModernWpf 테마 설정 (Light 모드)
        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }
}

