namespace TagNamer.Services;

public enum SnackbarType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// 화면 상단에 짧은 알림 메시지를 표시하기 위한 서비스 인터페이스입니다.
/// </summary>
public interface ISnackbarService
{
    /// <summary>
    /// 스낵바 알림을 표시합니다.
    /// </summary>
    /// <param name="message">표시할 메시지 내용</param>
    /// <param name="type">알림 타입 (색상 결정)</param>
    /// <param name="durationMs">표시 시간 (밀리초)</param>
    void Show(string message, SnackbarType type = SnackbarType.Info, int durationMs = 2000);
}
