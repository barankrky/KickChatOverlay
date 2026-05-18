using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KickChatOverlay.Converters;

public class PlatformToIconConverter : IValueConverter
{
    private static readonly Geometry Geometry = Geometry.Parse(
        "M1.333 0h8v5.333H12V2.667h2.667V0h8v8H20v2.667h-2.667v2.666H20V16h2.667v8h-8v-2.667H12v-2.666H9.333V24h-8Z");

    static PlatformToIconConverter()
    {
        Geometry.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Geometry;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
