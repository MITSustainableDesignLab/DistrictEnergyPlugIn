using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class CombinedHeatNPower : Dispatchable
    {
        public CombinedHeatNPower()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, EFF_CHP},
                {LoadTypes.Heating, HREC_CHP},
                {LoadTypes.Gas, -1},
            };
        }

        /// <summary>
        ///     Tracking Mode
        /// </summary>
        [DataMember]
        [DefaultValue(TrakingModeEnum.Thermal)]
        public TrakingModeEnum TMOD_CHP { get; set; } = TrakingModeEnum.Thermal;

        /// <summary>
        ///     Capacity as percent of peak electric load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_CHP { get; set; } = 0;

        /// <summary>
        ///     Electrical Efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.22)]
        public double EFF_CHP { get; set; } = 0.22;

        /// <summary>
        ///     Waste heat recovery effectiveness (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.29)]
        public double HREC_CHP { get; set; } = 0.29;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(1606)]
        public override double F { get; set; } = 1606;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0.010)]
        public override double V { get; set; } = 0.010;

        public override double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Combined Heat&Power")]
        public override string Name { get; set; } = "Combined Heat&Power";

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType { get; set; } = LoadTypes.Elec;
        public override LoadTypes InputType { get; set; } = LoadTypes.Gas;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(247, 96, 21));
    }

    [DataContract(Name = "TMOD_CHP")]
    public enum TrakingModeEnum
    {
        [EnumMember] Thermal,
        [EnumMember] Electrical
    }
}