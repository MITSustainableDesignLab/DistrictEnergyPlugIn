using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class LoadsViewModel : INotifyPropertyChanged
    {
        private double _xPointer;
        private double _yPointer;
        private Func<double, string> _xFormatter;
        private double _from;
        private double _to;
        private Func<double, string> _timeFormatter;
        private bool _isStorageVisible;

        public LoadsViewModel()
        {
            Instance = this;
            TimeFormatter = x => new DateTime((long) x).ToString("MMMM");
            SeriesCollection = new SeriesCollection();
            StorageSeriesCollection = new SeriesCollection();
            DemandLineCollection = new SeriesCollection();
            IsStorageVisible = false;
            //lets initialize in an invisible location
            XPointer = new DateTime(2018, 01, 01, 0, 0, 0).Ticks;
            YPointer = 0;

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;

            KWhLabelPointFormatter = delegate(ChartPoint chartPoint)
            {
                if (Math.Abs(chartPoint.Y) > 999) return string.Format("{0:N1} MWh", chartPoint.Y / 1000);

                if (Math.Abs(chartPoint.Y) > 999999) return string.Format("{0:N1} GWh", chartPoint.Y / 1000000);
                return string.Format("{0:N1} kWh", chartPoint.Y);
            };
            YFormatter = delegate(double value)
            {
                if (Math.Abs(value) > 999) return string.Format("{0:N0} MWh", value / 1000);
                if (Math.Abs(value) > 999999) return string.Format("{0:N0} GWh", value / 1000000);
                return string.Format("{0:N0} kWh", value);
            };

            From = new DateTime(2018, 01, 01, 0, 0, 0).Ticks;
            To = new DateTime(2018, 01, 01, 0, 0, 0).AddHours(8760).Ticks;
        }

        public bool IsStorageVisible
        {
            get => _isStorageVisible;
            set
            {
                if (value == _isStorageVisible) return;
                _isStorageVisible = value;
                OnPropertyChanged();
            }
        }

        public SeriesCollection DemandLineCollection { get; set; }

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

        public SeriesCollection SeriesCollection { get; set; }

        public Func<double, string> XFormatter
        {
            get => _xFormatter;
            set
            {
                _xFormatter = value;
                OnPropertyChanged();
            }
        }

        public double From
        {
            get { return _from; }
            set
            {
                _from = value;
                OnPropertyChanged(nameof(From));
            }
        }

        public double To
        {
            get { return _to; }
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));
            }
        }

        public Func<double, string> YFormatter { get; set; }

        public Func<double, string> TimeFormatter
        {
            get { return _timeFormatter; }
            set
            {
                _timeFormatter = value;
                OnPropertyChanged(nameof(TimeFormatter));
            }
        }

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
            DHRunLPModel.Instance.Completion += UpdateLoadsChart;
        }

        private void UpdateLoadsChart(object sender, EventArgs e)
        {
            var args = (DHRunLPModel.SimulationCompleted) e;
            var Total = new double[args.TimeSteps];
            var lineSmoothness = 0.5;
            SeriesCollection.Clear();
            DemandLineCollection.Clear();

            // Plot Demand (Negative)
            var plot_duration = args.TimeSteps;
            foreach (var demand in DistrictControl.Instance.ListOfDistrictLoads)
            {
                if (Math.Abs(demand.Input.Sum()) > 0)
                {
                    var series = new GStackedAreaSeries
                    {
                        Values = demand.Input.Split(plot_duration)
                            .Select(v => new DateTimePoint(v.First().DateTime, -v.Sum())).AsGearedValues(),
                        Title = "[+] " + demand.Name,
                        LineSmoothness = lineSmoothness,
                        LabelPoint = KWhLabelPointFormatter,
                        AreaLimit = 0,
                        Fill = demand.Fill
                    };
                    SeriesCollection.Add(series);
                    DemandLineCollection.Add(new GLineSeries
                    {
                        Values = demand.Input.Split(12)
                            .Select(v => new DateTimePoint(v.First().DateTime, v.Sum())).AsGearedValues(),
                        Title = demand.Name,
                        LineSmoothness = lineSmoothness,
                        Stroke = demand.Fill
                    });
                }

                Total = Total.Zip(demand.Input, (a, b) => a + b.Value).ToArray();
            }

            // Plot Additional Demand from Supply Modules (Negative)
            foreach (var dispatchable in DistrictControl.Instance.ListOfPlantSettings.OfType<IThermalPlantSettings>()
                .Where(x =>
                    x.InputType == LoadTypes.Cooling || x.InputType == LoadTypes.Heating ||
                    x.InputType == LoadTypes.Elec))
            {
                if (dispatchable.Input.Sum() > 0)
                {
                    var series = new GStackedAreaSeries
                    {
                        Values = dispatchable.Input.Split(plot_duration)
                            .Select(v => new DateTimePoint(v.First().DateTime, -v.Sum())).AsGearedValues(),
                        Title = "[+] " + dispatchable.Name,
                        LineSmoothness = lineSmoothness,
                        LabelPoint = KWhLabelPointFormatter,
                        AreaLimit = 0,
                        Fill = dispatchable.Fill
                    };
                    SeriesCollection.Add(series);
                }

                Total = Total.Zip(dispatchable.Input, (a, b) => a + b.Value).ToArray();
            }

            // Plot Total Demand as Line
            // var gLineSeries = new GLineSeries
            // {
            //     Values = Total.AsChartValues(),
            //     Title = "Total",
            //     //LineSmoothness = lineSmoothness,
            //     LabelPoint = KWhLabelPointFormatter,
            //     Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            //     Fill = null,
            // };
            // SeriesCollection.Add(gLineSeries);
            // Panel.SetZIndex(gLineSeries, 60);


            // Plot Supply (Positive)
            foreach (var supply in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>().Where(x =>
                x.OutputType == LoadTypes.Cooling ||
                x.OutputType == LoadTypes.Heating ||
                x.OutputType == LoadTypes.Elec))
                if (supply.Output.Sum() > 0)
                {
                    var series = new GStackedAreaSeries
                    {
                        Values = supply.Output.Split(plot_duration)
                            .Select(v => new DateTimePoint(v.First().DateTime, v.Sum())).AsGearedValues(),
                        Title = supply.Name,
                        LineSmoothness = lineSmoothness,
                        LabelPoint = KWhLabelPointFormatter,
                        AreaLimit = 0,
                        Fill = supply.Fill
                    };
                    SeriesCollection.Add(series);
                }

            StorageSeriesCollection.Clear();
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                if (storage.Output.Sum() > 0)
                {
                    StorageSeriesCollection.Add(new GStackedAreaSeries()
                    {
                        Values = storage.Stored.Split(plot_duration)
                            .Select(v => new DateTimePoint(v.First().DateTime, v.Sum())).AsGearedValues(),
                        Title = storage.Name,
                        Fill = storage.Fill,
                        LineSmoothness = lineSmoothness,
                        AreaLimit = 0,
                        LabelPoint = KWhLabelPointFormatter,
                    });

                    // Plot Supply From Storage
                    SeriesCollection.Add(new GStackedAreaSeries()
                    {
                        Values = storage.Output.Split(plot_duration)
                            .Select(v => new DateTimePoint(v.First().DateTime, v.Sum())).AsGearedValues(),
                        Title = storage.Name,
                        Fill = storage.Fill,
                        LineSmoothness = lineSmoothness,
                        AreaLimit = 0,
                        LabelPoint = KWhLabelPointFormatter,
                    });
                    IsStorageVisible = true;
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}