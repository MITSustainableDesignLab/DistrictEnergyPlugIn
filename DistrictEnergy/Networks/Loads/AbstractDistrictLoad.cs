using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using CsvHelper;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Rhino;
using Umi.Core;
using Umi.RhinoServices.Context;
using MessageBox = System.Windows.MessageBox;

namespace DistrictEnergy.Networks.Loads
{
    public abstract class AbstractDistrictLoad
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public abstract List<DateTimePoint> Input { get; set; }
        public abstract LoadTypes LoadType { get; set; }
        public abstract SolidColorBrush Fill { get; set; }
        public abstract string Name { get; set; }
        public string Path { get; set; }
        public abstract void GetUmiLoads(List<UmiObject> contextObjects);

        public static List<UmiObject> ContextBuildings(UmiContext umiContext)
        {
            var _idList = new List<string>();
            foreach (var building in umiContext.Buildings.All) _idList.Add(building.Id.ToString());
            // Getting the Aggregated Load Curve for all buildings
            var contextBuildings =
                umiContext.GetObjects()
                    .Where(o => o.Data.Any(x => x.Value.Data.Count == 8760) && _idList.Contains(o.Id)).ToList();
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            if (contextBuildings.Count == 0)
            {
                MessageBoxResult result;
                result = MessageBox.Show(
                    "There are no buildings with hourly results. Would you like to run an hourly energy simulation now?",
                    "Cannot continue with District simulation", buttons);
                if (result == MessageBoxResult.Yes)
                {
                    // Sets hourly results true and calls UMISimulateEnergy
                    umiContext.ProjectSettings.GenerateHourlyEnergyResults = true;
                    RhinoApp.RunScript("-UmiSimulateEnergy", true);
                }
            }

            return contextBuildings;
        }
    }
}