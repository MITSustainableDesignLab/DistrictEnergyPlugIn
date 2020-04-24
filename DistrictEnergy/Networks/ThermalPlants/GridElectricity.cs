using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridElectricity : IThermalPlantSettings
    {
        public GridElectricity()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, 1}
            };
            Efficiency = ConversionMatrix[LoadType];
        }
        [DataMember] [DefaultValue(0)] public double F { get; set; } = 0;
        [DataMember] [DefaultValue(0.15)] public double V { get; set; } = 0.15;
        public double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("Grid Electricity")] public string Name { get; set; } = "Grid Electricity";
        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Output { get; set; }
        public double Efficiency { get; set; }
    }
}