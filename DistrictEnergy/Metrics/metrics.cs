using Mit.Umi.RhinoServices;
using Rhino;
using Rhino.Geometry;
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
            if (maximimLoad <= 0)
                throw new System.DivideByZeroException();
            return averageLoad / maximimLoad;
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

        /// <summary>
        /// Calculates the FAR (Floor area ratio)
        /// </summary>
        /// <param name="floorArea">Total building floor area</param>
        /// <param name="landArea">Total land area</param>
        /// <returns></returns>
        public static double FAR(double floorArea, double landArea)
        {
            var far = floorArea / landArea;
            return far;
        }

        /// <summary>
        /// Returns the Land area [m2]. -1 if error
        /// </summary>
        /// <returns></returns>
        public static double LandArea()
        {
            string layername1 = "Site boundary";

            // Get all of the objects on the layer. If layername is bogus, you will
            // just get an empty list back
            Rhino.DocObjects.RhinoObject[] rhobjs1 = RhinoDoc.ActiveDoc.Objects.FindByLayer(layername1).Where(x => x.ObjectType == Rhino.DocObjects.ObjectType.Curve).ToArray();
            if (rhobjs1 == null || rhobjs1.Length < 1)
            {
                RhinoApp.WriteLine("Error: No boundary in project found");
                return -1;
            }

            var curves1 = new Curve[rhobjs1.Length];

            for (int i = 0; i < rhobjs1.Length; i++)
            {
                GeometryBase geom = rhobjs1[i].Geometry;
                Curve x = geom as Curve;
                if (x != null && x.IsValid)
                {
                    curves1[i] = x;
                }
            }
            double area = new double();
            foreach (var o in curves1)
            {
                var areaMassP = AreaMassProperties.Compute(o);
                if (areaMassP != null)
                    area = area + areaMassP.Area;
            }
            return area;
        }

        /// <summary>
        /// Returns the total route lenght. -1 if error.
        /// </summary>
        /// <returns></returns>
        public static double TotalRouteLength()
        {
            string layername = "Heating Network";

            // Get all of the objects on the layer. If layername is bogus, you will
            // just get an empty list back
            Rhino.DocObjects.RhinoObject[] rhobjs = RhinoDoc.ActiveDoc.Objects.FindByLayer(layername).Where(x => x.ObjectType == Rhino.DocObjects.ObjectType.Curve).ToArray();
            if (rhobjs == null || rhobjs.Length < 1)
                return -1;

            var curves = new Curve[rhobjs.Length];

            for (int i = 0; i < rhobjs.Length; i++)
            {
                GeometryBase geom = rhobjs[i].Geometry;
                Curve x = geom as Curve;
                if (x != null && x.IsValid)
                {
                    curves[i] = x;
                }
            }
            double lenght = curves.Sum(x => x.GetLength());
            return lenght;
        }

        /// <summary>
        /// Calculates the distribution capital cost, which represents annual repayments of investment capital for the construction of the district heating network.
        /// </summary>
        /// <param name="FAR">Floor to area ratio</param>
        /// <param name="specificHeatDemand">Specific heat demand</param>
        /// <param name="effWidth">Effective width</param>
        /// <param name="annuity">Annuity payment</param>
        /// <param name="C1">Construction cost constant (pipe)</param>
        /// <param name="C2">Construction cost coefficient (pipe)</param>
        /// <param name="avgDiameter">Average pipe diameter</param>
        /// <returns></returns>
        public static double DistributionCapitalCost(double FAR, double specificHeatDemand, double effWidth, double annuity, double C1, double C2, double avgDiameter)
        {
            try
            {
                var distCapitalCost = annuity * (C1 + C2 * avgDiameter) / (FAR * specificHeatDemand * effWidth);
                return distCapitalCost;
            }
            catch
            {
                return -1;
            }         
            
        }
        
        /// <summary>
        /// Calculates the empirical average pipe diamter, according to Sven Werner. (heat in GJ)
        /// </summary>
        /// <param name="linearHeatDensity">!IMPORTANT in GJ/m</param>
        /// <returns></returns>
        public static double AveragePipeDiamSwedish(double linearHeatDensity)
        {
            var averageDiam = 0.0486 * Math.Log(linearHeatDensity) + 0.0007;
            return averageDiam;
        }

        /// <summary>
        /// Heat sold per annum in GJ
        /// </summary>
        /// <returns>GJ</returns>
        public static double HeatSoldPerAnnum()
        {
            var heatDemand = GlobalContext.GetObjects().Select(b => b.Data["SDL/Heating"].Data.Zip(b.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Sum());
            var heatDemand2 = heatDemand.Sum() * 0.0036;
            return heatDemand2;
        }

        /// <summary>
        /// Annuity payment factor formula.
        /// </summary>
        /// <param name="rate">Rate per period</param>
        /// <param name="period">number of periods</param>
        /// <returns></returns>
        public static double AnnuityPayment(double rate, double period)
        {
            var a = rate / (1 - Math.Pow((1 + rate), -period));
            return a;
        }
    }
}
