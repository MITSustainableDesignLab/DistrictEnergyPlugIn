using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class PhotovoltaicArray : Dispatchable, ISolar
    {
        public PhotovoltaicArray()
        {
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_PV { get; set; } = 0;

        /// <summary>
        ///     Cell efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double EFF_PV { get; set; } = 0.15;

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.75)]
        public double UTIL_PV { get; set; } = 0.75;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_PV { get; set; } = 0.15;

        [DataMember] [DefaultValue(1313)] public override double F { get; set; } = 1313;
        [DataMember] [DefaultValue(0)] public override double V { get; set; }
        public override double Capacity => CalcCapacity();
        public override double CapacityFactor => OFF_PV;

        private double CalcCapacity()
        {
            if(DistrictControl.Instance is null) return 0;
            return OFF_PV * DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Elec).Select(v => v.Input.Sum()).Sum();
        }

        [DataMember] [DefaultValue("PV")] public override string Name { get; set; } = "PV";
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Elec;
        public override LoadTypes InputType => LoadTypes.SolarRadiation;
        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Elec, EFF_PV * UTIL_PV * (1 - LOSS_PV)}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(112, 159, 15));
        public double AvailableArea => Capacity / (SolarAvailableInput.Sum() * EFF_PV * (1 - LOSS_PV) * UTIL_PV);
        public double[] SolarAvailableInput => DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN;
    }
}