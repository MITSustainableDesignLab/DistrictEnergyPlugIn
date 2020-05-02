﻿using System.Collections.Generic;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomHeatingSupplyModule : CustomEnergySupplyModule
    {
        public CustomHeatingSupplyModule()
        {
        }

        public override LoadTypes OutputType => LoadTypes.Heating;
        public override LoadTypes InputType => LoadTypes.Custom;
        public override double CapacityFactor => 1;
        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Heating, 1}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; }
        public override double V { get; set; }

        public override double Capacity => Output.Max();

        public override double Efficiency => ConversionMatrix[OutputType];
    }
}