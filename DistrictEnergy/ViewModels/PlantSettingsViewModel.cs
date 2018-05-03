using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using DistrictEnergy.Networks.ThermalPlants;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rhino;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class PlantSettingsViewModel : INotifyPropertyChanged
    {
        public static List<IThermalPlantSettings> ListOfPlantSettings = new List<IThermalPlantSettings>
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
            new WindTurbine()
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
                typeof(WindTurbine)
            }
        };

        public PlantSettingsViewModel()
        {
            Instance = this;
            RhinoDoc.EndSaveDocument += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        public static PlantSettingsViewModel Instance { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RhinoDoc_EndSaveDocument(object sender, DocumentSaveEventArgs e)
        {
            SaveSettings();
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
                ListOfPlantSettings = JsonConvert.DeserializeObject<List<IThermalPlantSettings>>(json,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        SerializationBinder = _knownTypesBinder
                    });
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void SaveSettings()
        {
            var context = UmiContext.Current;

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CCOP_ECH)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_NGB)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_ABS)));
            }
        }

        public double CCOP_ABS
        {
            get => ListOfPlantSettings.OfType<AbsorptionChiller>().First().CCOP_ABS;
            set
            {
                ListOfPlantSettings.OfType<AbsorptionChiller>().First().CCOP_ABS = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CCOP_ABS)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AUT_BAT)));
            }
        }

        public double LOSS_BAT
        {
            get => ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT * 100;
            set
            {
                ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_BAT)));
            }
        }

        public double BAT_START
        {
            get => ListOfPlantSettings.OfType<BatteryBank>().First().BAT_START * 100;
            set
            {
                ListOfPlantSettings.OfType<BatteryBank>().First().BAT_START = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BAT_START)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TMOD_CHP)));
            }
        }

        public double OFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_CHP)));
            }
        }

        public double EFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_CHP)));
            }
        }

        public double HREC_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP * 100;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HREC_CHP)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_EHP)));
            }
        }

        public double HCOP_EHP
        {
            get => ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP;
            set
            {
                ListOfPlantSettings.OfType<ElectricHeatPump>().First().HCOP_EHP = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HCOP_EHP)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AUT_HWT)));
            }
        }

        public double TANK_START
        {
            get => ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START * 100;
            set
            {
                ListOfPlantSettings.OfType<HotWaterStorage>().First().TANK_START = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TANK_START)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_PV)));
            }
        }

        public double EFF_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_PV)));
            }
        }

        public double UTIL_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UTIL_PV)));
            }
        }

        public double LOSS_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV * 100;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_PV)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_SHW)));
            }
        }

        public double OFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_SHW)));
            }
        }

        public double UTIL_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UTIL_SHW)));
            }
        }

        public double LOSS_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW * 100;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_SHW)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_WND)));
            }
        }

        public double EFF_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().EFF_WND * 100;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().EFF_WND = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_WND)));
            }
        }

        public double CIN_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().CIN_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().CIN_WND = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CIN_WND)));
            }
        }

        public double COUT_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().COUT_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().COUT_WND = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(COUT_WND)));
            }
        }

        public double ROT_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().ROT_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().ROT_WND = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ROT_WND)));
            }
        }

        public double LOSS_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().LOSS_WND * 100;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().LOSS_WND = value / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_WND)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseEhpEvap)));
                }
                else
                {
                    ListOfPlantSettings.OfType<ElectricHeatPump>().First().UseEhpEvap = 0;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseEhpEvap)));
                }
                
            }
        }

        #endregion
    }
}