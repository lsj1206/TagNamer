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

    /// <summary>
    /// 진행률을 표시하는 스낵바를 띄웁니다. (자동으로 닫히지 않음)
    /// 완료 시 Show()를 호출하여 결과를 표시하거나 수동으로 닫아야 합니다.
    /// </summary>
    void ShowProgress(string message);

    /// <summary>
    /// 진행률 표시 중인 스낵바의 메시지를 업데이트합니다.
    /// </summary>
    void UpdateProgress(string message);
}
