using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class ElectricGenerationViewModel : PlantSettingsViewModel
    {
        public ElectricGenerationViewModel()
        {
            Instance = this;
        }

        public new static ElectricGenerationViewModel Instance { get; set; }

        #region PV

        public double OFF_PV
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().OFF_PV * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().OFF_PV = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_PV
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV = value / 100;
                OnPropertyChanged();
            }
        }

        public double UTIL_PV
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV = value / 100;
                OnPropertyChanged();
            }
        }

        public double LOSS_PV
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV = value / 100;
                OnPropertyChanged();
            }
        }
        public double F_PV
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_PV
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PhotovoltaicArray>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Wind

        public double OFF_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().OFF_WND * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().OFF_WND = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().EFF_WND * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().EFF_WND = value / 100;
                OnPropertyChanged();
            }
        }

        public double CIN_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().CIN_WND;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().CIN_WND = value;
                OnPropertyChanged();
            }
        }

        public double COUT_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().COUT_WND;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().COUT_WND = value;
                OnPropertyChanged();
            }
        }

        public double ROT_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().ROT_WND;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().ROT_WND = value;
                OnPropertyChanged();
            }
        }

        public double LOSS_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().LOSS_WND * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().LOSS_WND = value / 100;
                OnPropertyChanged();
            }
        }

        public double F_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_WND
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<WindTurbine>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region BatteryBank

        public double AUT_BAT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().AUT_BAT;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().AUT_BAT = value;
                OnPropertyChanged();
            }
        }

        public double LOSS_BAT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT = value / 100;
                OnPropertyChanged();
            }
        }

        public double BAT_START
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().BAT_START * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().BAT_START = value / 100;
                OnPropertyChanged();
            }
        }

        public double F_BAT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_BAT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<BatteryBank>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}