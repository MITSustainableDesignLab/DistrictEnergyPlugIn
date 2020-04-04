using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class ResultsViewModel : INotifyPropertyChanged
    {
        private int _aggregationPeriod = 730;
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
        private double _xPointer;
        private double _yPointer;

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
                if (Math.Abs(chartPoint.Y) > 999) return string.Format("{0:N1} MWh", chartPoint.Y / 1000);

                if (Math.Abs(chartPoint.Y) > 999999) return string.Format("{0:N1} GWh", chartPoint.Y / 1000000);
                return string.Format("{0:N1} kWh", chartPoint.Y);
            };

            MonthFormatter = val => (val + 1).ToString(CultureInfo.CreateSpecificCulture("en-US"));

            GaugeFormatter = value => value.ToString("N1"); // Formats the gauge number

            //lets initialize in an invisible location
            XPointer = -5;
            YPointer = -5;

            //the formatter or labels property is shared 
            Formatter = x => x.ToString("N2");

            Labels = new[]
            {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            };

        }

        public string[] Labels { get; set; }
        public ResultsViewModel Instance { get; set; }
        public SeriesCollection StackedSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedDemandSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedHeatingSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedCoolingSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection StackedElecSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection PieHeatingChartGraphSeries { get; set; } = new SeriesCollection();
        public SeriesCollection PieCoolingChartGraphSeries { get; set; } = new SeriesCollection();

        public double XPointer
        {
            get { return _xPointer; }
            set
            {
                _xPointer = value;
                OnPropertyChanged("XPointer");
            }
        }

        public double YPointer
        {
            get { return _yPointer; }
            set
            {
                _yPointer = value;
                OnPropertyChanged("YPointer");
            }
        }

        public Func<double, string> Formatter { get; set; }

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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateStackedChart;
            DHSimulateDistrictEnergy.Instance.PluginSettings.PropertyChanged += UpdateStackedChart;
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
            StackedSeriesCollection.Clear();
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

        public void UpdateStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var Demand = new List<ChartValue>
            {
                new ChartValue {Key = "Chilled Water Demand", Fill = new SolidColorBrush(Color.FromRgb(0, 140, 218)), Value = instance.DistrictDemand.ChwN},
                new ChartValue {Key = "Hot Water Demand", Fill = new SolidColorBrush(Color.FromRgb(235, 45, 45)), Value = instance.DistrictDemand.HwN},
                new ChartValue
                {
                    Key = "Total Electricity Demand",
                    Fill = new SolidColorBrush(Color.FromRgb(173, 221, 67)), 
                    Value = instance.DistrictDemand.ElecN.Zip(instance.ResultsArray.ElecEch, (x, y) => x + y).ToArray()
                            .Zip(instance.ResultsArray.ElecEhp, (x, y) => x + y).ToArray()
                }
            };
            var Supply = new List<ChartValue>
            {
                new ChartValue {Key = "CW Absorption Chiller", Fill = new SolidColorBrush(Color.FromRgb(146,241,254)), Value = instance.ResultsArray.ChwAbs},
                new ChartValue {Key = "CW Electric Chiller", Fill = new SolidColorBrush(Color.FromRgb(93,153,170)), Value = instance.ResultsArray.ChwEch},
                new ChartValue {Key = "CW Evaporator Side of EHPs", Fill = new SolidColorBrush(Color.FromRgb(0, 140, 218)), Value = instance.ResultsArray.ChwEhpEvap},
                new ChartValue {Key = "HW Solar Hot Water", Fill = new SolidColorBrush(Color.FromRgb(251,209,39)), Value = instance.ResultsArray.HwShw},
                new ChartValue {Key = "HW Hot Water Tank", Fill = new SolidColorBrush(Color.FromRgb(253,199,204)), Value = instance.ResultsArray.HwHwt},
                new ChartValue {Key = "HW Electric Heat Pump", Fill = new SolidColorBrush(Color.FromRgb(231,71,126)), Value = instance.ResultsArray.HwEhp},
                new ChartValue {Key = "HW Natural Gas Boiler", Fill = new SolidColorBrush(Color.FromRgb(189,133,74)), Value = instance.ResultsArray.HwNgb},
                new ChartValue {Key = "HW Combined Heating and Power", Fill = new SolidColorBrush(Color.FromRgb(247,96,21)), Value = instance.ResultsArray.HwChp},
                new ChartValue {Key = "EL Battery", Fill = new SolidColorBrush(Color.FromRgb(192,244,66)), Value = instance.ResultsArray.ElecBat},
                new ChartValue {Key = "EL Renewables", Fill = new SolidColorBrush(Color.FromRgb(112,159,15)), Value = instance.ResultsArray.ElecRen},
                new ChartValue {Key = "EL Combined Heat & Power", Fill = new SolidColorBrush(Color.FromRgb(253,199,204)), Value = instance.ResultsArray.ElecChp},
                new ChartValue {Key = "EL Purchased Electricity", Fill = new SolidColorBrush(Color.FromRgb(0,0,0)), Value = instance.ResultsArray.ElecProj}
            };

            StackedSeriesCollection.Clear();

            foreach (var demand in Demand)
            {
                var series = new StackedAreaSeries
                {
                    Values = AggregateByPeriod(demand.Value, true, instance.PluginSettings.AggregationPeriod),
                    Title = demand.Key,
                    LineSmoothness = 0,
                    LabelPoint = KWhLabelPointFormatter,
                    AreaLimit = 0,
                    Fill = demand.Fill
                };
                StackedSeriesCollection.Add(series);
            }

            foreach (var supply in Supply)
            {
                var series = new StackedAreaSeries
                {
                    Values = AggregateByPeriod(supply.Value, false, instance.PluginSettings.AggregationPeriod),
                    Title = supply.Key,
                    LineSmoothness = 0,
                    LabelPoint = KWhLabelPointFormatter,
                    AreaLimit = 0,
                    Fill = supply.Fill
                };
                StackedSeriesCollection.Add(series);
            }
        }

        private void UpdateDemandStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var Demand = new Dictionary<string, double[]>
            {
                {"Heating", instance.DistrictDemand.ChwN},
                {"Cooling", instance.DistrictDemand.HwN},
                {"Electricity", instance.DistrictDemand.ElecN}
            };

            StackedDemandSeriesCollection.Clear();

            foreach (var d in Demand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = AggregateByPeriod(d.Value, false, instance.PluginSettings.AggregationPeriod),
                    Title = d.Key,
                    LineSmoothness = 0,
                    LabelPoint = KWhLabelPointFormatter
                };
                StackedDemandSeriesCollection.Add(temp);
            }
        }

        private static ChartValues<double> AggregateByPeriod(double[] d, bool negative = true, int period = 730)
        {
            if (negative)
                return new ChartValues<double>(d
                    .Select((x, i) => new {Index = i, Value = x})
                    .GroupBy(obj => obj.Index / period)
                    .Select(obj => obj.Select(v => -v.Value).Sum()));
            return new ChartValues<double>(d
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(obj => obj.Index / period)
                .Select(obj => obj.Select(v => v.Value).Sum()));
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
                {"Solar Hot Water", instance.HwShw},
                {"Hot Water Tank", instance.HwHwt},
                {"Electric Heat Pump", instance.HwEhp},
                {"Natural Gas Boiler", instance.HwNgb},
                {"Combined Heating and Power", instance.HwChp}
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
            var elecDemand = new Dictionary<string, double[]>
            {
                {"Battery", instance.ResultsArray.ElecBat},
                {"Renewables", instance.ResultsArray.ElecRen},
                {"Combined Heat & Power", instance.ResultsArray.ElecChp},
                {"Purchased Electricity", instance.ResultsArray.ElecProj}
            };

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

        public class ChartValue
        {
            public string Key { get; set; }
            public SolidColorBrush Fill { get; set; }
            public double[] Value { get; set; }
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

        public ChartMode ChartMode { get; set; }

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