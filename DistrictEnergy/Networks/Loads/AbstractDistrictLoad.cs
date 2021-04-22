using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using Rhino;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.Loads
{
    public abstract class AbstractDistrictLoad
    {
        public abstract Guid Id { get; set; }
        public abstract LoadTypes LoadType { get; set; }
        public abstract double[] Input { get; set; }
        public abstract SolidColorBrush Fill { get; set; }
        public abstract string Name { get; set; }
        public string Path { get; set; }
        public abstract void GetUmiLoads(List<UmiObject> contextObjects, UmiContext umiContext);

        public static List<UmiObject> ContextBuildings(UmiContext umiContext)
        {
            var idList = umiContext.Buildings.All.Select(building => building.Id.ToString()).ToList();
            // Getting the Aggregated Load Curve for all buildings
            var contextBuildings =
                umiContext.GetObjects()
                    .Where(o => o.Data.Any(x => x.Value.Data.Count == 8760) && idList.Contains(o.Id)).ToList();
            var buttons = MessageBoxButton.YesNo;
            if (contextBuildings.Count == 0)
            {
                var result = MessageBox.Show(
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