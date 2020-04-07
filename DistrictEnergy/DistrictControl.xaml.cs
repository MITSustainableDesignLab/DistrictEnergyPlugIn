using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
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
            Instance = this;

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            SelectSimCase.SelectionChanged += OnSimCaseChanged;
            SelectSimCase.DropDownOpened += OnDropDownOpened;
            PlantSettingsViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;
            ChilledWaterViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;
        }

        private void OnCustomPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // throw new NotImplementedException();
        }

        public static DistrictControl Instance { get; set; }


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

        public class ComparisonConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                return value?.Equals(parameter);
            }

            public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                return value?.Equals(true) == true ? parameter : Binding.DoNothing;
            }
        }

        private void OnDropDownOpened(object sender, EventArgs e)
        {
            // SelectSimCase.SelectedItem = null;
        }

        private void OnSimCaseChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectSimCase.SelectedItem != null)
            {
                var item = (SimCase) SelectSimCase.SelectedItem;
                if (item.Id == 1)
                {
                    ChilledWaterViewModel.Instance.OFF_ABS = 0;
                    CombinedHeatAndPowerViewModel.Instance.OFF_CHP = 0;
                    ElectricGenerationViewModel.Instance.OFF_PV = 100;
                    HotWaterViewModel.Instance.OFF_EHP = 100;
                    HotWaterViewModel.Instance.OFF_SHW = 100;
                    ElectricGenerationViewModel.Instance.OFF_WND = 100;
                    ElectricGenerationViewModel.Instance.AUT_BAT = 0;
                    HotWaterViewModel.Instance.AUT_HWT = 0;
                }

                if (item.Id == 2)
                {
                    ChilledWaterViewModel.Instance.OFF_ABS = 0;
                    CombinedHeatAndPowerViewModel.Instance.OFF_CHP = 0;
                    ElectricGenerationViewModel.Instance.OFF_PV = 0;
                    HotWaterViewModel.Instance.OFF_EHP = 0;
                    HotWaterViewModel.Instance.OFF_SHW = 0;
                    ElectricGenerationViewModel.Instance.OFF_WND = 0;
                    ElectricGenerationViewModel.Instance.AUT_BAT = 0;
                    HotWaterViewModel.Instance.AUT_HWT = 0;
                }

                if (item.Id == 3)
                {
                    ChilledWaterViewModel.Instance.OFF_ABS = 100;
                    CombinedHeatAndPowerViewModel.Instance.OFF_CHP = 100;
                    ElectricGenerationViewModel.Instance.OFF_PV = 0;
                    HotWaterViewModel.Instance.OFF_EHP = 0;
                    HotWaterViewModel.Instance.OFF_SHW = 0;
                    ElectricGenerationViewModel.Instance.OFF_WND = 0;
                    ElectricGenerationViewModel.Instance.AUT_BAT = 0;
                    HotWaterViewModel.Instance.AUT_HWT = 0;
                }

                RhinoApp.WriteLine($"Plant settings changed to predefined case {item.DName}");
            }

            OnPropertyChanged();
        }

        private void CostsChecked(object sender, RoutedEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            // Display Costs
        }

        private void CarbonChecked(object sender, RoutedEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            // Display Carbon
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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