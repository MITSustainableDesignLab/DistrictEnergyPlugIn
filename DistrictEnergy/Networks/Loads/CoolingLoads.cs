using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Rhino;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.Loads
{
    class CoolingLoads : AbstractDistrictLoad, IBaseLoad
    {
        public CoolingLoads()
        {

        }

        public override double[] Input { get; set; }
        public override LoadTypes LoadType { get; set; } = LoadTypes.Cooling;
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(0, 140, 218));
        public override string Name { get; set; } = "Cooling Load";

        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating cooling loads");
            var nbDataPoint = 8760;
            var aggregationArray = new double[nbDataPoint];
            foreach (var umiObject in contextObjects)
            {
                var objectCop = UmiContext.Current.Buildings.TryGet(Guid.Parse(umiObject.Id)).Template.Perimeter
                    .Conditioning.CoolingCoeffOfPerf;
                for (var i = 0; i < nbDataPoint; i++)
                {
                    // Cooling is multiplied by objectCop to transform into space cooling demand
                    var d = umiObject.Data["SDL/Cooling"].Data[i] * objectCop;
                    aggregationArray[i] += d;
                }
            }

            Input = aggregationArray;
        }
    }
}