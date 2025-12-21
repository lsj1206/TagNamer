namespace TagNamer.Models;

public class TagItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ToolTip { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    // 고정 태그 여부 (삭제 불가 여부 결정)
    public bool IsStandard { get; set; } = false;
    // 컨텍스트 메뉴 등에서 사용할 수 있는 편의 속성
    public bool IsDeletable => !IsStandard;
}
