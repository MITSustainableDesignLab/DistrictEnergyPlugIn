using Mit.Umi.RhinoServices;
using Rhino;
using System.Windows;
using System.Windows.Controls;

namespace DistrictEnergy
{
    /// <summary>
    /// Interaction logic for ModuleControl.xaml
    /// </summary>
    public partial class DistrictControl : UserControl
    {
        public DistrictControl()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Page_Loaded);
            this.DataContext = this;
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Settingsdata.DataContext = DistrictEnergyPlugIn.activeSettings;
        }
      
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("CreateNetworkLayer", echo: true);
        }

        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("RunTrnsysCommand", echo: true);
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void radioButton2_Copy_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void button_Click_2(object sender, RoutedEventArgs e)
        {
            GetScenario(radioButton1, @"C:\UMI\temp\KuwaitGroup02_v22-sysInputs.py");
            GetScenario(radioButton2, @"C:\UMI\temp\sample1.py");
            GetScenario(radioButton3, @"C:\UMI\temp\sample1.py");
            GetScenario(radioButton4, @"C:\UMI\temp\sample1.py");
        }

        private void GetScenario(RadioButton rdoButton, string fileName)
        {
            if (rdoButton.IsChecked == true)
            {
                //Create weatherfile csv
                //DryBulbCSV();

                //Run Python.exe
                string args = " " + ElectricityGenerationCost.Text + " " + PriceNaturalGas.Text + " " + EmissionsElectricGeneration.Text + " " + LossesTransmission.Text + " " + LossesHeatHydronic.Text + " " + EfficPowerGen.Text;
                //cmd.runcmd(fileName, args);
            }
        }


        private void Cost_Electricity_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            //GlobalContext.StoreSettings("Cost_Electricity", Cost_Electricity);
        }

        private void Price_NaturalGas_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Emissions_ElectricGeneration_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Transmission_Losses_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Hydronic_Losses_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Effic_PowerGen_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void radioButton1_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CostElectricity_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ElectricityGenerationCost
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (button1.Content.ToString() == "Show Topology")
            {
                button1.Content = "Hide Topology";
            }
            else
            {
                button1.Content = "Show Topology";
            }
            RhinoApp.RunScript("ToggleShowNetworkTopology", echo: true);
        }
    }
}
