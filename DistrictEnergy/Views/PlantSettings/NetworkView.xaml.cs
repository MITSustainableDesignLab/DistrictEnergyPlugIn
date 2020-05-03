using System.Linq;
using System.Windows.Controls;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

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
            UmiEventSource.Instance.ProjectOpened += LoadThis;
        }

        private void LoadThis(object sender, UmiContext e)
        {
            foreach (var export in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                Exports.Children.Add(new ExportView(export));
            }
        }
    }
}
