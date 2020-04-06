using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using DistrictEnergy.ViewModels;
using LiveCharts;
using LiveCharts.Helpers;
using LiveCharts.Wpf;

namespace DistrictEnergy.Views
{
    /// <summary>
    /// Interaction logic for Loads.xaml
    /// </summary>
    public partial class Loads : UserControl
    {
        public Loads()
        {
            InitializeComponent();

            DataContext = new LoadsViewModel();
        }

        private void CartesianChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var chart = (LiveCharts.Wpf.CartesianChart) sender;
            var vm = (LoadsViewModel) DataContext;

            //lets get where the mouse is at our chart
            var mouseCoordinate = e.GetPosition(chart);

            //now that we know where the mouse is, lets use
            //ConverToChartValues extension
            //it takes a point in pixes and scales it to our chart current scale/values
            var p = chart.ConvertToChartValues(mouseCoordinate);

            //in the Y section, lets use the raw value
            vm.YPointer = p.Y;

            //for X in this case we will only highlight the closest point.
            //lets use the already defined ClosestPointTo extension
            //it will return the closest ChartPoint to a value according to an axis.
            //here we get the closest point to p.X according to the X axis
            if (chart.Series.Count > 0)
            {
                var series = chart.Series[0];
                var closetsPoint = series.ClosestPointTo(p.X, AxisOrientation.X);

                vm.XPointer = closetsPoint.X;
            }
        }
    }
}