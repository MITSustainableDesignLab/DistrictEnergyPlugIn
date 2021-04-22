﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CoolingExport : Exportable
    {
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Cooling;

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {InputType, 1}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromArgb(100, 0, 0, 0))}
                };
            set => throw new NotImplementedException();
        }

        public override LoadTypes InputType => LoadTypes.Cooling;
        public override double F { get; set; } = 0;
        public override double V { get; set; } = 0.15;
        public override string Name { get; set; } = "Cooling Export";
    }
}