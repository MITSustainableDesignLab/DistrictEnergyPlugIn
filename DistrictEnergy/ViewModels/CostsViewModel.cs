using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using DistrictEnergy.Annotations;
using LiveCharts;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class CostsViewModel : INotifyPropertyChanged
    {
        private double _totalCost;

        public CostsViewModel()
        {
            SeriesCollection = new SeriesCollection();
            Labels = new[] {"Natural Gas", "Grid Electricity"};
            Formatter = delegate(double value)
            {
                if (Math.Abs(value) > 999999) return string.Format("{0:N0} M$", value / 1000000);
                return string.Format("{0:N0} $", value);
            };
            TotalCost = 0;

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;

            CostLabelPointFormatter = delegate(ChartPoint chartPoint)
            {
                if (Math.Abs(chartPoint.Y) > 999999) return string.Format("{0:N} M$", chartPoint.Y / 1000000);
                return string.Format("{0:N2} $", chartPoint.Y);
            };
        }

        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }
        public Func<ChartPoint, string> CostLabelPointFormatter { get; set; }

        public double TotalCost
        {
            get => _totalCost;
            set
            {
                _totalCost = value;
                OnPropertyChanged();
            }
        }

        public SeriesCollection SeriesCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnProjectClosed(object sender, EventArgs e)
        {
            SeriesCollection.Clear();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCostsChart;
        }

        private void UpdateCostsChart(object sender, EventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var instance = DHSimulateDistrictEnergy.Instance;
            SeriesCollection.Clear();

            var gasCost = instance.ResultsArray.NgasProj.Sum() * UmiContext.Current.ProjectSettings.GasDollars;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Natural Gas",
                Values = new ChartValues<double>
                {
                    gasCost
                },
                LabelPoint = CostLabelPointFormatter
            });
            var electricityCost = instance.ResultsArray.ElecProj.Sum() *
                                  UmiContext.Current.ProjectSettings.ElectricityDollars;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Grid Electricity",
                Values = new ChartValues<double>
                {
                    electricityCost
                },
                LabelPoint = CostLabelPointFormatter
            });
            TotalCost = electricityCost + gasCost;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}