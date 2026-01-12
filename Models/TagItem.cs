using System.Collections.Generic;

namespace TagNamer.Models;
// 태그 타입 정의
public enum TagType
{
    NameOrigin, // 원본 이름
    Number,     // 숫자 증가
    AtoZ,       // 알파벳 증가
    Today,      // 오늘 날짜
    TimeNow,    // 현재 시간
    OriginSplit, // 원본 이름 분할 (앞/뒤, 남기기/삭제)
    OnlyNumber, // 숫자만 추출
    OnlyLetter, // 문자만 추출
    ToUpper,    // 전체 대문자 변환
    ToLower     // 전체 소문자 변환
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
    // 각 파트 (YY, YYYY, MM, DD, HH, SS 등)
    public string Part1 { get; set; } = string.Empty;
    public string Part2 { get; set; } = string.Empty;
    public string Part3 { get; set; } = string.Empty;

    // 구분자
    public string Sep1 { get; set; } = string.Empty;
    public string Sep2 { get; set; } = string.Empty;
}

// OriginSplit 태그 파라미터
public class OriginSplitTagParams
{
    // 앞에서부터(false) / 뒤에서부터(true)
    public bool IsFromBack { get; set; }
    // 삭제(false) / 남기기(true)
    public bool IsKeep { get; set; }

    public int StartCount { get; set; }
    public int EndCount { get; set; }
}

public class TagItem
{
    public string TagName { get; set; } = string.Empty;
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
    // 규칙 내 유일성 보장 (true면 동일 태그 중복 불가)
    public bool IsUnique { get; set; } = false;
    // 배타적 그룹 (쉼표로 구분, 같은 그룹의 태그들은 동시에 존재 불가)
    public string? ExclusiveGroup { get; set; } = null;
}
