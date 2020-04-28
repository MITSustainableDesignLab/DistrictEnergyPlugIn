using System.Linq;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class HotWaterViewModel : PlantSettingsViewModel
    {
        private double _hpCapacity;
        private double _hwStoCapacity;
        private double _shwCapacity;

        public HotWaterViewModel()
        {
            Instance = this;
        }

        public new static HotWaterViewModel Instance { get; set; }

        #region NatGAs

        public double EFF_NGB
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB = value / 100;
                OnPropertyChanged();
            }
        }

        public double F_NGB
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<NatGasBoiler>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<NatGasBoiler>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_NGB
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<NatGasBoiler>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<NatGasBoiler>().First().V = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region HPs

        public double OFF_EHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP = value / 100;
                OnPropertyChanged();
                CalcHpCapacity();
            }
        }

        public double HCOP_EHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP = value;
                OnPropertyChanged();
            }
        }

        public bool UseEhpEvap
        {
            get
            {
                if (DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 1;
                    OnPropertyChanged();
                }
                else
                {
                    DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 0;
                    OnPropertyChanged();
                }
            }
        }

        public double F_EHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_EHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().V = value;
                OnPropertyChanged();
            }
        }

        public double HpCapacity
        {
            get { return _hpCapacity; }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().Capacity = value;
                OnPropertyChanged();
            }
        }

        private void CalcHpCapacity()
        {
            HpCapacity = DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP *
                         DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Heating).Select(v => v.Input.Max()).Sum();
        }

        #endregion

        #region HWSto

        public double AUT_HWT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT = value;
                OnPropertyChanged();
                CalcHwStoCapacityCapacity();
            }
        }

        public double TANK_START
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START = value / 100;
                OnPropertyChanged();
            }
        }

        public double F_HWT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_HWT
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().V = value;
                OnPropertyChanged();
            }
        }

        public double HwStoCapacity
        {
            get { return _hwStoCapacity; }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().Capacity = value;
                OnPropertyChanged();
            }
        }

        private void CalcHwStoCapacityCapacity()
        {
            // todo Define a more advanced capacity formulation
            HwStoCapacity = DistrictControl.Instance.ListOfDistrictLoads.Where(x=>x.LoadType == LoadTypes.Heating).Select(v=>v.Input.Average()).Sum() *
                            DistrictControl.Instance.ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT * 24;
        }

        #endregion

        #region SolarThermal

        public double EFF_SHW
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW =
                    value / 100;
                OnPropertyChanged();
            }
        }

        public double OFF_SHW
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW =
                    value / 100;
                OnPropertyChanged();
                CalcShwCapacity();
            }
        }

        public double UTIL_SHW
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW =
                    value / 100;
                OnPropertyChanged();
            }
        }

        public double LOSS_SHW
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW =
                    value / 100;
                OnPropertyChanged();
            }
        }

        public double F_SHW
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().F = value;
                OnPropertyChanged();
            }
        }

        public double V_SHW
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().V = value;
                OnPropertyChanged();
            }
        }

        public double ShwCapacity
        {
            get { return _shwCapacity; }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().Capacity = value;
                OnPropertyChanged();
            }
        }

        private void CalcShwCapacity()
        {
            ShwCapacity = DistrictControl.Instance.ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW *
                          DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Heating).Select(v => v.Input.Max()).Sum();
        }

        #endregion
    }
}