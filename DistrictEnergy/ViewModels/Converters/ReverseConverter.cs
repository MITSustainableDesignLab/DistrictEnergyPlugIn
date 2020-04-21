using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using LiveCharts;

namespace DistrictEnergy.ViewModels.Converters
{
    public class ReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((SeriesCollection) value)?.Reverse(); // todo this doesn't work yet
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}