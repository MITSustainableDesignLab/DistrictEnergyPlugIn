using System.Linq;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
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
                if (DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>().Select(o=>o.UseDistrictLosses).Any(b=>b))
                    return true;
                return false;
            }
            set
            {
                foreach (var pipeNetwork in DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>())
                {
                    pipeNetwork.UseDistrictLosses = value;
                }

                OnPropertyChanged();
            }
        }

        public double RelDistHeatLoss
        {
            get => DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>().Where(x => x.LoadType == LoadTypes.Heating).Select(o => o.RelativeLoss).Average() * 100;
            set
            {
                foreach (var pipeNetwork in DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>()
                    .Where(x => x.LoadType == LoadTypes.Heating))
                {
                    pipeNetwork.RelativeLoss = value / 100;
                }

                OnPropertyChanged();
            }
        }

        public double RelDistCoolLoss
        {
            get => DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>()
                .Where(x => x.LoadType == LoadTypes.Cooling).Select(o => o.RelDistCoolLoss).Average() * 100;
            set
            {
                foreach (var pipeNetwork in DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>()
                    .Where(x => x.LoadType == LoadTypes.Cooling))
                {
                    pipeNetwork.RelativeLoss = value / 100;
                }

                OnPropertyChanged();
            }
        }
    }
}