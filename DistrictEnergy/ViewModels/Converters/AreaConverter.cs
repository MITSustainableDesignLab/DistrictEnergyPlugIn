using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     If value is higher than 999999 m2, than show square km2
    /// </summary>
    public class AreaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999999)
                    return (d * 1E-6).ToString("N0", culture); // km^2
                return d.ToString("N0", culture);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}