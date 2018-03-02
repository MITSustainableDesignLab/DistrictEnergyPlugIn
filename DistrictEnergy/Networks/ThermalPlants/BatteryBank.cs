using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.Networks.ThermalPlants
{
    class BatteryBank
    {
        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(1)]
        public double AUT_BAT { get; set; } = 1;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(15)]
        public double LOSS_BAT { get; set; } = 15;
    }
}
