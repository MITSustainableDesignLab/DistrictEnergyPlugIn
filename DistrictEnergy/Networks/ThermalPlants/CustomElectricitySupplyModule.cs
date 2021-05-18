using System.Collections.Generic;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomElectricitySupplyModule : CustomEnergySupplyModule
    {
        public override LoadTypes OutputType => LoadTypes.Elec;
        public override double OFF_Custom { get; set; } = 1;
        public override LoadTypes InputType => LoadTypes.Custom;

        public override double CapacityFactor
        {
            get => OFF_Custom;
            set => OFF_Custom = value;
        }

        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {OutputType, 1},
            {InputType, -1}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; }
        public override double V { get; set; }

        public override double Efficiency => ConversionMatrix[OutputType];

        public override double Capacity
        {
            get => Output.Max();
            set { }
        }

        public override double CarbonIntensity { get; set; }
    }
}