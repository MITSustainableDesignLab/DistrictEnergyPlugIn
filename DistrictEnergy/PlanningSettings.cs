using System.ComponentModel;

namespace DistrictEnergy
{
    public class PlanningSettings
    {

        [DefaultValue(230)]
        public double C1 { get; set; } = 230;

        [DefaultValue(1860)]
        public double C2 { get; set; } = 1860;

        [DefaultValue(0.03)]
        public double Rate { get; set; } = 0.03;

        [DefaultValue(25)]
        public double Periods { get; set; } = 25;

        [DefaultValue(0.9)]
        public double PumpEfficiency { get; set; } = 0.9;
    }
}
