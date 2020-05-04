using System.Linq;
using DistrictEnergy.Helpers;
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
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricChiller>().First().CCOP_ECH;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricChiller>().First().CCOP_ECH = value;
                OnPropertyChanged();
            }
        }

        public double F_ECH
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricChiller>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricChiller>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_ECH
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricChiller>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricChiller>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region AbsorptionChiller

        public double OFF_ABS
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().OFF_ABS * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().OFF_ABS = value / 100;
                OnPropertyChanged();
            }
        }

        public double CCOP_ABS
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().CCOP_ABS;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().CCOP_ABS = value;
                OnPropertyChanged();
            }
        }

        public double F_ABS
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().F;
            set 
            { 
                DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_ABS
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().V = value;
                OnPropertyChanged();
            }
        }

        public bool IsForced_ABS
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().IsForced;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<AbsorptionChiller>().First().IsForced = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
