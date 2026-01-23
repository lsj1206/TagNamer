using System;
using System.Globalization;
using System.Windows.Data;

namespace TagNamer.Converters;

/// <summary>
/// Language to Boolean Converter
/// </summary>
public class LanguageToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string lang)
        {
            // ko-KR이면 true (한국어, 버튼 파란색), en-US이면 false (English)
            return lang == "ko-KR";
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isKorean)
        {
            // true면 ko-KR, false면 en-US
            return isKorean ? "ko-KR" : "en-US";
        }
        return "en-US";
    }
}

