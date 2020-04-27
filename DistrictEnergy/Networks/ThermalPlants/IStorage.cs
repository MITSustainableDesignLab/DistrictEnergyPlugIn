using System.Collections.Generic;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public interface IStorage : IThermalPlantSettings
    {
        double ChargingEfficiency { get; }
        double DischargingEfficiency { get; }
        double StorageStandingLosses { get; }
        [JsonIgnore] List<DateTimePoint> Storage { get; set; }
        double MaxChargingRate { get; }
        double MaxDischargingRate { get; }
        double StartingCapacity { get; }
    }
}