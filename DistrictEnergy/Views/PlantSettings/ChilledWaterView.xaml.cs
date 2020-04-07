using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views
{
    /// <summary>
    ///     Interaction logic for AbsorptionChillerView.xaml
    /// </summary>
    public partial class AbsorptionChillerView : UserControl
    {
        public AbsorptionChillerView()
        {
            InitializeComponent();
            DataContext = new ChilledWaterViewModel();
        }
    }
}