using System.Collections.Generic;

namespace TagNamer.Models;
// 태그 타입 정의
public enum TagType
{
    NameOrigin, // 원본 이름
    Number,     // 숫자 증가
    AtoZ,       // 알파벳 증가
    Today,      // 오늘 날짜
    TimeNow     // 현재 시간
}

// Number 태그 파라미터
public class NumberTagParams
{
    public long StartValue { get; set; }
    public int Digits { get; set; }
}

// AtoZ 태그 파라미터
public class AtoZTagParams
{
    public string StartValue { get; set; } = "A";
    public int Digits { get; set; }
    public int LowerCount { get; set; }
}

// Date/Time 태그 파라미터
public class DateTimeTagParams
{
    public string Format { get; set; } = string.Empty;
}

public class TagItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string ToolTip { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();

    // 태그 타입
    public TagType Type { get; set; }
    // 태그 파라미터
    public object? Params { get; set; }

    // 기본 태그 구분
    public bool IsStandard { get; set; } = false;
    // 삭제 가능 구분
    public bool IsDeletable => !IsStandard;
}
