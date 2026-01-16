using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace TagNamer.Services;

/// <summary>
/// IWindowService의 구현체로, MVVM 패턴을 유지하면서 뷰모델에서 새 창을 열 수 있게 해주는 서비스입니다.
/// </summary>
public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 지정된 타입의 창을 모달(ShowDialog)로 표시합니다.
    /// </summary>
    /// <typeparam name="T">열고자 하는 Window 클래스 타입</typeparam>
    /// <param name="viewModel">창에 연결할 DataContext (ViewModel)</param>
    /// <returns>창이 닫힐 때의 DialogResult</returns>
    public bool? ShowDialog<T>(object viewModel) where T : Window
    {
        // DI 컨테이너에서 창 인스턴스를 가져옴
        var window = _serviceProvider.GetRequiredService<T>();
        window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow; // 부모 창 설정
        return window.ShowDialog();
    }

    /// <summary>
    /// 지정된 타입의 창을 일반 모드(Show)로 표시합니다.
    /// </summary>
    /// <typeparam name="T">열고자 하는 Window 클래스 타입</typeparam>
    /// <param name="viewModel">창에 연결할 DataContext (ViewModel)</param>
    public void Show<T>(object viewModel) where T : Window
    {
        var window = _serviceProvider.GetRequiredService<T>();
        window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow;
        window.Show();
    }
}
