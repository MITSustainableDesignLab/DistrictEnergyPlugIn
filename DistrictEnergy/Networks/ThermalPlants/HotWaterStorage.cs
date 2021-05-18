using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class HotWaterStorage : Storage
    {
        public HotWaterStorage()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>
            {
                {OutputType, 1}
            };
        }

        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double AUT_HWT { get; set; }

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
        public double TANK_START { get; set; }

        [DataMember] [DefaultValue(0)] public override double F { get; set; }
        [DataMember] [DefaultValue(0.167)] public override double V { get; set; } = 0.167;

        [DataMember]
        [DefaultValue("Thermal Energy Storage")]
        public override string Name { get; set; } = "Thermal Energy Storage";

        public override LoadTypes OutputType { get; } = LoadTypes.Heating;
        public override LoadTypes InputType { get; } = LoadTypes.Heating;

        public override double CapacityFactor
        {
            get => AUT_HWT;
            set => HotWaterViewModel.Instance.AUT_HWT = value;
        }

        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromArgb(200, 253, 100, 30))}
                };
            set => throw new NotImplementedException();
        }

        [JsonIgnore] public override double ChargingEfficiency => 1 - LOSS_HWT;
        [JsonIgnore] public override double DischargingEfficiency => 1 - LOSS_HWT;
        [JsonIgnore] public override double MaxChargingRate => Capacity > 0 ? Capacity / AUT_HWT : 0;
        [JsonIgnore] public override double MaxDischargingRate => Capacity > 0 ? Capacity / AUT_HWT : 0;
        [JsonIgnore] public override double StartingCapacity => TANK_START;
        [JsonIgnore] public override double Capacity { get; set; }
    }
}