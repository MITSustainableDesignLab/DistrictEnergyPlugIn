using Rhino;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System;
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
            GetScenario(radioButton1, @"C:\UMI\temp\SC01.py");
            GetScenario(radioButton2, @"C:\UMI\temp\SC02.py");
            GetScenario(radioButton3, @"C:\UMI\temp\SC03.py");
            GetScenario(radioButton4, @"C:\UMI\temp\SC04.py");
        }

        private void GetScenario(RadioButton rdoButton, string fileName)
        {
            if (rdoButton.IsChecked == true)
            {
                //Create Loads CSV File
                RhinoApp.RunScript("DHLoadstoCSV", echo: true);

                //Create weatherfile csv
                RhinoApp.RunScript("DHDryBulbCSV", echo: true);

                double a = Convert.ToDouble(ElectricityGenerationCost.Text);
                double b = Convert.ToDouble(PriceNaturalGas.Text);
                double c = Convert.ToDouble(EmissionsElectricGeneration.Text);
                double d = Convert.ToDouble(LossesTransmission.Text);
                double e = Convert.ToDouble(LossesHeatHydronic.Text);
                double f = Convert.ToDouble(EfficPowerGen.Text);

                //Run Python.exe
                string args = fileName + " " + a + " " + b + " " + c + " " + d + " " + e + " " + f;
                runcmd(fileName, args);
            }
        }

        private void runcmd(string fileName, string args)
        {
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(@"C:\Python27\python.exe")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = args,
                };
                p.ErrorDataReceived += cmd_Error;
                p.OutputDataReceived += cmd_DataReceived;
                p.EnableRaisingEvents = true;

                Rhino.Runtime.HostUtils.DisplayOleAlerts(false);
                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();
                Rhino.Runtime.HostUtils.DisplayOleAlerts(true);

            }
        }
        static void cmd_DataReceived(object sender, DataReceivedEventArgs e)
        {
            //RhinoApp.WriteLine("Output from other process");
            RhinoApp.WriteLine(e.Data);
        }

        static void cmd_Error(object sender, DataReceivedEventArgs e)
        {
            //RhinoApp.WriteLine("Error from other process");
            RhinoApp.WriteLine(e.Data);
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
