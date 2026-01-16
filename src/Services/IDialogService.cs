namespace TagNamer.Services;

public enum FolderAddOption
{
    Files,      // 폴더 내부 파일 추가
    Folder,     // 폴더 자체 추가
    Cancel      // 취소
}

public interface IDialogService
{
    /// <summary>
    /// 사용자에게 확인 대화 상자를 표시합니다.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string message, string title = "확인");

    /// <summary>
    /// 폴더 추가 시 방식을 묻는 대화 상자를 표시합니다.
    /// </summary>
    Task<FolderAddOption> ShowFolderAddOptionAsync(string firstFolderName, int count = 1);

    /// <summary>
    /// 수동으로 파일 이름을 입력받는 대화 상자를 표시합니다.
    /// </summary>
    Task<string?> ShowManualEditAsync(string currentName);
}
