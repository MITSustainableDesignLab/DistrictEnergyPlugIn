using System.Windows;
using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;
using DistrictEnergy.Views.DistrictSettings;

namespace DistrictEnergy.Views.ResultViews
{
    /// <summary>
    /// Interaction logic for Demands.xaml
    /// </summary>
    public partial class Demands : UserControl
    {
        public Demands()
        {
            InitializeComponent();
            DataContext = new AnAdditionalLoadViewModel();
        }

        private void btnAddAdditionalLoad_Click(object sender, RoutedEventArgs e)
        {
            AnAdditionalLoadsView newBtn = new AnAdditionalLoadsView(LoadTypes.Cooling);
            ((Panel)stackCustomCW.Parent).Children.Add(newBtn);
        }

        private void btnAddAdditionalHeatingLoad_Click(object sender, RoutedEventArgs e)
        {
            AnAdditionalLoadsView newBtn = new AnAdditionalLoadsView(LoadTypes.Heating);
            ((Panel)stackCustomCW.Parent).Children.Add(newBtn);
        }

        private void btnAddAdditionalElecLoad_Click(object sender, RoutedEventArgs e)
        {
            AnAdditionalLoadsView newBtn = new AnAdditionalLoadsView(LoadTypes.Elec);
            ((Panel)stackCustomCW.Parent).Children.Add(newBtn);
        }
    }
}
