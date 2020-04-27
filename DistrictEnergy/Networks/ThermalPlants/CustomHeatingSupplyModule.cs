using System.Collections.Generic;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomHeatingSupplyModule : CustomEnergySupplyModule
    {
        public CustomHeatingSupplyModule()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, 1}
            };
        }

        public override LoadTypes OutputType { get; set; } = LoadTypes.Heating;
        public override LoadTypes InputType { get; set; } = LoadTypes.Heating;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; }
        public override double V { get; set; }
        public override double Capacity { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
    }
}