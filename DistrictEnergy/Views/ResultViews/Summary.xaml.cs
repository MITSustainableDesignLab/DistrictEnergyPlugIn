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
    public partial class Summary : UserControl
    {
        public Summary()
        {
            InitializeComponent();

            DataContext = new SummaryViewModel();

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
                demandValue.Text = load.Input.Max().ToString("N2");
                PeakStack.Children.Add(demandValue);

                TextBlock energyValue = new TextBlock();
                energyValue.Text = load.Input.Sum().ToString("N2");
                EnergyStack.Children.Add(energyValue);

            }

            NamePlantStack.Children.Clear();
            PeakPlantStack.Children.Clear();
            EnergyPlantStack.Children.Clear();

            // Plot Plant Supply & Demand
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>().Where(x =>
                x.OutputType == LoadTypes.Cooling ||
                x.OutputType == LoadTypes.Heating ||
                x.OutputType == LoadTypes.Elec))
            {
                foreach (var cMat in plant.ConversionMatrix)
                {
                    var loadType = cMat.Key;
                    var eff = cMat.Value;
                    if (plant.Input.Sum() > 0)
                    {
                        TextBlock name = new TextBlock();
                        name.Text = $"[{loadType}] {plant.Name}";
                        NamePlantStack.Children.Add(name);
                        Grid.SetColumn(name, 2);

                        TextBlock demandValue = new TextBlock();
                        demandValue.Text = plant.Input.Select(v =>v.Value * eff).Max().ToString("N2");
                        PeakPlantStack.Children.Add(demandValue);
                        Grid.SetColumn(name, 2);

                        TextBlock energyValue = new TextBlock();
                        energyValue.Text = plant.Input.Select(v => v.Value * eff).Sum().ToString("N2");
                        EnergyPlantStack.Children.Add(energyValue);
                        Grid.SetColumn(name, 2);
                    }
                }
            }
        }
    }
}