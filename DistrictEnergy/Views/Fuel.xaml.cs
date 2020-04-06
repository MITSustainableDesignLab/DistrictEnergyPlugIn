using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views
{
    /// <summary>
    /// Interaction logic for Fuel.xaml
    /// </summary>
    public partial class Fuel : UserControl
    {
        public Fuel()
        {
            InitializeComponent();
            DataContext = new FuelViewModel();
        }
    }
}