using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.PlantSettings
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

        private void btnAddCustomChilledWaterModule_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ACustomModuleView newBtn = new ACustomModuleView(LoadTypes.Cooling);
            stackCustomCW.Children.Add(newBtn);
        }
    }
}