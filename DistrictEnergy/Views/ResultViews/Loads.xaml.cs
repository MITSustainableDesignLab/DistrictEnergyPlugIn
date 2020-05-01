using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DistrictEnergy.ViewModels;
using LiveCharts;
using LiveCharts.Events;
using LiveCharts.Helpers;
using LiveCharts.Wpf;

namespace DistrictEnergy.Views.ResultViews
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

            DHRunLPModel.Instance.Completion += Window_Loaded;
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            GenerateLoadList();
        }

        /// <summary>
        /// Generates list of demands in the annual view.
        /// </summary>
        private void GenerateLoadList()
        {
            LoadList.Children.Clear();
            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads)
            {
                TextBlock name = new TextBlock();
                name.Text = load.Name;
                name.Foreground = load.Fill;
                name.FontSize = 10;

                if (load.Input.Sum() > 0)
                {
                    LoadList.Children.Add(name);
                }
            }
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

        private void Axis_OnRangeChanged(RangeChangedEventArgs eventargs)
        {
            var vm = (LoadsViewModel)DataContext;
            ResetButton.Visibility = Visibility.Visible;
            ScrollTip.Visibility = Visibility.Hidden;
            var currentRange = eventargs.Range;

            if (currentRange < TimeSpan.TicksPerDay * 2)
            {
                vm.TimeFormatter = x => new DateTime((long)x).ToString("t");
                return;
            }

            if (currentRange < TimeSpan.TicksPerDay * 60)
            {
                vm.TimeFormatter = x => new DateTime((long)x).ToString("dd MMM yy");
                return;
            }

            if (currentRange < TimeSpan.TicksPerDay * 540)
            {
                vm.TimeFormatter = x => new DateTime((long)x).ToString("MMM yy");
                return;
            }

            vm.TimeFormatter = x => new DateTime((long)x).ToString("yyyy");
        }

        /// <summary>
        /// Clicking the reset button, resets the from and to properties of the scrollbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = (LoadsViewModel)DataContext;
            vm.From = new DateTime(2018, 01, 01, 0, 0, 0).Ticks;
            vm.To = new DateTime(2018, 01, 01, 0, 0, 0).AddHours(8760).Ticks;
            ScrollTip.Visibility = Visibility.Visible;
            ResetButton.Visibility = Visibility.Hidden;
        }
    }
}