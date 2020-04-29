using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.PlantSettings
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

        private void btnAddCustomElectricityModule_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ACustomModuleView newBtn = new ACustomModuleView(LoadTypes.Elec);
            stackCustomCW.Children.Add(newBtn);
        }
    }
}