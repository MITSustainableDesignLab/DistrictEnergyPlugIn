using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class Exportable : AbstractDistrictLoad
    {
        public abstract double F { get; set; }
        public abstract double V { get; set; }
        public GraphCost FixedCost => new FixedCost(this);
        public GraphCost VariableCost => new VariableCost(this, 200);
        public double TotalCost => FixedCost.Cost + VariableCost.Cost;
    }
}