using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DistrictEnergy.Annotations;
using DistrictEnergy.ViewModels;
using LiveCharts;
using LiveCharts.Events;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using Rhino.UI;

namespace DistrictEnergy.Views.ResultViews
{
    /// <summary>
    /// Interaction logic for Loads.xaml
    /// </summary>
    public partial class Loads
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
            if (DHRunLPModel.Instance == null) return;
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

        private void BuildPngOnClick(object sender, RoutedEventArgs e)
        {

            var loadsGrid = LoadsGrid;
            loadsGrid.Measure(LoadsChart.RenderSize);
            loadsGrid.Arrange(new Rect(new Point(0, 0), LoadsChart.RenderSize));
            LoadsChart.Update(true, true); //force chart redraw
            loadsGrid.UpdateLayout();

            // Displays a SaveFileDialog so the user can save the Image
            // assigned to Button2.
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Save an Image File"
            };
            saveFileDialog1.ShowSaveDialog();
            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
                SaveToPng(LoadsChart, saveFileDialog1.FileName);
            //png file was created at the root directory.
        }

        private void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            EncodeVisual(visual, fileName, encoder);
        }

        private static void EncodeVisual(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);
            using (var stream = File.Create(fileName)) encoder.Save(stream);
        }
    }
}