﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using LiveCharts;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class CarbonViewModel : INotifyPropertyChanged
    {
        private double _totalCarbon;

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
            SeriesCollection.Clear();

            var gasCarbon = instance.ResultsArray.NgasProj.Sum() * UmiContext.Current.ProjectSettings.GasCarbon;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Natural Gas",
                Values = new ChartValues<double>
                {
                    gasCarbon
                }
            });
            var electricityCarbon = instance.ResultsArray.ElecProj.Sum() *
                                    UmiContext.Current.ProjectSettings.ElectricityCarbon;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Grid Electricity",
                Values = new ChartValues<double>
                {
                    electricityCarbon
                }
            });
            TotalCarbon = electricityCarbon + gasCarbon;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}