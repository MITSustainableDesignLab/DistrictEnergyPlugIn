using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class ChilledWaterViewModel : PlantSettingsViewModel
    {
        public ChilledWaterViewModel()
        {
            Instance = this;
        }

        public new static ChilledWaterViewModel Instance { get; set; }

        #region Chiller

        public double CCOP_ECH
        {
            get => ListOfPlantSettings.OfType<ElectricChiller>().First().CCOP_ECH;
            set
            {
                ListOfPlantSettings.OfType<ElectricChiller>().First().CCOP_ECH = value;
                OnPropertyChanged();
            }
        }

        public double F_ECH
        {
            get => ListOfPlantSettings.OfType<ElectricChiller>().First().F;
            set
            {
                ListOfPlantSettings.OfType<ElectricChiller>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_ECH
        {
            get => ListOfPlantSettings.OfType<ElectricChiller>().First().V;
            set
            {
                ListOfPlantSettings.OfType<ElectricChiller>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region AbsorptionChiller

        public double OFF_ABS
        {
            get => ListOfPlantSettings.OfType<AbsorptionChiller>().First().OFF_ABS * 100;
            set
            {
                ListOfPlantSettings.OfType<AbsorptionChiller>().First().OFF_ABS = value / 100;
                OnPropertyChanged();
            }
        }

        public double CCOP_ABS
        {
            get => ListOfPlantSettings.OfType<AbsorptionChiller>().First().CCOP_ABS;
            set
            {
                ListOfPlantSettings.OfType<AbsorptionChiller>().First().CCOP_ABS = value;
                OnPropertyChanged();
            }
        }

        public double F_ABS
        {
            get => ListOfPlantSettings.OfType<AbsorptionChiller>().First().F;
            set 
            { 
                ListOfPlantSettings.OfType<AbsorptionChiller>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_ABS
        {
            get => ListOfPlantSettings.OfType<AbsorptionChiller>().First().V;
            set
            {
                ListOfPlantSettings.OfType<AbsorptionChiller>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
