using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace TagNamer.Services;

/// <summary>
/// IWindowService의 구현체로, IServiceProvider를 통해 창을 생성하고 관리합니다.
/// </summary>
public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool? ShowDialog<T>(object viewModel) where T : Window
    {
        var window = _serviceProvider.GetRequiredService<T>();
        window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow;
        return window.ShowDialog();
    }

    public void Show<T>(object viewModel) where T : Window
    {
        var window = _serviceProvider.GetRequiredService<T>();
        window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow;
        window.Show();
    }
}
