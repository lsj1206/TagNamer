using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using TagNamer.ViewModels;

namespace TagNamer.Services;

/// <summary>
/// 스낵바 알림을 관리하고 시간을 제어하는 서비스 구현체입니다.
/// </summary>
public class SnackbarService : ISnackbarService
{
    private readonly SnackbarViewModel _viewModel;
    private DispatcherTimer? _timer;
    private bool _isTransitioning;

    public SnackbarService(SnackbarViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public async void Show(string message, SnackbarType type = SnackbarType.Info, int durationMs = 3000)
    {
        // 이전 요청이 전환 중이면 무시하거나 큐 처리가 필요하지만,
        // 여기서는 가장 최신 요청을 우선시하되 애니메이션 흐름을 보장합니다.
        if (_isTransitioning) return;
        _isTransitioning = true;

        try
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // 이미 표시 중이라면 먼저 내림
                if (_viewModel.IsVisible && _viewModel.IsAnimating)
                {
                    _viewModel.IsAnimating = false;
                    _timer?.Stop();
                    await Task.Delay(350); // 퇴장 애니메이션 대기
                    _viewModel.IsVisible = false;
                    await Task.Delay(50); // 상태 초기화 대기
                }

                // 새로운 데이터 설정
                _viewModel.Message = message;
                _viewModel.Type = type;
                _viewModel.IsVisible = true;
                _viewModel.IsAnimating = true;

                // 새로운 타이머 설정
                _timer?.Stop();
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(durationMs)
                };

                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    CloseInternal();
                };

                _timer.Start();
            });
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void ShowProgress(string message)
    {
        // 큐 처리는 생략하고 즉시 반영합니다.
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _timer?.Stop(); // 기존 타이머 중지 (자동 닫힘 방지)

            _viewModel.Message = message;
            _viewModel.Type = SnackbarType.Info;
            _viewModel.IsVisible = true;
            _viewModel.IsAnimating = true;
        });
    }

    public void UpdateProgress(string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_viewModel.IsVisible)
            {
                _viewModel.Message = message;
            }
        });
    }

    private void CloseInternal()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            _viewModel.IsAnimating = false;
            await Task.Delay(350); // 퇴장 애니메이션 대기
            // 혹시 그 사이에 새로 애니메이션이 시작되지 않았을 때만 숨김
            if (!_viewModel.IsAnimating)
            {
                _viewModel.IsVisible = false;
            }
        });
    }
}
