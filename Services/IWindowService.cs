using System.Windows;

namespace TagNamer.Services;

/// <summary>
/// ViewModel에서 View(창)를 제어하기 위한 서비스 인터페이스입니다.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// 특정 형식의 창을 모달(ShowDialog)로 띄웁니다.
    /// </summary>
    /// <typeparam name="T">띄울 창의 클래스 타입</typeparam>
    /// <param name="viewModel">창에 바인딩할 ViewModel</param>
    /// <returns>창의 결과값</returns>
    bool? ShowDialog<T>(object viewModel) where T : Window;

    /// <summary>
    /// 특정 형식의 창을 일반(Show) 모드로 띄웁니다.
    /// </summary>
    /// <typeparam name="T">띄울 창의 클래스 타입</typeparam>
    /// <param name="viewModel">창에 바인딩할 ViewModel</param>
    void Show<T>(object viewModel) where T : Window;
}
