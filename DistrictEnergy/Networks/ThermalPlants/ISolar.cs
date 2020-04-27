namespace DistrictEnergy.Networks.ThermalPlants
{
    /// <summary>
    /// Interface for Supply Modules Using Solar as input
    /// </summary>
    internal interface ISolar : IThermalPlantSettings
    {
        double AvailableArea { get; }
        double[] SolarAvailableInput { get; }
    }
}