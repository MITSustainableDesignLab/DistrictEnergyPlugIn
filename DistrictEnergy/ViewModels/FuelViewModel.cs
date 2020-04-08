using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Deedle;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using LiveCharts;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class FuelViewModel : INotifyPropertyChanged
    {
        public FuelViewModel()
        {
            SeriesCollection = new SeriesCollection();
            Labels = new[]
            {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            };

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;

            KWhLabelPointFormatter = delegate(ChartPoint chartPoint)
            {
                if (Math.Abs(chartPoint.Y) > 999) return string.Format("{0:N1} MWh", chartPoint.Y / 1000);

                if (Math.Abs(chartPoint.Y) > 999999) return string.Format("{0:N1} GWh", chartPoint.Y / 1000000);
                return string.Format("{0:N1} kWh", chartPoint.Y);
            };

            Formatter = delegate(double value)
            {
                if (Math.Abs(value) > 999) return string.Format("{0:N0} MWh", value / 1000);
                if (Math.Abs(value) > 999999) return string.Format("{0:N0} GWh", value / 1000000);
                return string.Format("{0:N0} kWh", value);
            };
        }

        public Func<double, string> Formatter { get; set; }

        public Func<ChartPoint, string> KWhLabelPointFormatter { get; set; }
        public string[] Labels { get; set; }
        public SeriesCollection SeriesCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnProjectClosed(object sender, EventArgs e)
        {
            SeriesCollection.Clear();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCarbonChart;
        }

        private void UpdateCarbonChart(object sender, EventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var instance = DHSimulateDistrictEnergy.Instance;

            var Fuel = new List<ResultsViewModel.ChartValue>()
            {
                new ResultsViewModel.ChartValue
                {
                    Key = "Natural Gas",
                    Fill = new SolidColorBrush(Color.FromRgb(189, 133, 74)),
                    Value = instance.ResultsArray.NgasProj
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Purchased Elec",
                    Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    Value = instance.ResultsArray.ElecProj
                },
            };

            SeriesCollection.Clear();

            foreach (var fuel in Fuel)
                if (Math.Abs(fuel.Value.Sum()) > 0.001)
                {
                    var series = new StackedColumnSeries
                    {
                        Values = AggregateByPeriod(fuel.Value, false, instance.PluginSettings.AggregationPeriod),
                        Title = fuel.Key,
                        //LineSmoothness = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        //AreaLimit = 0,
                        Fill = fuel.Fill
                    };
                    SeriesCollection.Add(series);
                }
        }

        private ChartValues<double> AggregateByPeriod(double[] d, bool negative = true,
            TimeGroupers period = TimeGroupers.Monthly)
        {
            // Using a startdate of "2018-01-01" because it starts on a Monday
            var startDate = new DateTime(2018, 01, 01, 0, 0, 0);

            // Create SeriesBuilder
            var seriesBuilder = new SeriesBuilder<DateTime, double>();

            // Iterate over each element of the array of results & create datetime index incrementally
            for (var i = 0; i < d.Length; i++) seriesBuilder.Add(startDate.AddHours(i), d[i]);

            // To Series.
            var series = seriesBuilder.Series;
            Series<DateTime, double> result;
            // Group the series by period
            if (period == TimeGroupers.Monthly)
                result = series.GroupBy(c => new DateTime(2018, c.Key.Month, 01))
                    .Select(g => g.Value.Values.Sum());
            else
                result = series.GroupBy(c => c.Key.Date)
                    .Select(g => g.Value.Values.Sum());


            if (negative) result = -result;

            return result.Values.AsChartValues();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}