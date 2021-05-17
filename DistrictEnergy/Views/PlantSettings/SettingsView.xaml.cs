using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new PlanningSettingsViewModel();
            UmiEventSource.Instance.ProjectOpened += LoadThis;
            UmiEventSource.Instance.ProjectClosed += ClearThis;
        }

        private void LoadThis(object sender, UmiContext e)
        {
            PlantSettingsViewModel.Instance.PropertyChanged += LoadThis;
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
        private void LoadThis(object sender, PropertyChangedEventArgs e)
        {
            var exportables = DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>();
            foreach (var export in exportables)
            {
                if (Exports.Children.Count < exportables.Count())
                {
                    Exports.Children.Add(new ExportView(export));
                }
            }
        }
    }
}