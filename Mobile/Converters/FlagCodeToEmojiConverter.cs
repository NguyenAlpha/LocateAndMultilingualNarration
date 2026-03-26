using System.Globalization;

namespace Mobile.Converters;

public class FlagCodeToEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string flagCode || flagCode.Length < 2)
            return "🌐";

        var code = flagCode.ToUpperInvariant();
        return char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A'))
             + char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
