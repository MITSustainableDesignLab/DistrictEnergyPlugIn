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
    internal class ElectricityLoads : BaseLoad
    {
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public override SolidColorBrush Fill => new SolidColorBrush(Color.FromRgb(173, 221, 67));
        public override string Name { get; set; } = "Electricity Load";

        /// <summary>
        ///     Equipment & Lighting
        /// </summary>
        public override double[] Input { get; set; }

        public override void GetUmiLoads(List<UmiObject> contextObjects, UmiContext umiContext)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating electrical loads: SDL/Equipment + SDL/Lighting");

            Input = contextObjects.Select(umiObject =>
                    (umiObject.Data["SDL/Equipment"].Data.Count == 0
                        ? new double[8760]
                        : umiObject.Data["SDL/Equipment"].Data)
                    .Zip(umiObject.Data["SDL/Lighting"].Data.Count == 0
                        ? new double[8760]
                        : umiObject.Data["SDL/Lighting"].Data, (x, y) => x + y))
                .Aggregate((sum, val) => sum.Zip(val, (a, b) => a + b)).ToArray();
        }
    }
}