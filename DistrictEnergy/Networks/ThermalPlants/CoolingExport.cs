﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using LiveCharts.Defaults;
using Umi.Core;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CoolingExport : Exportable
    {
        public CoolingExport()
        {
        }

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; } = LoadTypes.Cooling;
        public override double[] Input { get; set; }
        public override double F { get; set; } = 0;
        public override double V { get; set; } = 0.15;

        public override SolidColorBrush Fill { get; set; } =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6161"));

        public override string Name { get; set; } = "Cooling Export";

        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            
        }
    }
}