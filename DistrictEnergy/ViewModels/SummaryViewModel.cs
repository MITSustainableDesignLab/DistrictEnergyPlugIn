using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class SummaryViewModel : INotifyPropertyChanged
    {
        private double _areaPv;
        private double _areaShw;
        private double _capAbs;
        private double _capBat;
        private double _capChpElec;
        private double _capChpHeat;
        private double _capEch;
        private double _capEhp;
        private double _capElecProj;
        private double _capHwt;
        private double _capNgb;
        private double _capPv;
        private double _capShw;
        private double _capWnd;
        private double _chgrBat;
        private double _chgrHwt;
        private double _dchgBat;
        private double _dchgrHwt;
        private double _lossHwt;
        private double _numWnd;
        private double _relDistCoolLoss;
        private double _relDistHeatLoss;
        private double _userDefinedAreaPv;
        private double _modelDchgBat;
        private double _modelDchgrHwt;
        private double _modelChgrBat;
        private double _modelChgrHwt;
        private double _modelNumWnd;
        private double _modelAreaPv;
        private double _modelAreaShw;
        private double _modelCapWnd;
        private double _modelCapPv;
        private double _modelCapElecProj;
        private double _modelCapEch;
        private double _modelCapShw;
        private double _modelCapNgb;
        private double _modelCapChpHeat;
        private double _modelCapChpElec;
        private double _modelCapHwt;
        private double _modelCapBat;
        private double _modelCapEhp;
        private double _modelCapAbs;
        private double _modelElectricityEnergy;
        private double _modelHeatingEnergy;
        private double _modelCoolingEnergy;
        private double _modelElectricityDemand;
        private double _modelHeatingDemand;
        private double _modelCoolingDemand;

        public SummaryViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
        }

        public static SummaryViewModel Instance { get; set; }


        /// <summary>
        ///     Cooling capacity of absorption chillers (kW)
        /// </summary>
        public double CapAbs
        {
            get => _capAbs;
            set
            {
                _capAbs = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of Electrical Heat Pumps
        /// </summary>
        public double CapEhp
        {
            get => _capEhp;
            set
            {
                _capEhp = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of Battery, defined as the average demand times the desired autonomy
        /// </summary>
        public double CapBat
        {
            get => _capBat;
            set
            {
                _capBat = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of Hot Water Tank, defined as the everage demand times the desired autonomy
        /// </summary>
        public double CapHwt
        {
            get => _capHwt;
            set
            {
                _capHwt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Electricity Output Capacity of CHP plant
        /// </summary>
        public double CapChpElec
        {
            get => _capChpElec;
            set
            {
                _capChpElec = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Calculated required area of solar thermal collector (m^2)
        /// </summary>
        public double AreaShw
        {
            get => _areaShw;
            set
            {
                _areaShw = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Calculated required area of PV collectors
        /// </summary>
        public double AreaPv
        {
            get => _areaPv;
            set
            {
                _areaPv = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Number of turbines needed: Annual electricity needed divided by how much one turbine generates.
        ///     [Annual Energy that needs to be generated/(0.635 x Rotor Area X sum of cubes of all wind speeds within cut-in and
        ///     cut-out speeds x COP)]
        /// </summary>
        public double NumWnd
        {
            get => _numWnd;
            set
            {
                _numWnd = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     The Hot Water Tank Charge Rate (kWh / h)
        /// </summary>
        public double ChgrHwt
        {
            get => _chgrHwt;
            set
            {
                _chgrHwt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     The Battery Charge Rate (kWh / h)
        /// </summary>
        public double ChgrBat
        {
            get => _chgrBat;
            set
            {
                _chgrBat = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Discharge rate of Hot Water Tank
        /// </summary>
        public double DchgrHwt
        {
            get => _dchgrHwt;
            set
            {
                _dchgrHwt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Discharge rate of battery
        /// </summary>
        public double DchgBat
        {
            get => _dchgBat;
            set
            {
                _dchgBat = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Hot Water Tank Losses (dependant of the outdoor temperature)
        /// </summary>
        public double LossHwt
        {
            get => _lossHwt;
            set
            {
                _lossHwt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Natural Gas Boiler Peak Demand
        /// </summary>
        public double CapNgb
        {
            get => _capNgb;
            set
            {
                _capNgb = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of CHP plant in hotwater production (max over the year)
        /// </summary>
        public double CapChpHeat
        {
            get => _capChpHeat;
            set
            {
                _capChpHeat = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of the solar hot water array
        /// </summary>
        public double CapShw
        {
            get => _capShw;
            set
            {
                _capShw = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of the Electric Chiller
        /// </summary>
        public double CapEch
        {
            get => _capEch;
            set
            {
                _capEch = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of the grid to supply the project
        /// </summary>
        public double CapElecProj
        {
            get => _capElecProj;
            set
            {
                _capElecProj = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of the PV array
        /// </summary>
        public double CapPv
        {
            get => _capPv;
            set
            {
                _capPv = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Capacity of the wind turbine field
        /// </summary>
        public double CapWnd
        {
            get => _capWnd;
            set
            {
                _capWnd = value;
                OnPropertyChanged();
            }
        }

        public double RelDistHeatLoss
        {
            get => _relDistHeatLoss;
            set
            {
                _relDistHeatLoss = value;
                OnPropertyChanged();
            }
        }

        public double RelDistCoolLoss
        {
            get => _relDistCoolLoss;
            set
            {
                _relDistHeatLoss = value;
                OnPropertyChanged();
            }
        }

        public double ModelDchgBat
        {
            get => _modelDchgBat;
            set
            {
                if (value.Equals(_modelDchgBat)) return;
                _modelDchgBat = value;
                OnPropertyChanged();
            }
        }

        public double ModelDchgrHwt
        {
            get => _modelDchgrHwt;
            set
            {
                if (value.Equals(_modelDchgrHwt)) return;
                _modelDchgrHwt = value;
                OnPropertyChanged();
            }
        }

        public double ModelChgrBat
        {
            get => _modelChgrBat;
            set
            {
                if (value.Equals(_modelChgrBat)) return;
                _modelChgrBat = value;
                OnPropertyChanged();
            }
        }

        public double ModelChgrHwt
        {
            get => _modelChgrHwt;
            set
            {
                if (value.Equals(_modelChgrHwt)) return;
                _modelChgrHwt = value;
                OnPropertyChanged();
            }
        }

        public double ModelNumWnd
        {
            get => _modelNumWnd;
            set
            {
                if (value.Equals(_modelNumWnd)) return;
                _modelNumWnd = value;
                OnPropertyChanged();
            }
        }

        public double ModelAreaPv
        {
            get => _modelAreaPv;
            set
            {
                if (value.Equals(_modelAreaPv)) return;
                _modelAreaPv = value;
                OnPropertyChanged();
            }
        }

        public double ModelAreaShw
        {
            get => _modelAreaShw;
            set
            {
                if (value.Equals(_modelAreaShw)) return;
                _modelAreaShw = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapWnd
        {
            get => _modelCapWnd;
            set
            {
                if (value.Equals(_modelCapWnd)) return;
                _modelCapWnd = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapPv
        {
            get => _modelCapPv;
            set
            {
                if (value.Equals(_modelCapPv)) return;
                _modelCapPv = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapElecProj
        {
            get => _modelCapElecProj;
            set
            {
                if (value.Equals(_modelCapElecProj)) return;
                _modelCapElecProj = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapEch
        {
            get => _modelCapEch;
            set
            {
                if (value.Equals(_modelCapEch)) return;
                _modelCapEch = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapShw
        {
            get => _modelCapShw;
            set
            {
                if (value.Equals(_modelCapShw)) return;
                _modelCapShw = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapNgb
        {
            get => _modelCapNgb;
            set
            {
                if (value.Equals(_modelCapNgb)) return;
                _modelCapNgb = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapChpHeat
        {
            get => _modelCapChpHeat;
            set
            {
                if (value.Equals(_modelCapChpHeat)) return;
                _modelCapChpHeat = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapChpElec
        {
            get => _modelCapChpElec;
            set
            {
                if (value.Equals(_modelCapChpElec)) return;
                _modelCapChpElec = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapHwt
        {
            get => _modelCapHwt;
            set
            {
                if (value.Equals(_modelCapHwt)) return;
                _modelCapHwt = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapBat
        {
            get => _modelCapBat;
            set
            {
                if (value.Equals(_modelCapBat)) return;
                _modelCapBat = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapEhp
        {
            get => _modelCapEhp;
            set
            {
                if (value.Equals(_modelCapEhp)) return;
                _modelCapEhp = value;
                OnPropertyChanged();
            }
        }

        public double ModelCapAbs
        {
            get => _modelCapAbs;
            set
            {
                if (value.Equals(_modelCapAbs)) return;
                _modelCapAbs = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += CalculateUserConstants;
            ChilledWaterViewModel.Instance.PropertyChanged += CalculateUserConstants;
            CombinedHeatAndPowerViewModel.Instance.PropertyChanged += CalculateUserConstants;
            ElectricGenerationViewModel.Instance.PropertyChanged += CalculateUserConstants;
            HotWaterViewModel.Instance.PropertyChanged += CalculateUserConstants;
            NetworkViewModel.Instance.PropertyChanged += CalculateUserConstants;
        }

        private void CalculateUserConstants(object sender, EventArgs e)
        {
            CalculateUserConstants();
            CalculateModelConstants();
        }

        /// <summary>
        ///     Calculates the necessary constants used in different equations
        /// </summary>
        public void CalculateUserConstants()
        {
            CapAbs = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Max() * Settings.OffAbs;
            CapEhp = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Max() * Settings.OffEhp;
            CapBat = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Average() * Settings.AutBat * 24;
            CapHwt = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Average() * Settings.AutHwt *
                     24; // todo Prendre jour moyen du mois max.
            CapChpElec = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Max() * Settings.OffChp;
            CapChpHeat = DHSimulateDistrictEnergy.Instance.ResultsArray.HwChp.Max(); //todo: this needs a fix
            CapNgb = DHSimulateDistrictEnergy.Instance.ResultsArray.NgasNgb.Max();
            CapShw = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum() * Settings.OffShw;
            CapEch = DHSimulateDistrictEnergy.Instance.ResultsArray.ChwEch.Max();
            CapElecProj = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecProj.Max();
            CapPv = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum() * Settings.OffPv; //todo: annual or peak?
            CapWnd = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum() *
                     Settings.OffWnd; //todo: annual or peak?
            AreaShw = CapShw /
                      (DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.Sum() * Settings.EffShw *
                       (1 - Settings.LossShw) * Settings.UtilShw);
            AreaPv = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum() * Settings.OffPv /
                    (DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.Sum() * Settings.EffPv *
                     (1 - Settings.LossPv) * Settings.UtilPv);
            var windCubed = DHSimulateDistrictEnergy.Instance.DistrictDemand.WindN
                .Where(w => w > Settings.CinWnd && w < Settings.CoutWnd).Select(w => Math.Pow(w, 3))
                .Sum();
            NumWnd = Math.Ceiling(DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum() * Settings.OffWnd /
                                  (0.6375 * windCubed * Settings.RotWnd * (1 - Settings.LossWnd) * Settings.CopWnd /
                                   1000)); // Divide by 1000 because equation spits out Wh
            ChgrHwt = CapHwt == 0 ? 0 : CapHwt / 12 / Settings.AutHwt; // 12 hours // (AUT_HWT * 12);
            ChgrBat = CapBat == 0 ? 0 : CapBat / 12 / Settings.AutBat;
            DchgrHwt = CapHwt == 0
                ? 0
                : CapHwt / 12 /
                  Settings.AutHwt; // (AUT_HWT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
            DchgBat = CapBat == 0
                ? 0
                : CapBat / 12 /
                  Settings.AutBat; // (AUT_BAT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
        }

        /// <summary>
        ///     Calculates Constants based on Simulation Results
        /// </summary>
        public void CalculateModelConstants()
        {
            ModelCapAbs = DHSimulateDistrictEnergy.Instance.ResultsArray.ChwAbs.Max();
            ModelCapEhp = DHSimulateDistrictEnergy.Instance.ResultsArray.HwEhp.Max();
            ModelCapBat = DHSimulateDistrictEnergy.Instance.ResultsArray.BatChgN.Max();
            ModelCapHwt = DHSimulateDistrictEnergy.Instance.ResultsArray.TANK_CHG_n.Max();
            ModelCapChpElec = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecChp.Max();
            ModelCapChpHeat = DHSimulateDistrictEnergy.Instance.ResultsArray.HwChp.Max();
            ModelCapNgb = DHSimulateDistrictEnergy.Instance.ResultsArray.NgasNgb.Max();
            ModelCapShw = DHSimulateDistrictEnergy.Instance.ResultsArray.HwShw.Max();
            ModelCapEch = DHSimulateDistrictEnergy.Instance.ResultsArray.ChwEch.Max();
            ModelCapElecProj = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecProj.Max();
            ModelCapPv = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecPv.Max();
            ModelCapWnd = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecWnd.Max();
            ModelAreaShw = DHSimulateDistrictEnergy.Instance.ResultsArray.HwShw.Sum() /
                          (DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.Sum() *
                           (1 - Settings.LossShw) * Settings.UtilShw);
            ModelAreaPv = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecPv.Sum() /
                         (DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.Sum() * Settings.EffPv *
                          (1 - Settings.LossPv) * Settings.UtilPv);
            var windCubed = DHSimulateDistrictEnergy.Instance.DistrictDemand.WindN
                .Where(w => w > Settings.CinWnd && w < Settings.CoutWnd).Select(w => Math.Pow(w, 3))
                .Sum();
            ModelNumWnd = Math.Ceiling(DHSimulateDistrictEnergy.Instance.ResultsArray.ElecWnd.Sum() /
                                      (0.6375 * windCubed * Settings.RotWnd * (1 - Settings.LossWnd) * Settings.CopWnd /
                                       1000)); // Divide by 1000 because equation spits out Wh
            ModelChgrHwt = CapHwt == 0 ? 0 : CapHwt / 12 / Settings.AutHwt; // 12 hours // (AUT_HWT * 12);
            ModelChgrBat = CapBat == 0 ? 0 : CapBat / 12 / Settings.AutBat;
            ModelDchgrHwt = CapHwt == 0
                ? 0
                : CapHwt / 12 /
                  Settings.AutHwt; // (AUT_HWT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
            ModelDchgBat = CapBat == 0
                ? 0
                : CapBat / 12 /
                  Settings.AutBat; // (AUT_BAT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
            ModelCoolingDemand = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Max();
            ModelHeatingDemand = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Max(); 
            ModelElectricityDemand = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Max();
            ModelCoolingEnergy = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Sum();
            ModelHeatingEnergy = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum();
            ModelElectricityEnergy = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum();
        }

        public double ModelElectricityEnergy
        {
            get => _modelElectricityEnergy;
            set
            {
                if (value.Equals(_modelElectricityEnergy)) return;
                _modelElectricityEnergy = value;
                OnPropertyChanged();
            }
        }

        public double ModelHeatingEnergy
        {
            get => _modelHeatingEnergy;
            set
            {
                if (value.Equals(_modelHeatingEnergy)) return;
                _modelHeatingEnergy = value;
                OnPropertyChanged();
            }
        }

        public double ModelCoolingEnergy
        {
            get => _modelCoolingEnergy;
            set
            {
                if (value.Equals(_modelCoolingEnergy)) return;
                _modelCoolingEnergy = value;
                OnPropertyChanged();
            }
        }

        public double ModelElectricityDemand
        {
            get => _modelElectricityDemand;
            set
            {
                if (value.Equals(_modelElectricityDemand)) return;
                _modelElectricityDemand = value;
                OnPropertyChanged();
            }
        }

        public double ModelHeatingDemand
        {
            get => _modelHeatingDemand;
            set
            {
                if (value.Equals(_modelHeatingDemand)) return;
                _modelHeatingDemand = value;
                OnPropertyChanged();
            }
        }

        public double ModelCoolingDemand
        {
            get => _modelCoolingDemand;
            set
            {
                if (value.Equals(_modelCoolingDemand)) return;
                _modelCoolingDemand = value;
                OnPropertyChanged();
            }
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}