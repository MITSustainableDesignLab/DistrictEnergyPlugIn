using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class WindTurbine : IThermalPlantSettings
    {
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.05)] public double OFF_WND { get; set; } = 0.05;

        /// <summary>
        ///     Turbine coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(0.3)] public double COP_WND { get; set; } = 0.3;

        /// <summary>
        ///     Cut-in speed (m/s)
        /// </summary>
        [DataMember]
        [DefaultValue(5)] public double CIN_WND { get; set; } = 5;

        /// <summary>
        ///     Cut-out speed (m/s)
        /// </summary>
        [DataMember]
        [DefaultValue(25)] public double COUT_WND { get; set; } = 25;

        /// <summary>
        ///     Rotor area per turbine (m2)
        /// </summary>
        [DataMember]
        [DefaultValue(15)] public double ROT_WND { get; set; } = 15;
    }
}