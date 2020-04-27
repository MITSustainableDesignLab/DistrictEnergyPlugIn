﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class NatGasBoiler : IDispatchable
    {
        public NatGasBoiler()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, EFF_NGB},
                {LoadTypes.Gas, -1}
            };
        }
        /// <summary>
        ///     Heating efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.84)] public double EFF_NGB { get; set; } = 0.84; // (SLD) I thought 70% was quite low

        [DataMember] [DefaultValue(1360)] public double F { get; set; } = 1360;
        [DataMember] [DefaultValue(0)] public double V { get; set; }
        public double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("Natural Gas Boiler")] public string Name { get; set; } = "Natural Gas Boiler";
        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes OutputType { get; set; } = LoadTypes.Heating;
        public LoadTypes InputType { get; set; } = LoadTypes.Heating;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Input { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[OutputType];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(189, 133, 74));
    }
}