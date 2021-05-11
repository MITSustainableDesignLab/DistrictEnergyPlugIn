using System;
using System.Linq;
using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using LiveCharts.Defaults;
using LiveCharts.Geared;

namespace DistrictEnergy.Views.ResultViews
{
    /// <summary>
    /// Interaction logic for Summary.xaml
    /// </summary>
    public partial class Summary
    {
        public Summary()
        {
            InitializeComponent();

            DataContext = this;

            DHRunLPModel.Instance.Completion += Window_Loaded;

        }
        public void Window_Loaded(object sender, EventArgs e)
        {
            GenerateControls();
        }
        public void GenerateControls()
        {
            NameStack.Children.Clear();
            PeakStack.Children.Clear();
            EnergyStack.Children.Clear();
            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads)
            {
                TextBlock name = new TextBlock();
                name.Text = load.Name;
                NameStack.Children.Add(name);

                TextBlock demandValue = new TextBlock();
                demandValue.Text = load.Input.Max().ToString("N0");
                PeakStack.Children.Add(demandValue);

                TextBlock energyValue = new TextBlock();
                energyValue.Text = load.Input.Sum().ToString("N0");
                EnergyStack.Children.Add(energyValue);

            }

            NamePlantStack.Children.Clear();
            PeakPlantStack.Children.Clear();
            EnergyPlantStack.Children.Clear();

            // Plot Plant Supply & Demand
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.Where(x =>
                x.OutputType == LoadTypes.Cooling ||
                x.OutputType == LoadTypes.Heating ||
                x.OutputType == LoadTypes.Elec))
            {
                foreach (var cMat in plant.ConversionMatrix)
                {
                    var loadType = cMat.Key;
                    if (plant.Input.Sum() > 0)
                    {
                        // Sources Name
                        TextBlock name = new TextBlock {Text = $"[{loadType}] {plant.Name}"};
                        NamePlantStack.Children.Add(name);
                        Grid.SetColumn(name, 2);

                        // Peak Column
                        TextBlock demandValue = new TextBlock {Text = plant.Capacity.ToString("N0")};
                        PeakPlantStack.Children.Add(demandValue);
                        Grid.SetColumn(name, 2);
                        
                        // Total Energy Column
                        TextBlock energyValue = new TextBlock {Text = (plant.Input.Sum() * cMat.Value).ToString("N0")};
                        EnergyPlantStack.Children.Add(energyValue);
                        Grid.SetColumn(name, 2);
                        
                    }
                }
            }
        }
    }
}