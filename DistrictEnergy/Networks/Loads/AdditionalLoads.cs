﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using CsvHelper;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using Rhino;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.Loads
{
    internal class AdditionalLoads : BaseLoad
    {
        public AdditionalLoads(LoadTypes loadType)
        {
            LoadType = loadType;
        }

        public override double[] Input { get; set; }
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; }
        public override SolidColorBrush Fill => new SolidColorBrush(Color);
        public override string Name { get; set; }
        public Color Color { get; set; }

        public override void GetUmiLoads(List<UmiObject> contextObjects, UmiContext umiContext)
        {
        }

        public void LoadCsv()
        {
            RhinoApp.WriteLine("Loading CSV file...");


            var context = UmiContext.Current;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = RhinoDoc.ActiveDoc.Path;
                openFileDialog.Filter = "CSV Files(*.csv)|*.csv";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file and assign to class instance
                    var filePath = openFileDialog.FileName;
                    Path = filePath;
                    //Read the contents of the file into the umi db

                    Input = LoadCustomDemand(filePath, context);
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

        /// <summary>
        ///     Reads a CSV file with 3 columns.
        /// </summary>
        /// <param name="filePath">The path to the csv file</param>
        /// <param name="context"></param>
        /// <param name="refId"></param>
        /// <returns></returns>
        public static void AddAdditionalLoad(string filePath, UmiContext context, Guid refId)
        {
            // Start stream reader
            using (var reader = new StreamReader(filePath))
            {
                // Using CSV reader class
                using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                {
                    ICollection<IUmiObject> records = new Collection<IUmiObject>();
                    const string name = "Additional Loads";
                    var record = new UmiAdditionalLoad
                    {
                        Name = name, //Path.GetFileName(filePath),
                        Id = refId.ToString(),
                        Data = new Dictionary<string, UmiDataSeries>(),
                        FilePath = filePath
                    };


                    foreach (var type in DHLoadAdditionalProfile.Types)
                    {
                        var seriesName = $"Additional {type} Load";
                        record.Data[seriesName] = new UmiDataSeries
                        {
                            Name = seriesName,
                            Units = "kWh",
                            Resolution = "Hourly",
                            Data = new List<double>()
                        };
                    }

                    // Actual reading of the columns
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                        // Iterate over 3 columns
                        foreach (var type in DHLoadAdditionalProfile.Types)
                        {
                            var value = csv.GetField<double>(type);
                            record.Data[$"Additional {type} Load"].Data.Add(value);
                        }

                    records.Add(record);
                    context.StoreObjects(records);
                }
            }
        }
    }

    /// <summary>
    ///     Class used for additional loads. This class inherits the UmiObject class.
    /// </summary>
    public class UmiAdditionalLoad : IUmiObject
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public IDictionary<string, UmiDataSeries> Data { get; set; }
    }
}