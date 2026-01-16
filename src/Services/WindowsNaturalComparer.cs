using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TagNamer.Services;

/// <summary>
/// 윈도우 탐색기와 동일한 방식(자연 정렬)으로 문자열을 비교하는 클래스입니다.
/// 예: "file1", "file2", "file10" 순으로 정렬 (기본 문자열 비교는 "file1", "file10", "file2")
/// </summary>
public class WindowsNaturalComparer : IComparer<string>
{
    // Windows의 shlwapi.dll 라이브러리에서 논리적 문자열 비교 함수를 가져옵니다.
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);

    /// <summary>
    /// 두 문자열을 윈도우 탐색기 방식으로 비교합니다.
    /// </summary>
    public int Compare(string? x, string? y)
    {
        // null 체크 처리
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        // 시스템 API 호출하여 결과 반환
        return StrCmpLogicalW(x, y);
    }
}
