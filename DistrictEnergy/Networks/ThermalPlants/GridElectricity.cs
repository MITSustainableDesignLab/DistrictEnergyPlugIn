using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridElectricity : IThermalPlantSettings
    {

        [DataMember] [DefaultValue(0)] public double F { get; set; } = 0;
        [DataMember] [DefaultValue(0.15)] public double V { get; set; } = 0.15;
        public double Capacity { get; set; } = double.PositiveInfinity;
        public string Name { get; set; } = "Grid Electricity";
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}