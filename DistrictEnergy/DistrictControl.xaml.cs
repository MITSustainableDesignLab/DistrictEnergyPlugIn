using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DistrictEnergy.Helpers;
using LiveCharts;
using LiveCharts.Wpf;
using Rhino;
using DistrictEnergy.ViewModels;
using LiveCharts.Helpers;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy
{
    /// <summary>
    ///     Interaction logic for ModuleControl.xaml
    /// </summary>
    public partial class DistrictControl : UserControl, INotifyPropertyChanged
    {
        public DistrictControl()
        {
            InitializeComponent();


            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;

            SelectSimCase.SelectionChanged += OnSelectionChanged;
            SelectSimCase.DropDownOpened += OnDropDownOpened;
        }


        private void ListBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item =
                ItemsControl.ContainerFromElement((ListBox) sender, (DependencyObject) e.OriginalSource) as ListBoxItem;
            if (item == null) return;

            var series = (StackedAreaSeries) item.Content;
            series.Visibility = series.Visibility == Visibility.Visible
                ? Visibility.Hidden
                : Visibility.Visible;
        }

        /*private void ListBox_OnUpdatedArrays(object sender, EventArgs e)
        {
            HeatingListBox.InvalidateArrange();
            HeatingListBox.ItemsSource = ResultsViewModel.StackedHeatingSeriesCollection;
            HeatingListBox.UpdateLayout();

            CoolingListBox.InvalidateArrange();
            CoolingListBox.ItemsSource = ResultsViewModel.StackedCoolingSeriesCollection;
            CoolingListBox.UpdateLayout();
            ElecListBox.InvalidateArrange();
            ElecListBox.ItemsSource = ResultsViewModel.StackedElecSeriesCollection;
            ElecListBox.UpdateLayout();

        }*/

        private void RunSimulationClick(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("DHSimulateDistrictEnergy", true);
        }

        private void AdditionalProfileClick(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("DHLoadAdditionalProfile", true);
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            // DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += ListBox_OnUpdatedArrays;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void TimeGrouped_Checked(object sender, RoutedEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            DHSimulateDistrictEnergy.Instance.PluginSettings.AggregationPeriod = TimeGroupers.Monthly;
            //DHSimulateDistrictEnergy.Instance.ResultsArray.OnResultsChanged(EventArgs.Empty);
        }

        private void TimeGrouped_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            DHSimulateDistrictEnergy.Instance.PluginSettings.AggregationPeriod = TimeGroupers.Daily;
            //DHSimulateDistrictEnergy.Instance.ResultsArray.OnResultsChanged(EventArgs.Empty);
        }

        private void CartesianChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var chart = (LiveCharts.Wpf.CartesianChart) sender;
            var vm = (ResultsViewModel) chart.DataContext;

            //lets get where the mouse is at our chart
            var mouseCoordinate = e.GetPosition(chart);

            //now that we know where the mouse is, lets use
            //ConverToChartValues extension
            //it takes a point in pixes and scales it to our chart current scale/values
            var p = chart.ConvertToChartValues(mouseCoordinate);

            //in the Y section, lets use the raw value
            vm.YPointer = p.Y;

            //for X in this case we will only highlight the closest point.
            //lets use the already defined ClosestPointTo extension
            //it will return the closest ChartPoint to a value according to an axis.
            //here we get the closest point to p.X according to the X axis
            if (chart.Series.Count > 0)
            {
                var series = chart.Series[0];
                var closetsPoint = series.ClosestPointTo(p.X, AxisOrientation.X);

                vm.XPointer = closetsPoint.X;
            }
        }

        public ChartMode ChartMode { get; set; }

        public class ComparisonConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value?.Equals(parameter);
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value?.Equals(true) == true ? parameter : Binding.DoNothing;
            }
        }

        private void OnDropDownOpened(object sender, EventArgs e)
        {
            SelectSimCase.SelectedItem = null;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectSimCase.SelectedItem != null)
            {
                var item = (SimCase) SelectSimCase.SelectedItem;
                if (item.DName == "Business As Usual")
                {
                    PlantSettingsViewModel.Instance.OFF_ABS = 0;
                }
                if (item.DName == "Business As Usual")
                {
                    PlantSettingsViewModel.Instance.OFF_ABS = 0;
                }
                if (item.DName == "Business As Usual")
                {
                    PlantSettingsViewModel.Instance.OFF_ABS = 0;
                }
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var vm = PlantSettingsViewModel.Instance;
            vm.OFF_ABS = 0;
        }
    }

    public class DoubleRangeRule : ValidationRule
    {
        public double Min { get; set; }

        public double Max { get; set; }

        public override ValidationResult Validate(object value,
            CultureInfo cultureInfo)
        {
            double parameter = 0;

            try
            {
                if (((string) value).Length > 0) parameter = double.Parse((string) value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or "
                                                   + e.Message);
            }
#if (DEBUG == true)
            if (parameter < Min || parameter > Max)
            {
                RhinoApp.WriteLine(string.Format("Please enter value in the range: " + Min + " - " + Max + "."));
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + Min + " - " + Max + ".");
            }
#endif
            if (parameter < Min || parameter > Max)
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + Min + " - " + Max + ".");
            return new ValidationResult(true, null);
        }
    }

    public class InverseAndBooleansToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.LongLength > 0)
                foreach (var value in values)
                    if (value is bool && (bool) value)
                        return false;
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}