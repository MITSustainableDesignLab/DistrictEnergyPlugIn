using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using LiveCharts;
using LiveCharts.Wpf;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class ResultsViewModel : INotifyPropertyChanged
    {
        private double _purchasedElec;
        private double _purchasedElecIntensity;
        private double _purchasedNgas;
        private double _purchasedNgasIntensity;

        public ResultsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            YFormatter = val =>
                (val / 1000).ToString("G0", CultureInfo.CreateSpecificCulture("en-US")) +
                " MWh"; // Formats the yAxis of the stacked graph
            GaugeFormatter = value => value.ToString("N1"); // Formats the gauge number
        }

        public static ResultsViewModel Instance { get; set; }
        public static SeriesCollection StackedHeatingSeries { get; set; } = new SeriesCollection();
        public static SeriesCollection StackedCoolingSeries { get; set; } = new SeriesCollection();
        public static SeriesCollection StackedElecSeries { get; set; } = new SeriesCollection();
        public static SeriesCollection PieHeatingChartGraphSeries { get; set; } = new SeriesCollection();
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }

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

        public Func<double, string> GaugeFormatter { get; set; }

        public SeriesCollection PieCoolingChartGraphSeries { get; set; } = new SeriesCollection();

        public event PropertyChangedEventHandler PropertyChanged;

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateHeatingStackedChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCoolingStackedChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateElecStackedChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateHeatingPieChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCoolingPieChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateMetrics;
        }

        /// <summary>
        ///     Updates the pie graph that shows the Heating energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateHeatingPieChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var HwDemand = new Dictionary<string, double[]>();
            HwDemand.Add("Solar Hot Water", instance.ResultsArray.HW_SHW);
            HwDemand.Add("Hot Water Tank", instance.ResultsArray.HW_HWT);
            HwDemand.Add("Electric Heat Pump", instance.ResultsArray.HW_EHP);
            HwDemand.Add("Natural Gas Boiler", instance.ResultsArray.HW_NGB);
            HwDemand.Add("Combined Heating and Power", instance.ResultsArray.HW_CHP);

            PieHeatingChartGraphSeries.Clear();

            foreach (var hw in HwDemand)
            {
                var temp = new PieSeries
                {
                    Values = new ChartValues<double> {hw.Value.Sum()},
                    Title = hw.Key,
                    DataLabels = true
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
            var chwDemand = new Dictionary<string, double[]>();
            chwDemand.Add("Absorption Chiller", instance.ResultsArray.CHW_ABS);
            chwDemand.Add("Electric Chiller", instance.ResultsArray.CHW_ECH);

            PieCoolingChartGraphSeries.Clear();

            foreach (var chw in chwDemand)
            {
                var temp = new PieSeries
                {
                    Values = new ChartValues<double> {chw.Value.Sum()},
                    Title = chw.Key,
                    DataLabels = false
                };
                PieCoolingChartGraphSeries.Add(temp);
            }
        }


        /// <summary>
        ///     Updates the Stacked graph that shows the heating energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateHeatingStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var HwDemand = new Dictionary<string, double[]>();
            HwDemand.Add("Solar Hot Water", instance.ResultsArray.HW_SHW);
            HwDemand.Add("Hot Water Tank", instance.ResultsArray.HW_HWT);
            HwDemand.Add("Electric Heat Pump", instance.ResultsArray.HW_EHP);
            HwDemand.Add("Natural Gas Boiler", instance.ResultsArray.HW_NGB);
            HwDemand.Add("Combined Heating and Power", instance.ResultsArray.HW_CHP);

            StackedHeatingSeries.Clear();

            foreach (var hw in HwDemand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(hw.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title = hw.Key,
                    LineSmoothness = 0.5
                };
                StackedHeatingSeries.Add(temp);
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
            var chwDemand = new Dictionary<string, double[]>();
            chwDemand.Add("Absorption Chiller", instance.ResultsArray.CHW_ABS);
            chwDemand.Add("Electric Chiller", instance.ResultsArray.CHW_ECH);

            StackedCoolingSeries.Clear();

            foreach (var chw in chwDemand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(chw.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title = chw.Key,
                    LineSmoothness = 0.5
                };
                StackedCoolingSeries.Add(temp);
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
            elecDemand.Add("Battery", instance.ResultsArray.ELEC_BAT);
            elecDemand.Add("Renewables", instance.ResultsArray.ELEC_REN);
            elecDemand.Add("Combined Heat & Power", instance.ResultsArray.ELEC_CHP);
            elecDemand.Add("Purchased Electricity", instance.ResultsArray.ELEC_PROJ);

            StackedElecSeries.Clear();

            foreach (var elec in elecDemand)
            {
                var temp = new StackedAreaSeries
                {
                    Values = new ChartValues<double>(elec.Value
                        .Select((x, i) => new {Index = i, Value = x})
                        .GroupBy(obj => obj.Index / 730)
                        .Select(obj => obj.Select(v => v.Value).Sum())),
                    Title = elec.Key,
                    LineSmoothness = 0.5
                };
                StackedElecSeries.Add(temp);
            }
        }


        private void UpdateMetrics(object sender, EventArgs e)
        {
            double totalGrossFloorArea = 1;
            var context = UmiContext.Current;
            if (context != null)
                totalGrossFloorArea = context.Buildings.All.Select(x => x.GrossFloorArea).Sum().Value;

            var instance = DHSimulateDistrictEnergy.Instance;

            PurchasedElec = instance.ResultsArray.ELEC_PROJ.Sum() / 1000; // To MWh
            PurchasedElecIntensity = PurchasedElec * 1000 / totalGrossFloorArea; // kWh/m2

            PurchasedNgas = instance.ResultsArray.NGAS_PROJ.Sum(); // To kWh
            PurchasedNgasIntensity = PurchasedNgas * 1000 / totalGrossFloorArea; // kWh/m2
        }
    }
}