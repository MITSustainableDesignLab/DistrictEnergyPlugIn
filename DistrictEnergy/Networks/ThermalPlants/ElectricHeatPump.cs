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
    public class ElectricHeatPump : Dispatchable
    {
        public ElectricHeatPump()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, HCOP_EHP},
                {LoadTypes.Cooling, (1-1/HCOP_EHP)}, // TODO Add Switch for Use Evaporator or Not
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

        [DataMember] [DefaultValue(1660)] public override double F { get; set; } = 1660;
        [DataMember] [DefaultValue(0.00332)] public override double V { get; set; } = 0.00332;
        public override double Capacity => CalcCapacity();
        private double CalcCapacity()
        {
            if (DistrictControl.Instance is null) return 0;
            return OFF_EHP * DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Heating).Select(v => v.Input.Max()).Sum();
        }

        [DataMember]
        [DefaultValue("Heat Pump")]
        public override string Name { get; set; } = "Heat Pump";

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Heating;
        public override LoadTypes InputType => LoadTypes.Elec;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(0, 140, 218));
    }
}