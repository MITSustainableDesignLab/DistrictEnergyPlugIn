using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class HotWaterStorage : Storage
    {
        public HotWaterStorage()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, 1}
            };
        }

        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double AUT_HWT { get; set; } = 0;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_HWT { get; set; } = 0.15;

        /// <summary>
        ///     The Tank charged state at the beginning of the simulation. Assumed at 0 %.
        /// </summary>
        [DataMember]
        [DefaultValue(0.8)]
        public double TANK_START { get; set; } = 0.0;

        [DataMember] [DefaultValue(0)] public override double F { get; set; } = 0;
        [DataMember] [DefaultValue(0.167)] public override double V { get; set; } = 0.167;

        [DataMember]
        [DefaultValue("Thermal Energy Storage")]
        public override string Name { get; set; } = "Thermal Energy Storage";
        public override LoadTypes OutputType { get; } = LoadTypes.Heating;
        public override LoadTypes InputType { get; } = LoadTypes.Heating;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(253, 199, 204));
        [JsonIgnore] public override double ChargingEfficiency => 1 - LOSS_HWT;
        [JsonIgnore] public override double DischargingEfficiency => 1 - LOSS_HWT;
        [JsonIgnore] public override double MaxChargingRate => Capacity > 0 ? Capacity / AUT_HWT : 0;
        [JsonIgnore] public override double MaxDischargingRate => Capacity > 0 ? Capacity / AUT_HWT : 0;
        [JsonIgnore] public override double StartingCapacity => Capacity * TANK_START;
        [JsonIgnore] public override double Capacity => CalcCapacity();
        private double CalcCapacity()
        {
            return DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Heating).Select(v => v.Input.Average()).Sum() * AUT_HWT * 24;
        }
    }
}