using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.ViewModels;
using Rhino;

namespace DistrictEnergy.Views.DistrictSettings
{
    /// <summary>
    /// Interaction logic for AnAdditionalLoadsView.xaml
    /// </summary>
    public partial class AnAdditionalLoadsView : UserControl
    {
        private Guid _id;

        public AnAdditionalLoadsView(LoadTypes type)
        {
            _id = Guid.NewGuid();
            switch (type)
            {
                // Todo: case for each
                case LoadTypes.Cooling:
                    DistrictControl.Instance.ListOfDistrictLoads.Add(new AdditionalLoads(LoadTypes.Cooling)
                        { Id = _id, Name = "Additional Cooling Load" });
                    break;
                case LoadTypes.Elec:
                    DistrictControl.Instance.ListOfDistrictLoads.Add(new AdditionalLoads(LoadTypes.Elec)
                        { Id = _id, Name = "Additional Electricity Load" });
                    break;
                case LoadTypes.Heating:
                    DistrictControl.Instance.ListOfDistrictLoads.Add(new AdditionalLoads(LoadTypes.Heating)
                        { Id = _id, Name = "Additional Heating Load" });
                    break;
            }

            InitializeComponent();
            DataContext = new AnAdditionalLoadViewModel { Id = _id };
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
                ((Panel)this.Parent).Children.Remove(this);

                AdditionalLoads to_remove = DistrictControl.Instance.ListOfDistrictLoads.OfType<AdditionalLoads>().First(x => x.Id == _id);
                DistrictControl.Instance.ListOfDistrictLoads.Remove(to_remove);
                Rhino.RhinoApp.WriteLine($"Removed {nameof(AdditionalLoads)} named {to_remove.Name}");
            }
        }

        private void LoadCSV_Click(object sender, RoutedEventArgs e)
        {
            var load = DistrictControl.Instance.ListOfDistrictLoads.OfType<AdditionalLoads>().First(x => x.Id == _id);
            try
            {
                load.LoadCsv();
            }
            catch (Exception exception)
            {
                MessageBoxResult result = MessageBox.Show(exception.Message, "An Error occured while reading the CSV file");
                RhinoApp.WriteLine(exception.Message);
            }
        }
    }
}
