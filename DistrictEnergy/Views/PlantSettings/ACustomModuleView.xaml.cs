using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using Rhino;
using Rhino.DocObjects.Tables;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    /// Interaction logic for ACustomModuleView.xaml
    /// </summary>
    public partial class ACustomModuleView : UserControl
    {
        private static int _id;

        public ACustomModuleView()
        {
            _id = Interlocked.Increment(ref _id);
            DistrictControl.Instance.ListOfPlantSettings.Add(new CustomEnergySupplyModule() {Id = _id});
            InitializeComponent();
            DataContext = new ACustomModuleViewModel() {Id = _id};
        }

        /// <summary>
        /// Removes the CustomEnergySupplyModule model from the list of PlantSettings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                ((Panel) this.Parent).Children.Remove(this);

                CustomEnergySupplyModule to_remove = DistrictControl.Instance.ListOfPlantSettings
                    .OfType<CustomEnergySupplyModule>().First(x => x.Id == _id);
                DistrictControl.Instance.ListOfPlantSettings.Remove(to_remove);
                Rhino.RhinoApp.WriteLine($"Removed {nameof(CustomEnergySupplyModule)} named {to_remove.Name}");
            }
        }

        /// <summary>
        /// Loads in a CSV file for the CustomEnergySupplyModule
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCSV_Click(object sender, RoutedEventArgs e)
        {
            var plant = DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>()
                .First(x => x.Id == _id);
            plant.LoadCsv();
        }
    }
}