﻿using System.Linq;
using DistrictEnergy.ViewModels;
using Rhino;
using Rhino.Commands;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("f178c24e-926c-41cb-91d0-9773eae17cd1")]
    public class DHDistributionCapitalCostCommand : Command
    {
        static DHDistributionCapitalCostCommand _instance;
        public DHDistributionCapitalCostCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DistributionCapitalCostCommand command.</summary>
        public static DHDistributionCapitalCostCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHDistributionCapitalCost"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var heatDemand = UmiContext.Current.GetObjects().Select(b => b.Data["SDL/Heating"].Data.Zip(b.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Sum()).Sum();
            var bldFloor = UmiContext.Current.GetObjects().Select(b => b.Data["SDL/Gross Floor Area"].Data.Sum()).Sum();
            var landArea = Metrics.LandArea();
            var length = Metrics.TotalRouteLength();
            var far = Metrics.FAR(bldFloor, landArea);
            var specificHeat = heatDemand / bldFloor;
            var effectiveWidth = Metrics.EffThermalWidth(landArea, length);

            double annuity = Metrics.AnnuityPayment(DistrictControl.PlanningSettings.Rate, DistrictControl.PlanningSettings.Periods);
            double c1 = 0;
            double c2 = 0;

            double da = Metrics.AveragePipeDiamSwedish(heatDemand * 0.0036 / length);

            double capitalCost = Metrics.DistributionCapitalCost(far, specificHeat, effectiveWidth, annuity, c1, c2, da);

            RhinoApp.WriteLine("Specific capital Cost : {0:F3} [$/kWh]", capitalCost);
#if DEBUG
            RhinoApp.WriteLine("Debug:\nHeat Demand: {0}", heatDemand);
            RhinoApp.WriteLine("Average pipe diameter: {0}", da);
            RhinoApp.WriteLine("Annuity: {0}", annuity);
            RhinoApp.WriteLine("Effective width : {0}",effectiveWidth);
            RhinoApp.WriteLine("Capital Cost : {0}",capitalCost);
#endif

            return Result.Success;
        }
    }
}
