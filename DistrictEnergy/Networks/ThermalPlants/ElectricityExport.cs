using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ElectricityExport : Dispatchable
    {
        public ElectricityExport()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>
            {
                {LoadTypes.GridElec, 1},
                {LoadTypes.Elec, -1}
            };
        }

        public override Guid Id { get; set; }
        public override LoadTypes OutputType => LoadTypes.GridElec;
        public override LoadTypes InputType => LoadTypes.Elec;
        public override double CapacityFactor => 1;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; } = 0;
        public override double V { get; set; } = 0.15;
        public override double Efficiency => ConversionMatrix[OutputType];

        public override SolidColorBrush Fill { get; set; } =
            new SolidColorBrush((Color) ColorConverter.ConvertFromString("#FF6161"));

        public override double Capacity => Output.Max();
        public override string Name { get; set; } = "Electricity Export";
    }
}