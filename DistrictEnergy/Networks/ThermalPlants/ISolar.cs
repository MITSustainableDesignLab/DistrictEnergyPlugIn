﻿using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    /// <summary>
    /// Interface for Supply Modules Using Solar as input
    /// </summary>
    internal interface ISolar : IThermalPlantSettings
    {
        [JsonIgnore] double AvailableArea { get; }
        [JsonIgnore] double[] SolarAvailableInput { get; }
    }
}