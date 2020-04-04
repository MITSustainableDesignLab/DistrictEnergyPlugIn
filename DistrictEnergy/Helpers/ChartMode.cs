using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.Helpers
{
    /// <summary>
    /// Stacked mode, for stacked series
    /// </summary>
    public enum ChartMode
    {
        /// <summary>
        /// Shows cost profile
        /// </summary>
        Cost,
        /// <summary>
        /// Shows the source energy use (fuel)
        /// </summary>
        FuelUse,
        /// <summary>
        /// Shows the carbon emissions (kgCO2eq)
        /// </summary>
        Carbon,
        /// <summary>
        /// Shoes the load
        /// </summary>
        Load
    }
}
