using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using LiveCharts;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class ResultsViewModel : INotifyPropertyChanged
    {
        private double _elecToChw;
        private double _elecToElec;
        private double _elecToHw;
        private double _gasToChw;
        private double _gasToElec;
        private double _gasToHw;
        private double _purchasedElec;
        private double _purchasedElecIntensity;
        private double _purchasedNgas;
        private double _purchasedNgasIntensity;
        private double _totalCarbonIntensity;
        private double _totalCostIntensity;
        private double _totalEnergyIntensity;

        public ResultsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;
            KWhFormatter = delegate(double val)
            {
                if (val > 999)
                    return (val / 1000).ToString("G0", CultureInfo.CreateSpecificCulture("en-US")) +
                           " MWh";

                if (val > 999999)
                    return (val / 1000000).ToString("G0", CultureInfo.CreateSpecificCulture("en-US")) +
                           " GWh";
                return val.ToString("G0", CultureInfo.CreateSpecificCulture("en-US")) +
                       " kWh";
            }; // Formats the yAxis of the stacked graph

            KWhLabelPointFormatter = delegate(ChartPoint chartPoint)
            {
                if (chartPoint.Y > 999) return string.Format("{0:N1} MWh", chartPoint.Y / 1000);

                if (chartPoint.Y > 999999) return string.Format("{0:N1} GWh", chartPoint.Y / 1000000);
                return string.Format("{0:N1} kWh", chartPoint.Y);
            };

            MonthFormatter = val => (val + 1).ToString(CultureInfo.CreateSpecificCulture("en-US"));

            GaugeFormatter = value => value.ToString("N1"); // Formats the gauge number
        }

        public ResultsViewModel Instance { get; set; }
        public SeriesCollection StackedDemandSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedHeatingSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedCoolingSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedElecSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection PieHeatingChartGraphSeries { get; set; } = new SeriesCollection();
        public SeriesCollection PieCoolingChartGraphSeries { get; set; } = new SeriesCollection();

        public double ElecToHw
        {
            get => _elecToHw;
            set
            {
                _elecToHw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElecToHw)));
            }
        }

        public double ElecToElec
        {
            get => _elecToElec;
            set
            {
                _elecToElec = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElecToElec)));
            }
        }

        public double GasToHw
        {
            get => _gasToHw;
            set
            {
                _gasToHw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GasToHw)));
            }
        }

        public double GasToElec
        {
            get => _gasToElec;
            set
            {
                _gasToElec = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GasToElec)));
            }
        }

        public double ElecToChw
        {
            get => _elecToChw;
            set
            {
                _elecToChw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElecToChw)));
            }
        }

        public double TotalCost { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateDemandStackedChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateHeatingStackedChart;
            // DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCoolingStackedChart;
            // DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateElecStackedChart;
            // DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateHeatingPieChart;
            // DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCoolingPieChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateMetrics;
        }

        private void OnProjectClosed(object sender, EventArgs e)
        {
            // Reset arrays
            PurchasedElec = new double();
            PurchasedElecIntensity = new double();
            PurchasedNgas = new double();
            PurchasedNgasIntensity = new double();
            TotalEnergyIntensity = new double();
            TotalCarbonIntensity = new double();
            TotalCostIntensity = new double();

            PieHeatingChartGraphSeries.Clear();
            PieCoolingChartGraphSeries.Clear();
            StackedCoolingSeriesCollection.Clear();
            StackedHeatingSeriesCollection.Clear();
            StackedElecSeriesCollection.Clear();
            StackedDemandSeriesCollection.Clear();
        }

        /// <summary>
        ///     Updates the pie graph that shows the Heating energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateHeatingPieChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var HwDemand = new Dictionary<string, double[]>
            {
                {"Solar Hot Water", instance.ResultsArray.HwShw},
                {"Hot Water Tank", instance.ResultsArray.HwHwt},
                {"Electric Heat Pump", instance.ResultsArray.HwEhp},
                {"Natural Gas Boiler", instance.ResultsArray.HwNgb},
                {"Combined Heating and Power", instance.ResultsArray.HwChp}
            };

            PieHeatingChartGraphSeries.Clear();

            foreach (var hw in HwDemand)
            {
                var temp = new PieSeries
                {
                    Values = new ChartValues<double> {hw.Value.Sum()},
                    Title = hw.Key,
                    DataLabels = true,
                    LabelPoint = KWhLabelPointFormatter
                };
                PieHeatingChartGraphSeries.Add(temp);
            }
        }

        /// <summary>
        ///     Updates the pie graph that shows the Cooling energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateCoolingPieChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var chwDemand = new Dictionary<string, double[]>
            {
                {"Absorption Chiller", instance.ResultsArray.ChwAbs},
                {"Electric Chiller", instance.ResultsArray.ChwEch}
            };

            PieCoolingChartGraphSeries.Clear();

            foreach (var chw in chwDemand)
            {
                var temp = new PieSeries
                {
                    Values = new ChartValues<double> {chw.Value.Sum()},
                    Title = chw.Key,
                    DataLabels = false,
                    LabelPoint = KWhLabelPointFormatter
                };
                PieCoolingChartGraphSeries.Add(temp);
            }
        }

        private void UpdateDemandStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var Demand = new Dictionary<string, double[]>
            {
                { "Heating", instance.DistrictDemand.ChwN },
                { "Cooling", instance.DistrictDemand.HwN },
                { "Electricity", instance.DistrictDemand.ElecN },
            };

            StackedDemandSeriesCollection.Clear();

            foreach (var d in Demand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(d.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title=d.Key,
                    LineSmoothness = 0,
                    LabelPoint = KWhLabelPointFormatter
                };
                StackedDemandSeriesCollection.Add(temp);
            }
        }


        /// <summary>
        ///     Updates the Stacked graph that shows the heating energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateHeatingStackedChart(object sender, EventArgs e)
        {
            var instance = (ResultsArray) sender;
            var HwDemand = new Dictionary<string, double[]>
            {
                { "Solar Hot Water", instance.HwShw },
                { "Hot Water Tank", instance.HwHwt },
                { "Electric Heat Pump", instance.HwEhp },
                { "Natural Gas Boiler", instance.HwNgb },
                { "Combined Heating and Power", instance.HwChp }
            };

            StackedHeatingSeriesCollection.Clear();

            foreach (var hw in HwDemand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(hw.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title = hw.Key,
                    LineSmoothness = 0.5,
                    LabelPoint = KWhLabelPointFormatter
                };
                StackedHeatingSeriesCollection.Add(temp);
            }
        }

        /// <summary>
        ///     Updates the Stacked graph that shows the cooling energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateCoolingStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var chwDemand = new Dictionary<string, double[]>
            {
                {"Absorption Chiller", instance.ResultsArray.ChwAbs},
                {"Electric Chiller", instance.ResultsArray.ChwEch},
                {"Evaporator Side of EHPs", instance.ResultsArray.ChwEhpEvap}
            };

            StackedCoolingSeriesCollection.Clear();

            foreach (var chw in chwDemand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(chw.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title = chw.Key,
                    LineSmoothness = 0.5,
                    LabelPoint = KWhLabelPointFormatter
                };
                StackedCoolingSeriesCollection.Add(temp);
            }
        }

        /// <summary>
        ///     Updates the Stacked graph that shows the electricity energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateElecStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var elecDemand = new Dictionary<string, double[]>();
            elecDemand.Add("Battery", instance.ResultsArray.ElecBat);
            elecDemand.Add("Renewables", instance.ResultsArray.ElecRen);
            elecDemand.Add("Combined Heat & Power", instance.ResultsArray.ElecChp);
            elecDemand.Add("Purchased Electricity", instance.ResultsArray.ElecProj);

            StackedElecSeriesCollection.Clear();

            foreach (var elec in elecDemand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(elec.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title = elec.Key,
                    LineSmoothness = 0.5,
                    LabelPoint = KWhLabelPointFormatter
                };
                StackedElecSeriesCollection.Add(temp);
            }
        }


        private void UpdateMetrics(object sender, EventArgs e)
        {
            double totalGrossFloorArea = 1;
            var context = UmiContext.Current;
            if (context != null)
                totalGrossFloorArea = context.Buildings.All.Select(x => x.GrossFloorArea).Sum().Value;

            var instance = DHSimulateDistrictEnergy.Instance;

            PurchasedElec = instance.ResultsArray.ElecProj.Sum(); // kWh
            PurchasedElecIntensity = PurchasedElec / totalGrossFloorArea; // kWh/m2

            PurchasedNgas = instance.ResultsArray.NgasProj.Sum(); // To kWh
            PurchasedNgasIntensity = PurchasedNgas / totalGrossFloorArea; // kWh/m2

            TotalEnergyIntensity = (instance.ResultsArray.ElecProj.Sum() + instance.ResultsArray.NgasProj.Sum()) /
                                   totalGrossFloorArea; // kWh/m2
            TotalCarbon = instance.ResultsArray.ElecProj.Sum() *
                          UmiContext.Current.ProjectSettings.ElectricityCarbon +
                          instance.ResultsArray.NgasProj.Sum() * UmiContext.Current.ProjectSettings.GasCarbon;
            TotalCarbonIntensity = TotalCarbon / totalGrossFloorArea;
            TotalCost = instance.ResultsArray.ElecProj.Sum() *
                        UmiContext.Current.ProjectSettings.ElectricityDollars +
                        instance.ResultsArray.NgasProj.Sum() *
                        UmiContext.Current.ProjectSettings.GasDollars;
            TotalCostIntensity = TotalCost / totalGrossFloorArea;

            // Gas To Chilled Water Paths

            var gasChwByBoilerAbs =
                instance.ResultsArray.NgasNgb.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                instance.ResultsArray.HwAbs.Sum()
                    .SafeDivision(instance.ResultsArray.HwAbs.Sum() + instance.DistrictDemand.HwN.Sum() -
                                  instance.ResultsArray.HwEhp.Sum());
            var gasChwByChpAbs =
                instance.ResultsArray.NgasChp.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                instance.ResultsArray.HwChp.Sum()
                    .SafeDivision(instance.ResultsArray.HwChp.Sum() + instance.ResultsArray.ElecChp.Sum()) *
                instance.ResultsArray.HwAbs.Sum()
                    .SafeDivision(instance.ResultsArray.HwAbs.Sum() + instance.DistrictDemand.HwN.Sum() -
                                  instance.ResultsArray.HwEhp.Sum());
            var gasChwByChpEhp =
                instance.ResultsArray.NgasChp.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                instance.ResultsArray.ElecChp.Sum()
                    .SafeDivision(instance.ResultsArray.HwChp.Sum() + instance.ResultsArray.ElecChp.Sum()) *
                instance.ResultsArray.ElecEhp.Sum()
                    .SafeDivision(instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                                  instance.DistrictDemand.ElecN.Sum()) *
                instance.ResultsArray.ChwEhpEvap.Sum()
                    .SafeDivision(instance.ResultsArray.HwEhp.Sum() + instance.ResultsArray.ChwEhpEvap.Sum());
            var gasChwByChpEch =
                instance.ResultsArray.NgasChp.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                instance.ResultsArray.ElecChp.Sum()
                    .SafeDivision(instance.ResultsArray.HwChp.Sum() + instance.ResultsArray.ElecChp.Sum()) *
                instance.ResultsArray.ElecEch.Sum()
                    .SafeDivision(instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                                  instance.DistrictDemand.ElecN.Sum());
            GasToChw = (gasChwByBoilerAbs + gasChwByChpAbs + gasChwByChpEhp + gasChwByChpEch) * 100;

            // Gas to How Water Paths
            var gasHwByBoiler = instance.ResultsArray.NgasNgb.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                                (instance.DistrictDemand.HwN.Sum() - instance.ResultsArray.HwEhp.Sum()).SafeDivision(
                                    instance.DistrictDemand.HwN.Sum() - instance.ResultsArray.HwEhp.Sum() +
                                    instance.ResultsArray.HwAbs.Sum());
            var gasHwByChp = instance.ResultsArray.NgasChp.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                             instance.ResultsArray.HwChp.Sum()
                                 .SafeDivision(instance.ResultsArray.HwChp.Sum() +
                                               instance.ResultsArray.ElecChp.Sum()) *
                             (instance.DistrictDemand.HwN.Sum() - instance.ResultsArray.HwEhp.Sum()).SafeDivision(
                                 instance.DistrictDemand.HwN.Sum() - instance.ResultsArray.HwEhp.Sum() +
                                 instance.ResultsArray.HwAbs.Sum());
            var gasHwByChpEhp = instance.ResultsArray.NgasChp.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                                instance.ResultsArray.ElecChp.Sum()
                                    .SafeDivision(instance.ResultsArray.ElecChp.Sum() +
                                                  instance.ResultsArray.HwChp.Sum()) *
                                instance.ResultsArray.ElecEhp.Sum().SafeDivision(
                                    instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                                    instance.DistrictDemand.ElecN.Sum()) *
                                instance.ResultsArray.HwEhp.Sum().SafeDivision(
                                    instance.ResultsArray.HwEhp.Sum() + instance.ResultsArray.ChwEhpEvap.Sum());

            GasToHw = (gasHwByBoiler + gasHwByChp + gasHwByChpEhp) * 100;

            // Gas to Electricity Paths
            var gasElecByChp = instance.ResultsArray.NgasChp.Sum().SafeDivision(instance.ResultsArray.NgasProj.Sum()) *
                               instance.ResultsArray.ElecChp.Sum()
                                   .SafeDivision(
                                       instance.ResultsArray.HwChp.Sum() + instance.ResultsArray.ElecChp.Sum()) *
                               instance.DistrictDemand.ElecN.Sum().SafeDivision(
                                   instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                                   instance.DistrictDemand.ElecN.Sum());

            GasToElec = gasElecByChp * 100;

            // Electricity to Chilled Water Paths
            var elecChwByEch = instance.ResultsArray.ElecEch.Sum().SafeDivision(
                instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                instance.DistrictDemand.ElecN.Sum());
            var elecChwByEhp = instance.ResultsArray.ElecEhp.Sum().SafeDivision(
                                   instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                                   instance.DistrictDemand.ElecN.Sum()) *
                               instance.ResultsArray.ChwEhpEvap.Sum().SafeDivision(
                                   instance.ResultsArray.ChwEhpEvap.Sum() + instance.ResultsArray.HwEhp.Sum());

            ElecToChw = (elecChwByEch + elecChwByEhp) * 100;

            // Elec to Hot Water
            var elecHwByEhp = instance.ResultsArray.ElecEhp.Sum().SafeDivision(
                                  instance.ResultsArray.ElecEhp.Sum() + instance.ResultsArray.ElecEch.Sum() +
                                  instance.DistrictDemand.ElecN.Sum()) *
                              instance.ResultsArray.HwEhp.Sum()
                                  .SafeDivision(instance.ResultsArray.HwEhp.Sum() +
                                                instance.ResultsArray.ChwEhpEvap.Sum());

            ElecToHw = elecHwByEhp * 100;

            // Elec to Elec Paths
            var elecElecDirect = instance.DistrictDemand.ElecN.Sum().SafeDivision(
                instance.DistrictDemand.ElecN.Sum() + instance.ResultsArray.ElecEhp.Sum() +
                instance.ResultsArray.ElecEch.Sum());

            ElecToElec = elecElecDirect * 100;
        }

        #region ViewResults

        public double PurchasedElec
        {
            get => _purchasedElec;
            set
            {
                _purchasedElec = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PurchasedElec)));
            }
        }

        public double PurchasedElecIntensity
        {
            get => _purchasedElecIntensity;
            set
            {
                _purchasedElecIntensity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PurchasedElecIntensity)));
            }
        }

        public double PurchasedNgas
        {
            get => _purchasedNgas;
            set
            {
                _purchasedNgas = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PurchasedNgas)));
            }
        }

        public double PurchasedNgasIntensity
        {
            get => _purchasedNgasIntensity;
            set
            {
                _purchasedNgasIntensity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PurchasedNgasIntensity)));
            }
        }

        public double TotalEnergyIntensity
        {
            get => _totalEnergyIntensity;
            set
            {
                _totalEnergyIntensity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalEnergyIntensity)));
            }
        }

        public double TotalCarbon { get; private set; }

        public double TotalCarbonIntensity
        {
            get => _totalCarbonIntensity;
            set
            {
                _totalCarbonIntensity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalCarbonIntensity)));
            }
        }

        public double TotalCostIntensity
        {
            get => _totalCostIntensity;
            set
            {
                _totalCostIntensity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalCostIntensity)));
            }
        }

        public double GasToChw
        {
            get => _gasToChw;
            set
            {
                _gasToChw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GasToChw)));
            }
        }

        #endregion

        #region Formatters

        public Func<double, string> GaugeFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<ChartPoint, string> KWhLabelPointFormatter { get; set; }
        public Func<double, string> KWhFormatter { get; set; }
        public Func<double, string> MonthFormatter { get; set; } // Adds 1 to month index

        #endregion
    }

    /// <summary>
    ///     Converts a kWh qauntity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class KWhConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return (d / 1000).ToString("N1", culture); // for MWh

                if (d > 999999)
                    return (d / 1000000).ToString("N1", culture); // for GWh
                return d.ToString("N1", culture); // for kWh;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            if (double.TryParse((string) value, out d))
                return d;
            return 0.0;
        }
    }

    /// <summary>
    ///     Converts a kWh qauntity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class KWhConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return "MWh";

                if (d > 999999)
                    return "GWh";
                return "kWh";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "kWh";
        }
    }

    /// <summary>
    ///     Converts a kW quantity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class KWConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return (d / 1000).ToString("N1", culture); // for MW

                if (d > 999999)
                    return (d / 1000000).ToString("N1", culture); // for GW
                return d.ToString("N1", culture); // for kW;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            if (double.TryParse((string) value, out d))
                return d;
            return 0.0;
        }
    }

    /// <summary>
    ///     Converts a kW qauntity to MW or GW depending on magnitude of value
    /// </summary>
    public class KWConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return "MW";

                if (d > 999999)
                    return "GW";
                return "kW";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "kW";
        }
    }

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

    /// <summary>
    ///     If value is higher than 999999 m2, than show square km2
    /// </summary>
    public class AreaConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                if (d > 999999)
                    return "km²"; // km^2
            return "m²";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class NumericExtensions
    {
        public static double SafeDivision(this double Numerator, double Denominator)
        {
            return Denominator == 0 ? 0 : Numerator / Denominator;
        }
    }
}