using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class WindInput : NotStorage
    {
        public abstract List<double> WindAvailableInput(int t = 0, int dt = 8760);
        public abstract double Power(int t = 0, int dt = 8760);
    }
}