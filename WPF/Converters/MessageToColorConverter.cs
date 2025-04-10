using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF.Converters
{
    public class MessageToColorConverter : IValueConverter
    {
        private readonly Dictionary<string, Brush> _mapping = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase)
        {
            { "ERROR", Brushes.LightCoral },
            { "WARN", Brushes.LightYellow },
            { "INFO", Brushes.LightGreen },
            { "DEBUG", Brushes.LightBlue }
        };

        private readonly Brush _defaultColor = Brushes.LightGray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
       
            string status = value as string;
            if (string.IsNullOrEmpty(status))
                return _defaultColor;

            if (_mapping.TryGetValue(status, out Brush brush))
            {
                return brush;
            }

            return _defaultColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
