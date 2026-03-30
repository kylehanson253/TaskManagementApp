using System.Globalization;
using System.Windows.Data;

namespace TaskManagement.Desktop.Converters;

public class LoadingTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Signing in…" : "Sign In";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
