using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using DistrictEnergy.Networks.ThermalPlants;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rhino;

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
            RhinoDoc.EndSaveDocument += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
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
            get => ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB;
            set
            {
                ListOfPlantSettings.OfType<NatGasBoiler>().First().EFF_NGB = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_NGB)));
            }
        }

        #endregion

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
                ListOfPlantSettings = JsonConvert.DeserializeObject<List<IThermalPlantSettings>>(json, new JsonSerializerSettings
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
            var dSjson = JsonConvert.SerializeObject(ListOfPlantSettings, Formatting.Indented, new JsonSerializerSettings
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

        #region AbsorptionChiller

        public double OFF_ABS
        {
            get => ListOfPlantSettings.OfType<AbsorptionChiller>().First().OFF_ABS;
            set
            {
                ListOfPlantSettings.OfType<AbsorptionChiller>().First().OFF_ABS = value;
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
            get => ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT;
            set
            {
                ListOfPlantSettings.OfType<BatteryBank>().First().LOSS_BAT = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_BAT)));
            }
        }

        #endregion

        #region CombinedHeatNPower
        public IList<TrakingModeEnum> PosibleTrackingModes
        {
            get
            {
                // Will result in a list like {"Electric", "Thermal"}
                return Enum.GetValues(typeof(TrakingModeEnum)).Cast<TrakingModeEnum>().ToList<TrakingModeEnum>();
            }
        }
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
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().OFF_CHP = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_CHP)));
            }
        }

        public double EFF_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().EFF_CHP = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_CHP)));
            }
        }

        public double HREC_CHP
        {
            get => ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP;
            set
            {
                ListOfPlantSettings.OfType<CombinedHeatNPower>().First().HREC_CHP = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HREC_CHP)));
            }
        }

        #endregion

        #region ElectricHeatPump

        public double OFF_EHP
        {
            get => ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP;
            set
            {
                ListOfPlantSettings.OfType<ElectricHeatPump>().First().OFF_EHP = value;
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

        public double LOSS_HWT
        {
            get => ListOfPlantSettings.OfType<HotWaterStorage>().First().LOSS_HWT;
            set
            {
                ListOfPlantSettings.OfType<HotWaterStorage>().First().LOSS_HWT = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_HWT)));
            }
        }

        #endregion

        #region PhotovoltaicArray

        public double OFF_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().OFF_PV;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().OFF_PV = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_PV)));
            }
        }

        public double EFF_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().EFF_PV = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_PV)));
            }
        }

        public double UTIL_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().UTIL_PV = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UTIL_PV)));
            }
        }

        public double LOSS_PV
        {
            get => ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV;
            set
            {
                ListOfPlantSettings.OfType<PhotovoltaicArray>().First().LOSS_PV = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_PV)));
            }
        }

        #endregion

        #region SolarThermalCollector

        public double EFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().EFF_SHW = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EFF_SHW)));
            }
        }

        public double OFF_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().OFF_SHW = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_SHW)));
            }
        }

        public double UTIL_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().UTIL_SHW = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UTIL_SHW)));
            }
        }

        public double LOSS_SHW
        {
            get => ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW;
            set
            {
                ListOfPlantSettings.OfType<SolarThermalCollector>().First().LOSS_SHW = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LOSS_SHW)));
            }
        }

        #endregion

        #region WindTurbines

        public double OFF_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().OFF_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().OFF_WND = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OFF_WND)));
            }
        }

        public double COP_WND
        {
            get => ListOfPlantSettings.OfType<WindTurbine>().First().COP_WND;
            set
            {
                ListOfPlantSettings.OfType<WindTurbine>().First().COP_WND = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(COP_WND)));
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

        #endregion
    }
}