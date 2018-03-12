using Rhino;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System;
using DistrictEnergy.ViewModels;

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
            GetScenario(radioButton1, 1);
            GetScenario(radioButton2, 2);
            GetScenario(radioButton3, 3);
            GetScenario(radioButton4, 4);
        }

        private void GetScenario(RadioButton rdoButton, int scenario)
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
                string args = scenario + " " + a + " " + b + " " + c + " " + d + " " + e + " " + f;
                runcmd(args);
            }
        }

        private void runcmd(string args)
        {
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(@"C:\UMI\temp\DEenginePython\DEenginePython.exe")
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
            RhinoApp.SetCommandPrompt(e.Data);
        }

        private void Cost_Electricity_TextChanged(object sender, TextChangedEventArgs e)
        {

            //UmiContext.Current.StoreSettings("Cost_Electricity", Cost_Electricity);
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

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            string value = @"%SystemDrive%\UMI\temp\DHSimulationResults";
            string path = Environment.ExpandEnvironmentVariables(value);
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("This location does not exist yet since no simulation has been performed\r\nRun a scenario first : {0}, {1}",ex.GetType().Name, ex.Message);
            }
        }

        private void ButtonRunSimulation(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("DHSimulateDistrictEnergy", true);
        }
    }

    public class DoubleRangeRule : ValidationRule
    {
        public double Min { get; set; }

        public double Max { get; set; }

        public override ValidationResult Validate(object value,
            CultureInfo cultureInfo)
        {
            double parameter = 0;

            try
            {
                if (((string) value).Length > 0) parameter = double.Parse((string) value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or "
                                                   + e.Message);
            }
#if (DEBUG == true)
            if (parameter < Min || parameter > Max)
            {
                RhinoApp.WriteLine(string.Format("Please enter value in the range: " + Min + " - " + Max + "."));
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + Min + " - " + Max + ".");
            }
#endif
            if (parameter < Min || parameter > Max)
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + Min + " - " + Max + ".");
            return new ValidationResult(true, null);
        }
    }

    public class InverseAndBooleansToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.LongLength > 0)
                foreach (var value in values)
                    if (value is bool && (bool) value)
                        return false;
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}