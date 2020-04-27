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
                typeof(PipeNetwork),
                typeof(GridElectricity),
                typeof(GridGas),
                typeof(CustomEnergySupplyModule)
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
                DistrictControl.Instance.ListOfPlantSettings = JsonConvert.DeserializeObject<ObservableCollection<IThermalPlantSettings>>(json,
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
            var dSjson = JsonConvert.SerializeObject(DistrictControl.Instance.ListOfPlantSettings, Formatting.Indented,
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
    }
}