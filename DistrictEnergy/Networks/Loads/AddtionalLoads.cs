using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using CsvHelper;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts.Defaults;
using Rhino;
using Rhino.UI;
using Umi.Core;
using Umi.RhinoServices.Context;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace DistrictEnergy.Networks.Loads
{
    class AddtionalLoads : DistrictLoad
    {
        public AddtionalLoads(LoadTypes loadType)
        {
            LoadType = loadType;
        }

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

                    Input = LoadCustomDemand(filePath, context).AggregateByPeriod(1);
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

        public override List<DateTimePoint> Input { get; set; }
        public override LoadTypes LoadType { get; set; }
        public override SolidColorBrush Fill { get; set; }
        public override string Name { get; set; }

        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            throw new NotImplementedException();
        }

        public String Path { get; set; }
    }
}