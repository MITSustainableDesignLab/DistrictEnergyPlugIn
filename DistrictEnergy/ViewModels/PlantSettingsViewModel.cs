using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Networks.ThermalPlants;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class PlantSettingsViewModel : INotifyPropertyChanged
    {
        public static ObservableCollection<IThermalPlantSettings> ListOfPlantSettings =
            new ObservableCollection<IThermalPlantSettings>
            {
                new AbsorptionChiller(),
                new BatteryBank(),
                new CombinedHeatNPower(),
                new ElectricChiller(),
                new ElectricHeatPump(),
                new HotWaterStorage(),
                new NatGasBoiler(),
                new PhotovoltaicArray(),
                new SolarThermalCollector(),
                new WindTurbine(),
                new PipeNetwork()
            };

        private readonly KnownTypesBinder _knownTypesBinder = new KnownTypesBinder
        {
            KnownTypes = new List<Type>
            {
                typeof(AbsorptionChiller),
                typeof(BatteryBank),
                typeof(CombinedHeatNPower),
                typeof(ElectricChiller),
                typeof(ElectricHeatPump),
                typeof(HotWaterStorage),
                typeof(NatGasBoiler),
                typeof(PhotovoltaicArray),
                typeof(SolarThermalCollector),
                typeof(WindTurbine),
                typeof(PipeNetwork)
            }
        };

        public PlantSettingsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        public static PlantSettingsViewModel Instance { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void RhinoDoc_EndSaveDocument(object sender, UmiContext e)
        {
            SaveSettings(e);
        }

        private void PopulateFrom(object sender, UmiContext e)
        {
            try
            {
                LoadSettings(e);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                //throw new ArgumentException("A project settings viewmodel cannot be instantiated from a project with no instantiated settings object");
            }
        }

        private void LoadSettings(UmiContext context)
        {
            if (context == null) return;
            var path = context.AuxiliaryFiles.GetFullPath("ThermalPlantSettings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                ListOfPlantSettings = JsonConvert.DeserializeObject<ObservableCollection<IThermalPlantSettings>>(json,
                    new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Populate,
                        TypeNameHandling = TypeNameHandling.Objects,
                        SerializationBinder = _knownTypesBinder
                    });
            }

            OnPropertyChanged(string.Empty);
        }

        private void SaveSettings(UmiContext e)
        {
            var context = e;

            if (context == null) return;
            var dSjson = JsonConvert.SerializeObject(ListOfPlantSettings, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = _knownTypesBinder
                });
            context.AuxiliaryFiles.StoreText("ThermalPlantSettings.json", dSjson);
        }

        public class KnownTypesBinder : ISerializationBinder
        {
            public IList<Type> KnownTypes { get; set; }

            public Type BindToType(string assemblyName, string typeName)
            {
                return KnownTypes.SingleOrDefault(t => t.Name == typeName);
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }
        }

        #region ElectricChiller

        public double CCOP_ECH
        {
            get => ListOfPlantSettings.OfType<ElectricChiller>().First().CCOP_ECH;
            set
            {
                ListOfPlantSettings.OfType<ElectricChiller>().First().CCOP_ECH = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region NatGasBoiler

        public double EFF_NGB
        {
            get => ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB * 100;
            set
            {
                ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB = value / 100;
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

        #endregion

        #region BatteryBank

        public double AUT_BAT
        {
            get => ListOfPlantSettings.OfType<BatteryBank>().First().AUT_BAT;
            set
            {
                ListOfPlantSettings.OfType<BatteryBank>().First().AUT_BAT = value;
                OnPropertyChanged();
            }
        }

        public double LOSS_BAT
        {
            get => ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT * 100;
            set
            {
                ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT = value / 100;
                OnPropertyChanged();
            }
        }

        public double BAT_START
        {
            get => ListOfPlantSettings.OfType<BatteryBank>().First().BAT_START * 100;
            set
            {
                ListOfPlantSettings.OfType<BatteryBank>().First().BAT_START = value / 100;
                OnPropertyChanged();
            }
        }

        #endregion

        #region CombinedHeatNPower

        public IList<TrakingModeEnum> PosibleTrackingModes =>
            Enum.GetValues(typeof(TrakingModeEnum)).Cast<TrakingModeEnum>().ToList();

        public TrakingModeEnum TMOD_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().TMOD_CHP = value;
                OnPropertyChanged();
            }
        }

        public double OFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double HREC_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP = value / 100;
                OnPropertyChanged();
            }
        }

        #endregion

        #region ElectricHeatPump

        public double OFF_EHP
        {
            get => ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP * 100;
            set
            {
                ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP = value / 100;
                OnPropertyChanged();
            }
        }

        public double HCOP_EHP
        {
            get => ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP;
            set
            {
                ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region HotWaterStorage

        public double AUT_HWT
        {
            get => ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT;
            set
            {
                ListOfPlantSettings.OfType<HotWaterStorage>().First().AUT_HWT = value;
                OnPropertyChanged();
            }
        }

        public double TANK_START
        {
            get => ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START * 100;
            set
            {
                ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START = value / 100;
                OnPropertyChanged();
            }
        }

        #endregion

        #region PhotovoltaicArray

        public double OFF_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().OFF_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().OFF_PV = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV = value / 100;
                OnPropertyChanged();
            }
        }

        public double UTIL_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV = value / 100;
                OnPropertyChanged();
            }
        }

        public double LOSS_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV = value / 100;
                OnPropertyChanged();
            }
        }

        #endregion

        #region SolarThermalCollector

        public double EFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public double OFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public double UTIL_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        public double LOSS_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW = value / 100;
                OnPropertyChanged();
            }
        }

        #endregion

        #region WindTurbines

        public double OFF_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().OFF_WND * 100;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().OFF_WND = value / 100;
                OnPropertyChanged();
            }
        }

        public double EFF_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().EFF_WND * 100;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().EFF_WND = value / 100;
                OnPropertyChanged();
            }
        }

        public double CIN_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().CIN_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().CIN_WND = value;
                OnPropertyChanged();
            }
        }

        public double COUT_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().COUT_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().COUT_WND = value;
                OnPropertyChanged();
            }
        }

        public double ROT_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().ROT_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().ROT_WND = value;
                OnPropertyChanged();
            }
        }

        public double LOSS_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().LOSS_WND * 100;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().LOSS_WND = value / 100;
                OnPropertyChanged();
            }
        }

        public bool UseEhpEvap
        {
            get
            {
                if (ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value)
                {
                    ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 1;
                    OnPropertyChanged();
                }
                else
                {
                    ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 0;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Pipe Network

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

        #endregion
    }
}