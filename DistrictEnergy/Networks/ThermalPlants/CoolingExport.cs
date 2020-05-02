using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CoolingExport : Dispatchable
    {
        public CoolingExport()
        {
        }

        public override Guid Id { get; set; }
        public override LoadTypes OutputType => LoadTypes.GridCool;
        public override LoadTypes InputType => LoadTypes.Cooling;
        public override double CapacityFactor => 1;
        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {LoadTypes.GridCool, 1},
            {LoadTypes.Cooling, -1}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; } = 0;
        public override double V { get; set; } = 0.15;
        public override double Efficiency => ConversionMatrix[OutputType];

        public override SolidColorBrush Fill { get; set; } =
            new SolidColorBrush((Color) ColorConverter.ConvertFromString("#EA1515"));

        public override double Capacity => Double.PositiveInfinity;
        public override string Name { get; set; } = "Cooling Export";
    }
}