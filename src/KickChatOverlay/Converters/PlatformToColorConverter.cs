using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KickChatOverlay.Converters;

public class PlatformToColorConverter : IValueConverter
{
    private static readonly Brush Brush = new SolidColorBrush(Color.FromRgb(83, 252, 24));

    static PlatformToColorConverter()
    {
        Brush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
