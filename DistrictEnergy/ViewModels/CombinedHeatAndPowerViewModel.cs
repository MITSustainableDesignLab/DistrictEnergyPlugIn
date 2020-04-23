using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deedle.Vectors;
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

        public IList<TrakingModeEnum> PosibleTrackingModes =>
            Enum.GetValues(typeof(TrakingModeEnum)).Cast<TrakingModeEnum>().ToList();

        public TrakingModeEnum TMOD_CHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP = value;
                OnPropertyChanged();
                CalcCapacity();
            }
        }

        public double OFF_CHP
        {
            get => DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP * 100;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP = value / 100;
                OnPropertyChanged(nameof(OFF_CHP));
                CalcCapacity();
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

        public double Capacity
        {
            get { return _capacity; }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().Capacity = value;
                OnPropertyChanged();
            }
        }

        private void CalcCapacity()
        {
            switch (TMOD_CHP)
            {
                case (TrakingModeEnum.Electrical):
                    Capacity =
                        DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP *
                        DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Max();
                    break;
                case (TrakingModeEnum.Thermal):
                    Capacity =
                        DistrictControl.Instance.ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP *
                        DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Max();
                    break;
            }
        }
    }
}