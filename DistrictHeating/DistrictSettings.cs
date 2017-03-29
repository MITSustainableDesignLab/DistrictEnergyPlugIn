using System.ComponentModel;

namespace DistrictEnergy
{
    public class DistrictSettings
    {
        [DefaultValue(0.15)]
        public double ElectricityGenerationCost { get; set; } = 0.15;

        [DefaultValue(0.18)]
        public double PriceNaturalGas { get; set; } = 0.18;

        [DefaultValue(0.18)]
        public double EmissionsElectricGeneration { get; set; } = 0.18;

        [DefaultValue(0.18)]
        public double LossesTransmission { get; set; } = 0.18;

        [DefaultValue(0.18)]
        public double LossesHeatHydronic { get; set; } = 0.18;

        [DefaultValue(0.18)]
        public double EfficPowerGen { get; set; } = 0.18;
    }

    internal class DistrictSettingsPath
    {
        public const string SettingsFilePathInBundle = "districtSettings.json";
    }
}
