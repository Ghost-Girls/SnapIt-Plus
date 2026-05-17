using System.Globalization;
using System.Windows.Data;

namespace SnapIt.Common.Converters;

public class LanguageNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            "en-US" => "English",
            "zh-CN" => "中文",
            _ => value?.ToString() ?? "English"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}
