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
            // ko-KR이면 false (한국어), en-US이면 true (English)
            return lang == "en-US";
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnglish)
        {
            // true면 en-US, false면 ko-KR
            return isEnglish ? "en-US" : "ko-KR";
        }
        return "ko-KR";
    }
}

