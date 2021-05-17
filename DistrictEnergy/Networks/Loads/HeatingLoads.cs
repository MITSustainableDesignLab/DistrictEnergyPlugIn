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
    internal class HeatingLoads : BaseLoad
    {
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; } = LoadTypes.Heating;
        public override SolidColorBrush Fill => new SolidColorBrush(Color.FromRgb(235, 45, 45));
        public override string Name { get; set; } = "Heating Load";

        /// <summary>
        ///     Heating + DHW
        /// </summary>
        public override double[] Input { get; set; }

        public override void GetUmiLoads(List<UmiObject> contextObjects, UmiContext umiContext)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating hot water loads");
            Input = contextObjects.Select(umiObject =>
                    umiObject.Data["SDL/Heating"].Data
                        .Zip(
                            umiObject.Data["SDL/Domestic Hot Water"].Data.Count == 0
                                ? new double[8760]
                                : umiObject.Data["SDL/Domestic Hot Water"].Data, (x, y) => x * umiContext
                                .Buildings.TryGet(Guid.Parse(umiObject.Id)).Template.Perimeter
                                .Conditioning.HeatingCoeffOfPerf + y))
                .Aggregate((sum, val) => sum.Zip(val, (a, b) => a + b)).ToArray();
        }
    }
}