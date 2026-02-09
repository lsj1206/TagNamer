using System.Threading;
using System.Threading.Tasks;
using TagNamer.ViewModels;

namespace TagNamer.Services;

/// <summary>
/// 스낵바 알림을 관리하고 세션 기반으로 비동기 흐름을 제어하는 서비스 구현체입니다.
/// </summary>
public class SnackbarService : ISnackbarService
{
    private readonly SnackbarViewModel _viewModel;
    private long _currentSessionId = 0;
    private CancellationTokenSource? _sessionCts;

    public SnackbarService(SnackbarViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public async void Show(string message, SnackbarType type = SnackbarType.Info, int durationMs = 3000)
    {
        // 새로운 세션 시작
        long sessionId = Interlocked.Increment(ref _currentSessionId);

        // 이전 세션 취소 (대기 중인 Task.Delay 등 종료)
        _sessionCts?.Cancel();
        _sessionCts = new CancellationTokenSource();
        var ct = _sessionCts.Token;

        try
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            await app.Dispatcher.InvokeAsync(async () =>
            {
                // 이미 표시 중인 메세지를 교체하기전에 약간의 대기
                if (_viewModel.IsVisible)
                {
                    await Task.Delay(150, ct);
                }

                _viewModel.Message = message;
                _viewModel.Type = type;
                _viewModel.IsVisible = true;
                _viewModel.IsAnimating = true;

                try
                {
                    // 설정된 시간만큼 대기 (취소 가능)
                    await Task.Delay(durationMs, ct);

                    // 대기가 정상적으로 끝났다면 (취소되지 않았다면) 닫기
                    if (sessionId == Interlocked.Read(ref _currentSessionId))
                    {
                        await CloseInternalAsync(sessionId);
                    }
                }
                catch (TaskCanceledException)
                {
                    // 새로운 알림이 들어와서 취소된 경우이므로 아무것도 하지 않음
                }
            });
        }
        catch (Exception)
        {
            // 예외 발생 시 세션 상태 확인 후 정리
        }
    }

    public void ShowProgress(string message)
    {
        // 진행률 표시는 자동 종료 타이머가 없는 무한 세션으로 취급
        Interlocked.Increment(ref _currentSessionId);
        _sessionCts?.Cancel();
        _sessionCts = null;

        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Dispatcher.Invoke(() =>
        {
            _viewModel.Message = message;
            _viewModel.Type = SnackbarType.Info;
            _viewModel.IsVisible = true;
            _viewModel.IsAnimating = true;
        });
    }

    public void UpdateProgress(string message)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Dispatcher.Invoke(() =>
        {
            if (_viewModel.IsVisible)
            {
                _viewModel.Message = message;
            }
        });
    }

    private async Task CloseInternalAsync(long sessionId)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        await app.Dispatcher.InvokeAsync(async () =>
        {
            // 현재 세션이 여전히 유효할 때만 닫기 애니메이션 시작
            if (sessionId == Interlocked.Read(ref _currentSessionId))
            {
                _viewModel.IsAnimating = false;
                await Task.Delay(350); // 퇴장 애니메이션 대기

                // 애니메이션 대기 후에도 세션이 동일하면 완전히 숨김
                if (sessionId == Interlocked.Read(ref _currentSessionId))
                {
                    _viewModel.IsVisible = false;
                }
            }
        });
    }
}
