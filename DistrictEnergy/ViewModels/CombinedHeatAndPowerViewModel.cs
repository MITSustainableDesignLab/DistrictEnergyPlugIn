using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class CombinedHeatAndPowerViewModel : PlantSettingsViewModel
    {
        public CombinedHeatAndPowerViewModel()
        {
            Instance = this;
        }

        public new static CombinedHeatAndPowerViewModel Instance { get; set; }

        public IList<TrakingModeEnum> PosibleTrackingModes =>
            Enum.GetValues(typeof(TrakingModeEnum)).Cast<TrakingModeEnum>().ToList();

        public TrakingModeEnum TMOD_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP = value;
                OnPropertyChanged();
            }
        }

        public double OFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double HREC_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double F
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().F;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().V;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().V = value;
                OnPropertyChanged();
            }
        }
    }
}
