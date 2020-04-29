using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Rhino;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.Loads
{
    class HeatingLoads : AbstractDistrictLoad
    {
        public HeatingLoads()
        {

        }

        public override LoadTypes LoadType { get; set; } = LoadTypes.Heating;
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(235, 45, 45));
        public override string Name { get; set; } = "Heating Load";
        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating hot water loads");
            var nbDataPoint = 8760;
            var aggregationArray = new double[nbDataPoint];
            foreach (var umiObject in contextObjects)
            {
                var objectEff = UmiContext.Current.Buildings.TryGet(Guid.Parse(umiObject.Id)).Template.Perimeter
                    .Conditioning.HeatingCoeffOfPerf;
                for (var i = 0; i < nbDataPoint; i++)
                {
                    // Heating is multiplied by objectEff to transform into space heating demand
                    var d = umiObject.Data["SDL/Heating"].Data[i] * objectEff +
                            umiObject.Data["SDL/Domestic Hot Water"].Data[i];

                    aggregationArray[i] += d;
                }
            }

            Input = aggregationArray.ToDateTimePoint();
        }
        /// <summary>
        /// Heating + DHW
        /// </summary>
        public override List<DateTimePoint> Input { get; set; }
    }
}