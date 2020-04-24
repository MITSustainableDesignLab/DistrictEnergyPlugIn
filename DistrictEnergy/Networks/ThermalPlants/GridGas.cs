using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridGas : IThermalPlantSettings
    {
        public GridGas()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Gas, 1}
            };
        }
        [DataMember] [DefaultValue(0)] public double F { get; set; } = 0;
        [DataMember] [DefaultValue(0.05)] public double V { get; set; } = 0.05;
        public double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Grid Natural Gas")]
        public string Name { get; set; } = "Grid Natural Gas";

        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Gas;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
    }
}