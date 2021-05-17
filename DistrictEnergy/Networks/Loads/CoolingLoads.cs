using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using Rhino;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.Loads
{
    internal class CoolingLoads : BaseLoad
    {
        public override double[] Input { get; set; }
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; } = LoadTypes.Cooling;
        public override SolidColorBrush Fill => new SolidColorBrush(Color.FromRgb(0, 140, 218));
        public override string Name { get; set; } = "Cooling Load";

        public override void GetUmiLoads(List<UmiObject> contextObjects, UmiContext umiContext)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating cooling loads");

            Input = contextObjects.Select(umiObject =>
                    umiObject.Data["SDL/Cooling"].Data.Select(o => o * umiContext.Buildings
                        .TryGet(Guid.Parse(umiObject.Id))
                        .Template.Perimeter.Conditioning.CoolingCoeffOfPerf))
                .Aggregate((sum, val) => sum.Zip(val, (a, b) => a + b)).ToArray();
        }
    }
}