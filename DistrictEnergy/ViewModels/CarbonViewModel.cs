using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class CarbonViewModel : INotifyPropertyChanged
    {
        private double _totalCarbon;
        private double _normalizedTotalCarbon;

        public CarbonViewModel()
        {
            SeriesCollection = new SeriesCollection();
            Labels = new[] {"Natural Gas", "Grid Electricity"};
            Formatter = value => value.ToString("N");
            TotalCarbon = 0;

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;
        }

        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public double TotalCarbon
        {
            get => _totalCarbon;
            set
            {
                _totalCarbon = value;
                OnPropertyChanged();
            }
        }

        public double NormalizedTotalCarbon
        {
            get => _normalizedTotalCarbon;
            set
            {
                _normalizedTotalCarbon = value;
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
            DHRunLPModel.Instance.Completion += UpdateCarbonChart;
        }

        private void UpdateCarbonChart(object sender, EventArgs e)
        {

            SeriesCollection.Clear();

            var gasCarbon = DistrictControl.Instance.ListOfPlantSettings.OfType<GridGas>().Select(o=>o.Input.Select(x=>x.Value).Sum()).Sum() * UmiContext.Current.ProjectSettings.GasCarbon;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Natural Gas",
                Values = new ChartValues<double>
                {
                    gasCarbon
                },
                Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            });
            var electricityCarbon = DistrictControl.Instance.ListOfPlantSettings.OfType<GridElectricity>().Select(o => o.Input.Select(x => x.Value).Sum()).Sum() * UmiContext.Current.ProjectSettings.ElectricityCarbon;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Grid Electricity",
                Values = new ChartValues<double>
                {
                    electricityCarbon
                },
                Fill = new SolidColorBrush(Color.FromRgb(189, 133, 74)),
            });
            TotalCarbon = electricityCarbon + gasCarbon;
            NormalizedTotalCarbon = TotalCarbon / FloorArea;
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