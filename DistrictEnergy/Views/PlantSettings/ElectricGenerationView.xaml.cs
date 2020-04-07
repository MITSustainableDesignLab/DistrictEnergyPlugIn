using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views
{
    /// <summary>
    ///     Interaction logic for ElectricGenerationView.xaml
    /// </summary>
    public partial class ElectricGenerationView : UserControl
    {
        public ElectricGenerationView()
        {
            InitializeComponent();
            DataContext = new ElectricGenerationViewModel();
        }
    }
}