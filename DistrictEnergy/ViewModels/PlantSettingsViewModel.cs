using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
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
        private static readonly KnownTypesBinder _knownTypesBinder = new KnownTypesBinder
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
                typeof(GridElectricity),
                typeof(GridGas),
                typeof(CustomEnergySupplyModule),
                typeof(ElectricityExport),
                typeof(CoolingExport),
                typeof(HeatingExport),
                typeof(Dictionary<LoadTypes, SolidColorBrush>)
            }
        };

        public PlantSettingsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
            // DistrictControl.Instance.SimCaseChanged += PopulateFrom;
        }

        public static PlantSettingsViewModel Instance { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        public void OnPropertyChanged([CallerMemberName] string name = null)
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
                RhinoApp.WriteLine(exception.Message);
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
                DistrictControl.Instance.ListOfPlantSettings = DeserializeFromString(json);

                if (!DistrictControl.Instance.ListOfPlantSettings.OfType<GridElectricity>().Any())
                {
                    DistrictControl.Instance.ListOfPlantSettings.Add(new GridElectricity());
                }

                if (!DistrictControl.Instance.ListOfPlantSettings.OfType<GridGas>().Any())
                {
                    DistrictControl.Instance.ListOfPlantSettings.Add(new GridGas());
                }
                if (!DistrictControl.Instance.ListOfPlantSettings.OfType<ElectricityExport>().Any())
                {
                    DistrictControl.Instance.ListOfPlantSettings.Add(new ElectricityExport());
                }
                if (!DistrictControl.Instance.ListOfPlantSettings.OfType<CoolingExport>().Any())
                {
                    DistrictControl.Instance.ListOfPlantSettings.Add(new CoolingExport());
                }
                if (!DistrictControl.Instance.ListOfPlantSettings.OfType<HeatingExport>().Any())
                {
                    DistrictControl.Instance.ListOfPlantSettings.Add(new HeatingExport());
                }
            }

            OnPropertyChanged(string.Empty);
        }

        public static ObservableCollection<IThermalPlantSettings> DeserializeFromString(string json)
        {
            var plants =
                JsonConvert.DeserializeObject<ObservableCollection<IThermalPlantSettings>>(json,
                    new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Populate,
                        TypeNameHandling = TypeNameHandling.Objects,
                        SerializationBinder = _knownTypesBinder
                    });
            return plants;
        }

        private void SaveSettings(UmiContext e)
        {
            var context = e;

            if (context == null) return;
            var dSjson = SerializeToString(DistrictControl.Instance.ListOfPlantSettings);
            context.AuxiliaryFiles.StoreText("ThermalPlantSettings.json", dSjson);
        }

        public static string SerializeToString(ObservableCollection<IThermalPlantSettings> listOfPlantSettings)
        {
            var dSjson = JsonConvert.SerializeObject(listOfPlantSettings, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = _knownTypesBinder
                });
            return dSjson;
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
    }
}