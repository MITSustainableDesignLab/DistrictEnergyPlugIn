using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using LiveCharts.Wpf;
using Rhino;

namespace DistrictEnergy
{
    /// <summary>
    ///     Interaction logic for ModuleControl.xaml
    /// </summary>
    public partial class DistrictControl : UserControl, INotifyPropertyChanged
    {
        private readonly GridLength[] starHeight;
        private ObservableCollection<IBaseLoad> _listOfDistrictLoads;
        private ObservableCollection<IThermalPlantSettings> _listOfPlantSettings;

        public DistrictControl()
        {
            ListOfPlantSettings = new ObservableCollection<IThermalPlantSettings>
            {
                new AbsorptionChiller(),
                new BatteryBank(),
                new CombinedHeatNPower(),
                new ElectricChiller(),
                new ElectricHeatPump(),
                new HotWaterStorage(),
                new NatGasBoiler(),
                new PhotovoltaicArray(),
                new SolarThermalCollector(),
                new WindTurbine(),
                new GridElectricity(),
                new GridGas()
            };
            ListOfDistrictLoads = new ObservableCollection<IBaseLoad>
            {
                new HeatingLoads(),
                new CoolingLoads(),
                new ElectricityLoads(),
                new PipeNetwork(LoadTypes.Heating, "Heating Losses"),
                new PipeNetwork(LoadTypes.Cooling, "Cooling Losses")
            };
            PlanningSettings = new PlanningSettings();
            Scenarios = new ObservableCollection<SimCase>();
            Instance = this;
            DataContext = this;

            InitializeComponent();

            SelectSimCase.SelectionChanged += OnSimCaseChanged;
            SelectSimCase.DropDownOpened += OnDropDownOpened;
            PlantSettingsViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;


            // For the different PlantSettingViewModel Children, listen for PropertyChanged
            ChilledWaterViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;
            CombinedHeatAndPowerViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;
            ElectricGenerationViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;
            HotWaterViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;
            PlanningSettingsViewModel.Instance.PropertyChanged += OnCustomPropertyChanged;

            starHeight = new GridLength[MainGrid.RowDefinitions.Count];
            starHeight[0] = MainGrid.RowDefinitions[0].Height;
            starHeight[1] = MainGrid.RowDefinitions[1].Height;
            starHeight[3] = MainGrid.RowDefinitions[3].Height;

            ExpandedOrCollapsed(MyExpander);
            // InitializeComponent calls topExpander.Expanded
            // while bottomExpander is null, if we hook this up in the xaml
            MyExpander.Expanded += ExpandedOrCollapsed;
            MyExpander.Collapsed += ExpandedOrCollapsed;
        }

        public ObservableCollection<IBaseLoad> ListOfDistrictLoads
        {
            get => _listOfDistrictLoads;
            set
            {
                if (Equals(value, _listOfDistrictLoads)) return;
                _listOfDistrictLoads = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IThermalPlantSettings> ListOfPlantSettings
        {
            get => _listOfPlantSettings;
            set
            {
                if (Equals(value, _listOfPlantSettings)) return;
                _listOfPlantSettings = value;
                OnPropertyChanged();
            }
        }

        public static DistrictControl Instance { get; set; }
        public static PlanningSettings PlanningSettings { get; set; }
        public ObservableCollection<SimCase> Scenarios { get; set; }
        public SimCase ASimCase { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnCustomPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UseDistrictLosses")
                DhRunLpModel.Instance.StaleResults = true;
            if (e.PropertyName == "RelDistHeatLoss") DhRunLpModel.Instance.StaleResults = true;
            if (e.PropertyName == "RelDistCoolLoss") DhRunLpModel.Instance.StaleResults = true;

            // DHSimulateDistrictEnergy.Instance.RerunSimulation(); // Todo: Uncomment this to activate dynamic refresh of results
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

        private void RunSimulationClick(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("DHRunLPModel", true);
        }

        private void AdditionalProfileClick(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("DHLoadAdditionalProfile", true);
        }

        private void OnDropDownOpened(object sender, EventArgs e)
        {
            // SelectSimCase.SelectedItem = null;
        }

        public void OnSimCaseChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectSimCase.SelectedItem != null)
            {
                var item = (SimCase) SelectSimCase.SelectedItem;
                ListOfPlantSettings.Clear();
                if (item.ListOfPlantSettings == null) return;
                foreach (var plant in item.ListOfPlantSettings) ListOfPlantSettings.Add(plant);

                ChilledWaterViewModel.Instance.OnPropertyChanged(null);
                ElectricGenerationViewModel.Instance.OnPropertyChanged(null);
                HotWaterViewModel.Instance.OnPropertyChanged(null);
                CombinedHeatAndPowerViewModel.Instance.OnPropertyChanged(null);
                RhinoApp.WriteLine($"Plant settings changed to predefined case {item.DName}");
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        ///     See http://csuporj2.blogspot.com/2009/12/wpf-expanders-with-stretching-height.html for more information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpandedOrCollapsed(object sender, RoutedEventArgs e)
        {
            ExpandedOrCollapsed(sender as Expander);
        }

        private void ExpandedOrCollapsed(Expander expander)
        {
            if (expander.Parent is Grid grid && grid.Name == "ExpanderGrid")
            {
                var rowIndex = Grid.GetRow(grid);
                var row = MainGrid.RowDefinitions[rowIndex];
                if (expander.IsExpanded)
                {
                    row.Height = starHeight[rowIndex];
                    row.MinHeight = 88;
                }
                else
                {
                    starHeight[rowIndex] = row.Height;
                    row.Height = GridLength.Auto;
                    row.MinHeight = 0;
                }
            }

            var isExpanded = MyExpander.IsExpanded;
            splitter.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var inputScenarioName = DistrictSettingsViewModel.InputScenarioName;
            if (string.IsNullOrEmpty(inputScenarioName)) return;
            var json = PlantSettingsViewModel.SerializeToString(Instance.ListOfPlantSettings);
            Instance.Scenarios.Add(new SimCase
            {
                DName = inputScenarioName,
                ListOfPlantSettings = new ObservableCollection<IThermalPlantSettings>(PlantSettingsViewModel.DeserializeFromString(json))
            });
            DistrictSettingsViewModel.Instance.OnPropertyChanged(nameof(DistrictSettingsViewModel.Instance.SimCases));
            DistrictSettingsViewModel.Instance.IsDialogBoxVisible = Visibility.Collapsed;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DistrictSettingsViewModel.Instance.IsDialogBoxVisible = Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DistrictSettingsViewModel.Instance.IsDialogBoxVisible = Visibility.Visible;
        }


        public class ComparisonConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter,
                CultureInfo culture)
            {
                return value?.Equals(parameter);
            }

            public object ConvertBack(object value, Type targetType, object parameter,
                CultureInfo culture)
            {
                return value?.Equals(true) == true ? parameter : Binding.DoNothing;
            }
        }

        private void RemoveRemotePathItem_Click(object sender, RoutedEventArgs e)
        {
            var depObj = sender as DependencyObject;

            while (!(depObj is ComboBoxItem))
            {
                if (depObj == null) return;
                depObj = VisualTreeHelper.GetParent(depObj);
            }

            var comboBoxItem = depObj as ComboBoxItem;
            Instance.Scenarios.Remove((SimCase)comboBoxItem.Content);
            DistrictSettingsViewModel.Instance.OnPropertyChanged(nameof(DistrictSettingsViewModel.Instance.SimCases));
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