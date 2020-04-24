using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Windows.Media;
using CsvHelper;
using DistrictEnergy.Helpers;
using Rhino;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomEnergySupplyModule : IThermalPlantSettings
    {
        /// <summary>
        /// Path of the CSV File
        /// </summary>
        public String Path { get; set; }

        /// <summary>
        /// Hourly Data Array
        /// </summary>
        public double[] Data { get; set; }

        /// <summary>
        /// Unique identifier 
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name of the Custom Energy Supply Module
        /// </summary>
        [DataMember]
        [DefaultValue("Unnamed")]
        public string Name { get; set; } = "Unnamed";

        public void LoadCsv()
        {
            RhinoApp.WriteLine("Loading CSV file...");


            var context = UmiContext.Current;
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = RhinoDoc.ActiveDoc.Path;
                openFileDialog.Filter = "CSV Files(*.csv)|*.csv";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file and assign to class instance
                    filePath = openFileDialog.FileName;
                    Path = filePath;
                    //Read the contents of the file into the umi db

                    Data = LoadCustomDemand(filePath, context);
                    RhinoApp.WriteLine($"Added additional load from '{filePath}'");
                }
            }
        }

        /// <summary>
        ///     Reads a CSV file with 3 columns.
        /// </summary>
        /// <param name="filePath">The path to the csv file</param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static double[] LoadCustomDemand(string filePath, UmiContext context)
        {
            // Start stream reader
            double[] records;
            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.HasHeaderRecord = false;
                    records = csv.GetRecords<double>().ToArray();
                }
            }

            return records;
        }

        public double F { get; set; }
        public double V { get; set; }
        public double Capacity { get; set; } = double.PositiveInfinity;
        public double Norm { get; set; } = 1;

        public SolidColorBrush Fill => new SolidColorBrush(Color);

        public Color Color { get; set; } = Color.FromRgb(200, 1, 0);
        public LoadTypes LoadType { get; set; }

        public double ComputeHeatBalance(double demand, double chiller, double solar, int i)
        {
            var custom = Data[i];
            var excess = Math.Max((chiller + solar + custom) - demand, 0);
            var balance = demand - (chiller + solar + custom - excess);
            return balance;
        }
    }
}