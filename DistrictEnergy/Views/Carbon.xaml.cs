using System;
using System.Windows.Controls;
using DistrictEnergy.ViewModels;
using LiveCharts;
using LiveCharts.Wpf;

namespace DistrictEnergy.Views
{
    /// <summary>
    /// Interaction logic for Carbon.xaml
    /// </summary>
    public partial class Carbon : UserControl
    {
        public Carbon()
        {
            InitializeComponent();

            DataContext = new CarbonViewModel();
        }
    }
}