using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    /// <summary>
    /// Interface for Supply Modules Using Solar as input
    /// </summary>
    internal interface IWind : IThermalPlantSettings
    {
        double NumWnd { get; }
        double Power(int t = 0, int dt = 8760);
    }
}