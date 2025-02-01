using System.Globalization;
using System.Windows.Data;

namespace Sentinel.Support.Converters;

public class MetaDataConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);

        var member = parameter as string;

        if (value is IDictionary<string, object> metaData && !string.IsNullOrWhiteSpace(member))
        {
            metaData.TryGetValue(member, out var metaDataValue);
            return metaDataValue;
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}