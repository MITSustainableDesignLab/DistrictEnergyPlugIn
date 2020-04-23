using System.Windows.Controls;
using DistrictEnergy.Helpers;
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

        private void btnAddCustomHotWaterModule_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ACustomModuleView newBtn = new ACustomModuleView(LoadTypes.Heating);
            stackCustomCW.Children.Add(newBtn);
        }
    }
}