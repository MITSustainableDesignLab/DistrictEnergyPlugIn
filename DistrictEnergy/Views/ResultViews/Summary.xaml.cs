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
            foreach (var plant in DistrictControl.Instance.ListOfDistrictLoads)
            {
                TextBlock name = new TextBlock();
                name.Text = plant.Name;
                NameStack.Children.Add(name);

                TextBlock demandValue = new TextBlock();
                demandValue.Text = plant.Input.Max().ToString("N2");
                PeakStack.Children.Add(demandValue);

                TextBlock energyValue = new TextBlock();
                energyValue.Text = plant.Input.Sum().ToString("N2");
                EnergyStack.Children.Add(energyValue);

            }
        }
    }
}