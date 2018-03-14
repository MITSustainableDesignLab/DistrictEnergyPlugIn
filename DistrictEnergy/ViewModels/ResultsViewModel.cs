using System;
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
        }

        public static ResultsViewModel Instance { get; set; }
        public static SeriesCollection Series { get; set; } = new SeriesCollection();

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance != null)
                DHSimulateDistrictEnergy.Instance.ResultsArray.ArrayChanged += UpdateChart;
        }

        private void UpdateChart(object sender, EventArgs e)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var hwShw = instance.ResultsArray.HW_SHW;
            var hwHwt = instance.ResultsArray.HW_HWT;
            var hwAbs = instance.ResultsArray.HW_ABS;
            Series.Clear();
            var a = new StackedAreaSeries
            {
                Values = new ChartValues<double>(hwShw
                    .Select((x, i) => new {Index = i, Value = x})
                    .GroupBy(obj => obj.Index / 730)
                    .Select(obj => obj.Select(v => v.Value).Sum())),
                Title = "Solar Hot Water",
                LineSmoothness = 0
            };
            var b = new StackedAreaSeries
            {
                Values = new ChartValues<double>(hwHwt
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(obj => obj.Index / 730)
                    .Select(obj => obj.Select(v => v.Value).Sum())),
                Title = "Hot water Tank",
                LineSmoothness = 0
            };
            var c = new StackedAreaSeries
            {
                Values = new ChartValues<double>(hwAbs
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(obj => obj.Index / 730)
                    .Select(obj => obj.Select(v => v.Value).Sum())),
                Title = "Absorption Chiller",
                LineSmoothness = 0
            };
            Series.Add(a);
            Series.Add(b);
            Series.Add(c);
        }
    }
}