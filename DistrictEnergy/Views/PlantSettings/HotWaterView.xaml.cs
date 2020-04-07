using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    ///     Interaction logic for HotWaterView.xaml
    /// </summary>
    public partial class HotWaterView : UserControl
    {
        public HotWaterView()
        {
            InitializeComponent();
            DataContext = new HotWaterViewModel();
        }
    }
}