using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DistrictEnergy.Annotations;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class SummaryViewModel : INotifyPropertyChanged
    {
        private double _userDefinedAreaPv;
        public event PropertyChangedEventHandler PropertyChanged;

        public SummaryViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
        }

        public static SummaryViewModel Instance { get; set; }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += CalculateConstants;
            ChilledWaterViewModel.Instance.PropertyChanged += CalculateConstants;
            CombinedHeatAndPowerViewModel.Instance.PropertyChanged += CalculateConstants;
            ElectricGenerationViewModel.Instance.PropertyChanged += CalculateConstants;
            HotWaterViewModel.Instance.PropertyChanged += CalculateConstants;
            NetworkViewModel.Instance.PropertyChanged += CalculateConstants;
        }

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
        ///     Capacity in hot water production (max over the year)
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
            get { return _capPv; }
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
            get { return _capWnd; }
            set
            {
                _capWnd = value;
                OnPropertyChanged();
            }
        }

        public double RelDistHeatLoss
        {
            get { return _relDistHeatLoss; }
            set
            {
                _relDistHeatLoss = value;
                OnPropertyChanged();
            }
        }

        public double RelDistCoolLoss
        {
            get { return _relDistCoolLoss; }
            set
            {
                _relDistHeatLoss = value;
                OnPropertyChanged();
            }
        }

        private void CalculateConstants(object sender, EventArgs e)
        {
            CalculateConstants();
        }

        /// <summary>
        ///     Calculates the necessary constants used in different equations
        /// </summary>
        public void CalculateConstants()
        {
            CapAbs = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Max() * Settings.OffAbs;
            CapEhp = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Max() * Settings.OffEhp;
            CapBat = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Average() * Settings.AutBat * 24;
            CapHwt = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Average() * Settings.AutHwt *
                     24; // todo Prendre jour moyen du mois max.
            CapChpElec = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Max() * Settings.OffChp;
            CapChpHeat = DHSimulateDistrictEnergy.Instance.ResultsArray.HwChp.Max();
            CapNgb = DHSimulateDistrictEnergy.Instance.ResultsArray.NgasNgb.Max();
            CapShw = DHSimulateDistrictEnergy.Instance.ResultsArray.HwShw.Max();
            CapEch = DHSimulateDistrictEnergy.Instance.ResultsArray.ChwEch.Max();
            CapElecProj = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecProj.Max();
            CapPv = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecPv.Max();
            CapWnd = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecWnd.Max();
            AreaShw = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum() * Settings.OffShw /
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


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}