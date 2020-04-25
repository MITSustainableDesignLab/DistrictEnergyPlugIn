using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class HotWaterStorage : IStorage
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

        [DataMember] [DefaultValue(0)] public double F { get; set; } = 0;
        [DataMember] [DefaultValue(0.167)] public double V { get; set; } = 0.167;
        public double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Thermal Energy Storage")]
        public string Name { get; set; } = "Thermal Energy Storage";

        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Heating;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[LoadType];
        public double ChargingEfficiency => 1 - LOSS_HWT;
        public double DischargingEfficiency => 1 - LOSS_HWT;
        public double StorageStandingLosses => 0.01;
        public double[] Input { get; set; }
        public double[] Storage { get; set; }
        public double MaxChargingRate => Capacity > 0 ? Capacity / AUT_HWT : 0;
        public double MaxDischargingRate => Capacity > 0 ? Capacity / AUT_HWT : 0;
    }
}