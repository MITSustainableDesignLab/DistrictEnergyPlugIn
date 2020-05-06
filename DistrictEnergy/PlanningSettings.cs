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
    }
}