using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.ResultViews
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