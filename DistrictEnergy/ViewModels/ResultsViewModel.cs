using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiveCharts;
using LiveCharts.Wpf;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class ResultsViewModel
    {
        public ResultsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            YFormatter = val => (val / 1000).ToString("G0", CultureInfo.CreateSpecificCulture("en-US")) + " MWh";
        }

        public static ResultsViewModel Instance { get; set; }
        public static SeriesCollection StackedGraphSeries { get; set; } = new SeriesCollection();
        public static SeriesCollection PieChartGraphSeries { get; set; } = new SeriesCollection();
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public double PurchasedElec { get; set; }

        public double PurchasedElecIntensity { get; set; }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateStackedChart;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateMetrics;
        }

        /// <summary>
        ///     Updates the Stacked graph that shows the heating energy demand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateStackedChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var HwDemand = new Dictionary<string, double[]>();
            HwDemand.Add("Solar Hot Water", instance.ResultsArray.HW_SHW);
            HwDemand.Add("Hot Water Tank", instance.ResultsArray.HW_HWT);
            HwDemand.Add("Electric Heat Pump", instance.ResultsArray.HW_EHP);
            HwDemand.Add("Natural Gas Boiler", instance.ResultsArray.HW_NGB);
            HwDemand.Add("Combined Heating and Power", instance.ResultsArray.HW_CHP);

            StackedGraphSeries.Clear();

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
                StackedGraphSeries.Add(temp);
            }
        }

        private void UpdateMetrics(object sender, EventArgs e)
        {
            double totalGrossFloorArea = 1;
            var context = UmiContext.Current;
            if (context != null)
                totalGrossFloorArea = context.Buildings.All.Select(x => x.GrossFloorArea).Sum().Value;

            var instance = DHSimulateDistrictEnergy.Instance;

            PurchasedElec = instance.ResultsArray.ELEC_PROJ.Sum();
            PurchasedElecIntensity = PurchasedElec / totalGrossFloorArea;
        }
    }
}