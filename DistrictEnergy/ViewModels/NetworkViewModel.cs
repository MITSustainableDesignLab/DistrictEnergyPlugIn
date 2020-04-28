using System.Linq;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class NetworkViewModel : PlantSettingsViewModel
    {
        public NetworkViewModel()
        {
            Instance = this;
        }

        public new static NetworkViewModel Instance { get; set; }

        public bool UseDistrictLosses
        {
            get
            {
                if (DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().UseDistrictLosses == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().UseDistrictLosses = 1;
                    OnPropertyChanged();
                }
                else
                {
                    DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().UseDistrictLosses = 0;
                    OnPropertyChanged();
                }
            }
        }

        public double RelDistHeatLoss
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistHeatLoss * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistHeatLoss = value / 100;
                OnPropertyChanged();
            }
        }

        public double RelDistCoolLoss
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistCoolLoss * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistCoolLoss = value / 100;
                OnPropertyChanged();
            }
        }
    }
}