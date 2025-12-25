namespace TagNamer.Services;

public enum SnackbarType
{
    Info,   // 기본
    Success, // 작업 성공
    Warning, // 작업 성공 - 경고 (일부 성공)
    Error // 오류
}

/// <summary>
/// 화면 상단에 짧은 알림 메시지를 표시하기 위한 서비스 인터페이스입니다.
/// </summary>
public interface ISnackbarService
{
    void Show(string message, SnackbarType type = SnackbarType.Info, int durationMs = 2000);
}
