using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    ///     Interaction logic for CombinedHeatAndPowerView.xaml
    /// </summary>
    public partial class CombinedHeatAndPowerView : UserControl
    {
        public CombinedHeatAndPowerView()
        {
            InitializeComponent();
            DataContext = new CombinedHeatAndPowerViewModel();
        }
    }
}