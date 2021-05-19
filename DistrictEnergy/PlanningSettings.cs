using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace DistrictEnergy
{
    public class PlanningSettings
    {
        [DefaultValue(0.03)] public double Rate { get; set; } = 0.03;
        [DefaultValue(25)] public double Periods { get; set; } = 25;
        [DefaultValue(365)] public double TimeSteps { get; set; } = 365;
        [JsonIgnore] public double AnnuityFactor => Metrics.Metrics.AnnuityPayment(Rate, Periods);
        public bool UseDistrictLosses { get; set; } = true;
        public double RelDistHeatLoss { get; set; } = 0.15;
        public double RelDistCoolLoss { get; set; } = 0.06;
        /// <summary>
        /// The ratio by which the objective function is multiplied
        /// </summary>
        public double CarbonRatio { get; set; } = 0.5;
        /// <summary>
        /// The price of carbon per ton emitted as defined by the Biden administration.
        /// </summary>
        [DefaultValue(51)] public double CarbonPricePerTon { get; set; } = 51;

        /// <summary>
        /// If True, a constraint is added to satisfy the Zero Energy Community rule
        /// </summary>
        public bool IsZeroEnergyCommunity { get; set; }
        /// <summary>
        /// If True, a constraint is added preventing use of natural gas in the energy hub
        /// </summary>
        public bool IsNoGasSolution { get; set; }
    }
}