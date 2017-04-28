using System;
using Rhino;
using Rhino.Commands;
using Mit.Umi.RhinoServices;
using System.Linq;
using Mit.Umi.Core;
using System.Collections.Generic;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("55eb3b88-a514-4722-aa91-3c86b2788ff2")]
    public class CapacityFactor : Command
    {
        static CapacityFactor _instance;
        public CapacityFactor()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CapacityFactor command.</summary>
        public static CapacityFactor Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CapacityFactor"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var MaxHeatinLoadQuery = GlobalContext.GetObjects().
                Select(b => new
                { BuildingId = b.Id, MaxLoad = MaxHeatingLoad(b), AverageLoad = AverageHeatingLoad(b) });

            List<double> cap = new List<double>();
            foreach(var a in MaxHeatinLoadQuery)
            {
                cap.Add(CalcCapactityFactor(a.AverageLoad, a.MaxLoad));
            }


            return Result.Success;
        }

        /// <summary>
        /// This finds the maximum heating Load (SP + DHW)
        /// </summary>
        /// <param name="building">A building IUmiObject</param>
        /// <returns></returns>
        public static double MaxHeatingLoad(Mit.Umi.Core.IUmiObject building)
        {
            return building.Data["SDL/Heating"].Data.Zip(building.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Max();
        }

        /// <summary>
        /// This finds the maximum heating Load (SP + DHW)
        /// </summary>
        /// <param name="building">A building IUmiObject</param>
        /// <returns></returns>
        public static double AverageHeatingLoad(Mit.Umi.Core.IUmiObject building)
        {
            return building.Data["SDL/Heating"].Data.Zip(building.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Average();
        }

        /// <summary>
        /// Calculates a capacity factor
        /// </summary>
        /// <param name="averageLoad"></param>
        /// <param name="maximimLoad"></param>
        /// <returns></returns>
        public double CalcCapactityFactor(double averageLoad,double maximimLoad)
        {
            double cap = averageLoad / maximimLoad;
            return cap;
        }
    }
}
