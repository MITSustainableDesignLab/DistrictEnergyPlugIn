using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public interface IDispatchable : IThermalPlantSettings
    {
        LoadTypes InputType { get; set; }
    }
}