using System.ComponentModel;

namespace DistrictEnergy
{
    public class DistrictSettings
    {

        [DefaultValue(0.15)]
        public double ElectricityGenerationCost { get; set; } = 0.15;

        [DefaultValue(0.05)]
        public double PriceNaturalGas { get; set; } = 0.05;

        [DefaultValue(4.25E-4)]
        public double EmissionsElectricGeneration { get; set; } = 4.25E-4;

        [DefaultValue(0.05)]
        public double LossesTransmission { get; set; } = 0.05;

        [DefaultValue(0.10)]
        public double LossesHeatHydronic { get; set; } = 0.10;

        [DefaultValue(0.40)]
        public double EfficPowerGen { get; set; } = 0.40;

    }

    internal class DistrictSettingsPath
    {
        public const string SettingsFilePathInBundle = "districtSettings.json";
    }
}
