using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                if (ListOfPlantSettings.OfType<PipeNetwork>().First().UseDistrictLosses == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    ListOfPlantSettings.OfType<PipeNetwork>().First().UseDistrictLosses = 1;
                    OnPropertyChanged();
                }
                else
                {
                    ListOfPlantSettings.OfType<PipeNetwork>().First().UseDistrictLosses = 0;
                    OnPropertyChanged();
                }
            }
        }

        public double RelDistHeatLoss
        {
            get => ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistHeatLoss * 100;
            set
            {
                ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistHeatLoss = value / 100;
                OnPropertyChanged();
            }
        }

        public double RelDistCoolLoss
        {
            get => ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistCoolLoss * 100;
            set
            {
                ListOfPlantSettings.OfType<PipeNetwork>().First().RelDistCoolLoss = value / 100;
                OnPropertyChanged();
            }
        }
    }
}