using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.Metrics
{
    /// <summary>A list of metrics formulas.</summary>
    public class Metrics
    {
        /// <summary>
        /// Calculates a capacity factor
        /// </summary>
        /// <param name="averageLoad">The Average capacity demand</param>
        /// <param name="maximimLoad">The Maximum capacity demand</param>
        /// <returns>The capacity factor. -1 if maximumLoad is lowerOrequal to zero.=</returns>
        public static double CalcCapacityFactor(double averageLoad, double maximimLoad)
        {
            if (maximimLoad > 0)
            {
                double cap = averageLoad / maximimLoad;
                return cap;
            }
            else
                return -1;
        }

        /// <summary>
        /// Calculates the capacity function for the whole system.
        /// </summary>
        /// <param name="mu_sh">Capacity factor for space heating alone</param>
        /// <param name="mu_dhw">Capacity factor for domestic hot water alone</param>
        /// <param name="mu_hl">capacity factor for distribution heat losses alone</param>
        /// <param name="f_sh">Annual proportion of heat supply for space heating</param>
        /// <param name="f_dhw">Annual proportion of heat supply for DHW</param>
        /// <param name="f_hl">Annual proportion of heat supply for distribution heat losses</param>
        /// <returns>The capacity factor. -1 if sum of annual proportions is not equal to 1</returns>
        public static double CalcCapacityFactor(double mu_sh, double mu_dhw, double mu_hl, double f_sh, double f_dhw, double f_hl)
        {
            var t = f_sh + f_dhw + f_hl;
            if (t == 1)
            {
                double cap_1 = f_sh / mu_sh + f_dhw / mu_dhw + f_hl / mu_hl;
                double cap = 1 / cap_1;
                return cap;
            }
            else
                return -1;
        }

        /// <summary>
        /// Simple calculation of the effective thermal width : w = A_L / L [m]
        /// </summary>
        /// <param name="area">Total land area</param>
        /// <param name="length">Total route length</param>
        /// <returns>The effective thermal width. -1 if lenght is lowerOrequal than zero</returns>
        public static double EffThermalWidth(double area, double length)
        {
            double w = new double(); //The Effective width
            if (area > 0 && length > 0)
            {
                w = area / length;
                return w;
            }
            else
                return -1;
            
        }
    }
}
