using System;
using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;

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
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings)
            {
                TextBlock name = new TextBlock();
                name.Text = plant.Name;
                NamePlantStack.Children.Add(name);
                Grid.SetColumn(name, 2);

                TextBlock demandValue = new TextBlock();
                demandValue.Text = plant.Input.Max().ToString("N2");
                PeakPlantStack.Children.Add(demandValue);
                Grid.SetColumn(name, 2);

                TextBlock energyValue = new TextBlock();
                energyValue.Text = plant.Input.Sum().ToString("N2");
                EnergyPlantStack.Children.Add(energyValue);
                Grid.SetColumn(name, 2);
            }
        }
    }
}