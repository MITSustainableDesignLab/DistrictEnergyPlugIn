using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class HeatingExport : Dispatchable
    {
        public HeatingExport()
        {
        }

        public override Guid Id { get; set; }
        public override LoadTypes OutputType => LoadTypes.GridHeat;
        public override LoadTypes InputType => LoadTypes.Heating;
        public override double CapacityFactor => 1;
        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {LoadTypes.GridHeat, 1},
            {LoadTypes.Heating, -1}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; } = 0;
        public override double V { get; set; } = 0.15;
        public override double Efficiency => ConversionMatrix[OutputType];

        public override SolidColorBrush Fill { get; set; } =
            new SolidColorBrush((Color) ColorConverter.ConvertFromString("#00DACD"));

        public override double Capacity => Double.PositiveInfinity;
        public override string Name { get; set; } = "Heating Export";
    }
}