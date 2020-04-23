using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Deedle;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
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
            XFormatter = val => new DateTime((long) val).ToString("yyyy");
            YFormatter = val => val.ToString("N") + " M";
            SeriesCollection = new SeriesCollection();
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnProjectClosed(object sender, EventArgs e)
        {
            SeriesCollection.Clear();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;

            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateLoadsChart;
        }

        private void UpdateLoadsChart(object sender, EventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var instance = DHSimulateDistrictEnergy.Instance;
            var Demand = new List<ResultsViewModel.ChartValue>
            {
                new ResultsViewModel.ChartValue
                {
                    Key = "Cooling Demand", Fill = new SolidColorBrush(Color.FromRgb(0, 140, 218)),
                    Value = instance.DistrictDemand.ChwN
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Heating Demand", Fill = new SolidColorBrush(Color.FromRgb(235, 45, 45)),
                    Value = instance.DistrictDemand.HwN.Zip(instance.ResultsArray.HwAbs, (x, y)=> x+y).ToArray()
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Electricity Demand",
                    Fill = new SolidColorBrush(Color.FromRgb(173, 221, 67)),
                    Value = instance.DistrictDemand.ElecN.Zip(instance.ResultsArray.ElecEch, (x, y) => x + y).ToArray()
                        .Zip(instance.ResultsArray.ElecEhp, (x, y) => x + y).ToArray()
                },
                // todo Add demand by Battery and Thermal Storage
            };
            var Supply = new List<ResultsViewModel.ChartValue>
            {
                new ResultsViewModel.ChartValue
                {
                    Key = "Cooling from Absorption Chiller", Fill = new SolidColorBrush(Color.FromRgb(146, 241, 254)),
                    Value = instance.ResultsArray.ChwAbs
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Cooling from Electric Chiller", Fill = new SolidColorBrush(Color.FromRgb(93, 153, 170)),
                    Value = instance.ResultsArray.ChwEch
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Cooling from Evaporator Side of EHPs",
                    Fill = new SolidColorBrush(Color.FromRgb(0, 140, 218)),
                    Value = instance.ResultsArray.ChwEhpEvap
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Cooling from Custom Supply",
                    Value = instance.ResultsArray.ChwCustom
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Heating from Solar Hot Water", Fill = new SolidColorBrush(Color.FromRgb(251, 209, 39)),
                    Value = instance.ResultsArray.HwShw
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Heating from Hot Water Tank", Fill = new SolidColorBrush(Color.FromRgb(253, 199, 204)),
                    Value = instance.ResultsArray.HwHwt
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Heating from Electric Heat Pump", Fill = new SolidColorBrush(Color.FromRgb(231, 71, 126)),
                    Value = instance.ResultsArray.HwEhp
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Heating from Natural Gas Boiler", Fill = new SolidColorBrush(Color.FromRgb(189, 133, 74)),
                    Value = instance.ResultsArray.HwNgb
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Heating from CHP",
                    Fill = new SolidColorBrush(Color.FromRgb(247, 96, 21)),
                    Value = instance.ResultsArray.HwChp
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Electricity from Battery", Fill = new SolidColorBrush(Color.FromRgb(192, 244, 66)),
                    Value = instance.ResultsArray.ElecBat
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Electricity from Renewables", Fill = new SolidColorBrush(Color.FromRgb(112, 159, 15)),
                    Value = instance.ResultsArray.ElecRen
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Electricity from CHP",
                    Fill = new SolidColorBrush(Color.FromRgb(253, 199, 204)),
                    Value = instance.ResultsArray.ElecChp
                },
                new ResultsViewModel.ChartValue
                {
                    Key = "Electricity from Purchased Electricity", Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    Value = instance.ResultsArray.ElecProj
                }
            };

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<CustomCoolingSupplyModule>())
            {
                Supply.Add(new ResultsViewModel.ChartValue()
                {
                    Key = plant.Name,
                    Value = plant.Used,
                    Fill = plant.Fill
                });
                
            }

            SeriesCollection.Clear();

            foreach (var demand in Demand)
                if (Math.Abs(demand.Value.Sum()) > 0.001)
                {
                    var series = new StackedColumnSeries
                    {
                        Values = AggregateByPeriod(demand.Value, true, instance.PluginSettings.AggregationPeriod),
                        Title = demand.Key,
                        //LineSmoothness = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        //AreaLimit = 0,
                        Fill = demand.Fill
                    };
                    SeriesCollection.Add(series);
                }

            foreach (var supply in Supply)
                if (Math.Abs(supply.Value.Sum()) > 0.001)
                {
                    var series = new StackedColumnSeries
                    {
                        Values = AggregateByPeriod(supply.Value, false, instance.PluginSettings.AggregationPeriod),
                        Title = supply.Key,
                        // LineSmoothness = 0,
                        LabelPoint = KWhLabelPointFormatter,
                        // AreaLimit = 0,
                        Fill = supply.Fill
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