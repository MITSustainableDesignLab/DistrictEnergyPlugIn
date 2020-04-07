using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    /// Interaction logic for NetworkView.xaml
    /// </summary>
    public partial class NetworkView : UserControl
    {
        public NetworkView()
        {
            InitializeComponent();
            DataContext = new NetworkViewModel();
        }
    }
}
