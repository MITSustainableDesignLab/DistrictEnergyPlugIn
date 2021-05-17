using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Deedle;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Dtos;
using LiveCharts.Geared;
using LiveCharts.Wpf;
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
        private double _fixedTo;
        private double _fixedFrom;

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
            FixedFrom = From;
            To = new DateTime(2018, 01, 01, 0, 0, 0).AddHours(8760).Ticks;
            FixedTo = To;
        }

        public double FixedTo
        {
            get => _fixedTo;
            set
            {
                if (value.Equals(_fixedTo)) return;
                _fixedTo = value;
                OnPropertyChanged();
            }
        }

        public double FixedFrom
        {
            get => _fixedFrom;
            set
            {
                if (value.Equals(_fixedFrom)) return;
                _fixedFrom = value;
                OnPropertyChanged();
            }
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
            DemandLineCollection.Clear();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            DHRunLPModel.Instance.Completion += UpdateLoadsChart;
        }

        private void UpdateLoadsChart(object sender, EventArgs e)
        {
            var args = (DHRunLPModel.SimulationCompleted) e;
            var Total = new double[args.TimeSteps];
            var lineSmoothness = 0.1;
            SeriesCollection.Clear();
            DemandLineCollection.Clear();
            UnorderedCollection = new List<MySeries>();

            // Plot District Demand (Negative)
            var plotDuration = args.TimeSteps;
            foreach (var demand in DistrictControl.Instance.ListOfDistrictLoads)
            {
                if (Math.Abs(demand.Input.Sum()) > 0)
                {
                    var values = demand.Input.ToDateTimePoint().Split(plotDuration)
                        .Select(v => new DateTimePoint(v.First().DateTime, -v.Sum()));
                    var series = new GStackedAreaSeries
                    {
                        Values = values.AsGearedValues(),
                        Title = $"[{demand.LoadType}] {demand.Name}",
                        LineSmoothness = lineSmoothness,
                        LabelPoint = KWhLabelPointFormatter,
                        AreaLimit = 0,
                        Fill = demand.Fill
                    };
                    UnorderedCollection.Add(new MySeries {Variance = values.Variance(), Series = series});
                    DemandLineCollection.Add(new GLineSeries
                    {
                        Values = demand.Input.ToDateTimePoint().Split(plotDuration)
                            .Select(v => new DateTimePoint(v.First().DateTime, v.Sum())).AsGearedValues(),
                        Title = demand.Name,
                        LineSmoothness = lineSmoothness,
                        Stroke = demand.Fill,
                        PointGeometry = null,
                        StrokeThickness = 0.5,
                    });
                }

                Total = Total.Zip(demand.Input, (a, b) => a + b).ToArray();
            }

            // Plot Plant Supply & Demand
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<NotStorage>())
            {
                foreach (var cMat in plant.ConversionMatrix.Where(x =>
                    x.Key == LoadTypes.Cooling ||
                    x.Key == LoadTypes.Heating ||
                    x.Key == LoadTypes.Elec))
                {
                    var loadType = cMat.Key;
                    var eff = cMat.Value;
                    if (plant.Input.Sum() > 0)
                    {
                        var values = plant.Input.Split(plotDuration).Select(v =>
                            new DateTimePoint(v.First().DateTime, v.Select(o => o.Value * eff).Sum()));
                        var series = new GStackedAreaSeries
                        {
                            Values = values.AsGearedValues(),
                            Title = $"[{loadType}] {plant.Name}",
                            LineSmoothness = lineSmoothness,
                            LabelPoint = KWhLabelPointFormatter,
                            AreaLimit = 0,
                            Fill = plant.Fill[loadType]
                        };
                        UnorderedCollection.Add(new MySeries { Variance = values.Variance(), Series = series });
                    }
                }
            }

            // Plot Exports
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                {
                    if (plant.Input.Sum() > 0)
                    {
                        var values = plant.Input.Split(plotDuration).Select(v =>
                            new DateTimePoint(v.First().DateTime, v.Select(o => -o.Value).Sum()));
                        LoadTypes loadType = plant.OutputType;
                        var series = new GStackedAreaSeries
                        {
                            Values = values.AsGearedValues(),
                            Title = $"[{loadType}] {plant.Name}",
                            LineSmoothness = lineSmoothness,
                            LabelPoint = KWhLabelPointFormatter,
                            AreaLimit = 0,
                            Fill = plant.Fill[loadType]
                        };
                        UnorderedCollection.Add(new MySeries { Variance = values.Variance(), Series = series });
                    }
                }
            }

            StorageSeriesCollection.Clear();
            IsStorageVisible = false; // Start hidden
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                if (storage.Output.Sum() > 0)
                {
                    IsStorageVisible = true;  // Make visible
                    StorageSeriesCollection.Add(new GStackedAreaSeries()
                    {
                        Values = storage.Stored.Split(plotDuration)
                            .Select(v => new DateTimePoint(v.First().DateTime, v.Sum())).AsGearedValues(),
                        Title = storage.Name,
                        Fill = storage.Fill[storage.OutputType],
                        LineSmoothness = 0.5,
                        AreaLimit = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        PointGeometry = null,
                    });

                    // Plot Supply From Storage
                    var values = storage.Output.Split(plotDuration)
                        .Select(v => new DateTimePoint(v.First().DateTime, v.Sum()));
                    var series = new GStackedAreaSeries()
                    {
                        Values = values.AsGearedValues(),
                        Title = $"[{storage.OutputType}] {storage.Name} Out",
                        Fill = storage.Fill[storage.OutputType],
                        LineSmoothness = lineSmoothness,
                        AreaLimit = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        PointGeometry = null,
                    };
                    UnorderedCollection.Add(new MySeries { Variance = values.Variance(), Series = series });

                    // Plot Demand From Storage
                    var values2 = storage.Input.Split(plotDuration)
                        .Select(v => new DateTimePoint(v.First().DateTime, -v.Sum()));
                    var series2 = new GStackedAreaSeries()
                    {
                        Values = values2.AsGearedValues(),
                        Title = $"[{storage.OutputType}] {storage.Name} In",
                        Fill = storage.Fill[storage.InputType],
                        LineSmoothness = lineSmoothness,
                        AreaLimit = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        PointGeometry = null,
                    };
                    UnorderedCollection.Add(new MySeries { Variance = values2.Variance(), Series = series2 });
                }
            }

            SeriesCollection.AddRange(UnorderedCollection.OrderBy(x=>x.Variance).Select(o=>o.Series));
        }

        public List<MySeries> UnorderedCollection { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MySeries
    {
        public double Variance { get; set; }
        public GStackedAreaSeries Series { get; set; }
    }
}