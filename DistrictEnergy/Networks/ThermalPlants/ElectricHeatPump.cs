using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class ElectricHeatPump : IDispatchable
    {
        public ElectricHeatPump()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, HCOP_EHP},
                {LoadTypes.Cooling, (1-1/HCOP_EHP)},
                {LoadTypes.Elec, -1}
            };
        }

        /// <summary>
        ///     Capacity as percent of peak heating load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_EHP { get; set; } = 0;

        /// <summary>
        ///     Heating coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(3.2)]
        public double HCOP_EHP { get; set; } = 3.2;

        /// <summary>
        ///     Should the evaporator side be used as a cold source?
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public int UseEhpEvap { get; set; } = 0;

        [DataMember] [DefaultValue(1660)] public double F { get; set; } = 1660;
        [DataMember] [DefaultValue(0.00332)] public double V { get; set; } = 0.00332;
        public double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Heat Pump")]
        public string Name { get; set; } = "Heat Pump";

        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes OutputType { get; set; } = LoadTypes.Heating;
        public LoadTypes InputType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Input { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[OutputType];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(0, 140, 218));
    }
}