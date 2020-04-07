using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class HotWaterViewModel : PlantSettingsViewModel
    {
        public HotWaterViewModel()
        {
            Instance = this;
        }

        public new static HotWaterViewModel Instance { get; set; }

        public double EFF_NGB
        {
            get => ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB * 100;
            set
            {
                ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB = value / 100;
                OnPropertyChanged();
            }
        }

        public double OFF_EHP
        {
            get => ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP * 100;
            set
            {
                ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double HCOP_EHP
        {
            get => ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP;
            set
            {
                ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP = value;
                OnPropertyChanged();
            }
        }

        public double AUT_HWT
        {
            get => ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT;
            set
            {
                ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT = value;
                OnPropertyChanged();
            }
        }

        public double TANK_START
        {
            get => ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START * 100;
            set
            {
                ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public double OFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public double UTIL_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public double LOSS_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public bool UseEhpEvap
        {
            get
            {
                if (ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 1;
                    OnPropertyChanged();
                }
                else
                {
                    ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 0;
                    OnPropertyChanged();
                }
            }
        }
    }
}
