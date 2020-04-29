using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;
using DistrictEnergy.Helpers;
using LiveCharts.Wpf;

namespace DistrictEnergy.ViewModels
{
    internal class CostsViewModel : INotifyPropertyChanged
    {
        private double _totalCost = 0;
        private double _normalizedTotalCost = 1;

        public CostsViewModel()
        {
            SeriesCollection = new SeriesCollection();
            Formatter = delegate(double value)
            {
                if (Math.Abs(value) > 999999) return string.Format("{0:N} M$", value / 1000000);
                return string.Format("{0:N0} $", value);
            };
            TotalCost = 0;

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;

            CostLabelPointFormatter = delegate(ChartPoint chartPoint)
            {
                if (Math.Abs(chartPoint.Y) > 999999) return string.Format("{0:N3} M$", chartPoint.Y / 1000000);
                return string.Format("{0:N2} $", chartPoint.Y);
            };
        }

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
            if (DHSimulateDistrictEnergy.Instance == null) return;
            DHRunLPModel.Instance.Completion += UpdateCostsChart;
        }

        private void UpdateCostsChart(object sender, EventArgs e)
        {
            var args = (DHRunLPModel.SimulationCompleted) e;
            var Total = new double[args.TimeSteps];
            TotalCost = 0;
            SeriesCollection.Clear();
            foreach (var supplyModule in DistrictControl.Instance.ListOfPlantSettings)
            {
                if (supplyModule.FixedCost.Cost > 0)
                {
                    SeriesCollection.Add(new PieSeries
                    {
                        Title = supplyModule.FixedCost.Name,
                        Values = new ChartValues<double> {supplyModule.FixedCost.Cost},
                        LabelPoint = CostLabelPointFormatter,
                        Fill = supplyModule.FixedCost.Fill
                    });
                }
                if (supplyModule.VariableCost.Cost > 0)
                {
                    SeriesCollection.Add(new PieSeries
                    {
                        Title = supplyModule.VariableCost.Name,
                        Values = new ChartValues<double> { supplyModule.VariableCost.Cost },
                        LabelPoint = CostLabelPointFormatter,
                        Fill = supplyModule.VariableCost.Fill
                    });
                }
                TotalCost += supplyModule.TotalCost;
            }

            NormalizedTotalCost = TotalCost / FloorArea;
        }

        public double NormalizedTotalCost
        {
            get => _normalizedTotalCost;
            set
            {
                _normalizedTotalCost = value;
                OnPropertyChanged();
            }
        }

        private double FloorArea
        {
            get { return UmiContext.Current.Buildings.All.Select(b => b.GrossFloorArea).Sum().Value; }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}