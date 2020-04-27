using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Deedle;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
using LiveCharts.Geared;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class LoadsViewModel : INotifyPropertyChanged
    {
        private double _xPointer;
        private double _yPointer;

        public LoadsViewModel()
        {
            Instance = this;
            XFormatter = val => new DateTime((long) val).ToString("yyyy");
            YFormatter = val => val.ToString("N") + " M";
            SeriesCollection = new SeriesCollection();
            StorageSeriesCollection = new SeriesCollection();
            Labels = new[]
            {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            };
            //lets initialize in an invisible location
            XPointer = -5;
            YPointer = -5;
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

        public static LoadsViewModel Instance { get; set; }

        public Func<ChartPoint, string> KWhLabelPointFormatter { get; set; }

        public double XPointer
        {
            get => _xPointer;
            set
            {
                _xPointer = value;
                OnPropertyChanged();
            }
        }

        public double YPointer
        {
            get => _yPointer;
            set
            {
                _yPointer = value;
                OnPropertyChanged();
            }
        }

        public string[] Labels { get; set; }

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public Func<double, string> Formatter { get; set; }

        public SeriesCollection StorageSeriesCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnProjectClosed(object sender, EventArgs e)
        {
            SeriesCollection.Clear();
            StorageSeriesCollection.Clear();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateLoadsChart;
            DHRunLPModel.Instance.Completion += UpdateLoadsChart;
        }

        private void UpdateLoadsChart(object sender, EventArgs e)
        {
            var args = (DHRunLPModel.SimulationCompleted) e;
            var Total = new double[args.TimeStep];
            var Supply = new List<ResultsViewModel.ChartValue>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<IDispatchable>().Where(x =>
                x.OutputType == LoadTypes.Cooling || x.OutputType == LoadTypes.Heating ||
                x.OutputType == LoadTypes.Elec))
            {
                Supply.Add(new ResultsViewModel.ChartValue
                {
                    Key = supplymodule.Name,
                    Fill = supplymodule.Fill,
                    Value = supplymodule.Output
                });
            }

            if (DHSimulateDistrictEnergy.Instance == null) return;
            var instance = DHSimulateDistrictEnergy.Instance;

            var Demand = new List<ResultsViewModel.ChartValue>();
            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads)
            {
                Demand.Add(new ResultsViewModel.ChartValue
                {
                    Key = load.Name,
                    Fill = load.Fill,
                    Value = load.HourlyLoads
                });
            }

            SeriesCollection.Clear();

            foreach (var demand in Demand)
                if (Math.Abs(demand.Value.Sum()) > 0.001)
                {
                    var series = new GStackedAreaSeries
                    {
                        Values = AggregateByPeriod(demand.Value, args.TimeStep),
                        Title = demand.Key,
                        //LineSmoothness = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        AreaLimit = 0,
                        Fill = demand.Fill
                    };
                    SeriesCollection.Add(series);
                }
            foreach (var dispatchable in DistrictControl.Instance.ListOfPlantSettings.OfType<IDispatchable>().Where(x =>
                x.InputType == LoadTypes.Cooling || x.InputType == LoadTypes.Heating || x.InputType == LoadTypes.Elec))
            {
                var series = new GStackedAreaSeries
                {
                    Values = dispatchable.Input.Select(x => -x).AsChartValues(),
                    Title = dispatchable.Name,
                    //LineSmoothness = 0,
                    LabelPoint = KWhLabelPointFormatter,
                    AreaLimit = 0,
                    Fill = dispatchable.Fill
                };
                SeriesCollection.Add(series);
                Total = Total.Zip(dispatchable.Output, (a, b) => a + b).ToArray();
            }

            var gLineSeries = new GLineSeries
            {
                Values = Total.AsChartValues(),
                Title = "Total",
                //LineSmoothness = 0,
                LabelPoint = KWhLabelPointFormatter,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                Fill = null,
            };
            SeriesCollection.Add(gLineSeries);
            Panel.SetZIndex(gLineSeries, 60);

            foreach (var supply in Supply)
                if (Math.Abs(supply.Value.Sum()) > 0.001)
                {
                    var series = new GStackedAreaSeries
                    {
                        Values = supply.Value.AsChartValues(),
                        Title = supply.Key,
                        // LineSmoothness = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        AreaLimit = 0,
                        Fill = supply.Fill
                    };
                    SeriesCollection.Add(series);
                }

            StorageSeriesCollection.Clear();
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<IStorage>())
            {
                if (storage.Input.Sum() > 0)
                {
                    StorageSeriesCollection.Add(new GStackedAreaSeries()
                    {
                        Values = storage.Storage.AsChartValues(),
                        Title = storage.Name,
                        Fill = storage.Fill,
                        AreaLimit = 0,
                        LabelPoint = KWhLabelPointFormatter,
                    });
                }
            }
        }

        private ChartValues<double> AggregateByPeriod(double[] d, int timestep)
        {
            return d
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / (8760 / timestep))
                .Select(x => x.Select(v => -v.Value).Sum()).AsChartValues();
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