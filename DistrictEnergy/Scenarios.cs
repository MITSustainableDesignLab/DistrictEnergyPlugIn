using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy
{
    [Serializable]
    public class SimCase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DName { get; set; }
        public ObservableCollection<IThermalPlantSettings> ListOfPlantSettings { get; set; }
    }
}