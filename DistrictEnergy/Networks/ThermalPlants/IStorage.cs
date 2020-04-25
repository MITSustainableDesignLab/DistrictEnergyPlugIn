namespace DistrictEnergy.Networks.ThermalPlants
{
    public interface IStorage : IThermalPlantSettings
    {
        double ChargingEfficiency { get; }
        double DischargingEfficiency { get; }
        double StorageStandingLosses { get; }
        double[] Input { get; set; }
        double[] Storage { get; set; }
        double MaxChargingRate { get; }
        double MaxDischargingRate { get; }
    }
}