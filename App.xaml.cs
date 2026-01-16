using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf;
using TagNamer.Services;
using TagNamer.ViewModels;

namespace TagNamer;

public partial class App : Application
{
    public const string Version = "v1.0.1";
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
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IRenameService, RenameService>();
        services.AddSingleton<ISortingService, SortingService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddSingleton<SnackbarViewModel>();
        services.AddSingleton<TagManagerViewModel>();
        services.AddSingleton<RenameViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<TagNamer.Views.RenameWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            // ModernWpf 테마 설정 (Light 모드)
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
        catch (Exception)
        {
            Shutdown();
        }
    }
}
