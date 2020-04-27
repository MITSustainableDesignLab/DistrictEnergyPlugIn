﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ElectricChiller : IDispatchable
    {
        public ElectricChiller()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Cooling, CCOP_ECH},
                {LoadTypes.Elec, -1}
            };
        }

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(4.40)]
        public double CCOP_ECH { get; set; } = 4.40;

        [DataMember] [DefaultValue(164.1)] public double F { get; set; } = 164.1;
        [DataMember] [DefaultValue(0)] public double V { get; set; }
        public double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("Chiller")] public string Name { get; set; } = "Chiller";
        public Guid Id { get; set; }
        public LoadTypes OutputType { get; set; } = LoadTypes.Cooling;
        public LoadTypes InputType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Input { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[OutputType];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(93, 153, 170));
    }
}