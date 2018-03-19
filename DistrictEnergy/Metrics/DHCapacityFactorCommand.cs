using System;
using Rhino;
using Rhino.Commands;
using System.Linq;
using System.Collections.Generic;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("55eb3b88-a514-4722-aa91-3c86b2788ff2")]
    public class DHCapacityFactorCommand : Command
    {
        static DHCapacityFactorCommand _instance;
        public DHCapacityFactorCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CapacityFactor command.</summary>
        public static DHCapacityFactorCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHCapacityFactor"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var MaxHeatinLoadQuery = UmiContext.Current.GetObjects().
                Select(b => new
                { BuildingId = b.Id, MaxLoad = MaxHeatingLoad(b), AverageLoad = AverageHeatingLoad(b) });

            List<double> cap = new List<double>();
            foreach(var a in MaxHeatinLoadQuery)
            {
                try
                {
                    cap.Add(Metrics.CalcCapacityFactor(a.AverageLoad, a.MaxLoad));
                }
                catch (DivideByZeroException e)
                {
                    Console.WriteLine("Attempted divide by zero");
                    throw;
                }
                
            }


            return Result.Success;
        }

        /// <summary>
        /// This finds the maximum heating Load (SP + DHW)
        /// </summary>
        /// <param name="building">A building IUmiObject</param>
        /// <returns></returns>
        public static double MaxHeatingLoad(IUmiObject building)
        {
            return building.Data["SDL/Heating"].Data.Zip(building.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Max();
        }

        /// <summary>
        /// This finds the maximum heating Load (SP + DHW)
        /// </summary>
        /// <param name="building">A building IUmiObject</param>
        /// <returns></returns>
        public static double AverageHeatingLoad(IUmiObject building)
        {
            return building.Data["SDL/Heating"].Data.Zip(building.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Average();
        }
    }
}
