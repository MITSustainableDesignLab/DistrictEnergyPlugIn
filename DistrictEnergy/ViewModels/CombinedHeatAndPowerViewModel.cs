using System;
using System.Collections.Generic;
using System.Linq;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class CombinedHeatAndPowerViewModel : PlantSettingsViewModel
    {
        private double _capacity;

        public CombinedHeatAndPowerViewModel()
        {
            Instance = this;
        }

        public new static CombinedHeatAndPowerViewModel Instance { get; set; }

        public IList<LoadTypes> PosibleTrackingModes =>
            new List<LoadTypes>() {LoadTypes.Elec, LoadTypes.Heating};

        public LoadTypes TMOD_CHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP = value;
                OnPropertyChanged();
            }
        }

        public double OFF_CHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP = value / 100;
                OnPropertyChanged(nameof(OFF_CHP));
            }
        }

        public double EFF_CHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double HREC_CHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP =
                    value / 100;
                OnPropertyChanged();
            }
        }

        public double F
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().V = value;
                OnPropertyChanged();
            }
        }
    }
}