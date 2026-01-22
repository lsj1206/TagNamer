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
            return lang == "ko-KR";
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isKorean)
        {
            return isKorean ? "ko-KR" : "en-US";
        }
        return "ko-KR";
    }
}

