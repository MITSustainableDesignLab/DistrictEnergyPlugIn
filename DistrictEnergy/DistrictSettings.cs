using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy
{
    public class SimCase
    {
        public int Id { get; set; }
        public string DName { get; set; }
    }

    public class DistrictSettings
    {
        public ObservableCollection<SimCase> SimCases { get; set; }
        public SimCase ASimCase { get; set; }
    }
}