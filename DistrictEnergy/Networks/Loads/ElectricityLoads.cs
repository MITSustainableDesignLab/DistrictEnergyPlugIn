using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Rhino;
using Umi.Core;

namespace DistrictEnergy.Networks.Loads
{
    class ElectricityLoads : AbstractDistrictLoad
    {
        public ElectricityLoads()
        {

        }

        public override LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(173, 221, 67));
        public override string Name { get; set; } = "Electricity Load";
        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating electrical loads: SDL/Equipment + SDL/Lighting");
            var nbDataPoint = 8760;
            var aggreagationArray = new double[nbDataPoint];
            foreach (var umiObject in contextObjects)
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Equipment"].Data[i] + umiObject.Data["SDL/Lighting"].Data[i];
                    aggreagationArray[i] += d;
                }

            Input = aggreagationArray.ToDateTimePoint();
        }
        /// <summary>
        /// Equipment & Lighting
        /// </summary>
        public override List<DateTimePoint> Input { get; set; }
    }
}