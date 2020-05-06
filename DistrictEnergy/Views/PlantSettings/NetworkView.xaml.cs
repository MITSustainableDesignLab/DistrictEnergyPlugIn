using System;
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
            DataContext = new PlanningSettingsViewModel();
            UmiEventSource.Instance.ProjectOpened += LoadThis;
            UmiEventSource.Instance.ProjectClosed += ClearThis;
        }

        /// <summary>
        /// Clear the views in the StackPanel named "Exports"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearThis(object sender, EventArgs e)
        {
            Exports.Children.Clear();
        }

        /// <summary>
        /// Create the views in the StackPanel named "Exports"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadThis(object sender, UmiContext e)
        {
            foreach (var export in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                Exports.Children.Add(new ExportView(export));
            }
        }
    }
}