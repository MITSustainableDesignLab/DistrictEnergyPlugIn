using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using LiveCharts.Defaults;
using Umi.Core;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ElectricityExport : Exportable
    {
        public ElectricityExport()
        {
        }

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Elec;

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {InputType, 1}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>()
                {
                    {OutputType, new SolidColorBrush(Color.FromArgb(200, 0, 0, 0))}
                };
            set => throw new NotImplementedException();
        }

        public override LoadTypes InputType => LoadTypes.Elec;
        public override double F { get; set; } = 0;
        public override double V { get; set; } = 0.15;
        public override string Name { get; set; } = "Electricity Export";
    }
}