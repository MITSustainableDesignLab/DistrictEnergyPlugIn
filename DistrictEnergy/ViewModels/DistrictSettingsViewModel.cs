using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using Newtonsoft.Json;
using Rhino;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {
        private Visibility _isDialogBoxVisible;

        public DistrictSettingsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        public ObservableCollection<SimCase> SimCases
        {
            get => DistrictControl.Instance.Scenarios;
            set
            {
                DistrictControl.Instance.Scenarios = value;
                OnPropertyChanged();
            }
        }

        public SimCase ASimCase
        {
            get => DistrictControl.Instance.ASimCase;
            set
            {
                if (Equals(value, DistrictControl.Instance.ASimCase = value)) return;
                DistrictControl.Instance.ASimCase = value;
                OnPropertyChanged();
            }
        }

        public static string InputScenarioName { get; set; }

        public Visibility IsDialogBoxVisible
        {
            get => _isDialogBoxVisible;
            set
            {
                if (value == _isDialogBoxVisible) return;
                _isDialogBoxVisible = value;
                OnPropertyChanged();
            }
        }

        public static DistrictSettingsViewModel Instance { get; set; }

        private void RhinoDoc_EndSaveDocument(object sender, UmiContext e)
        {
            SaveSettings(e);
        }

        private void PopulateFrom(object sender, UmiContext e)
        {
            IsDialogBoxVisible = Visibility.Collapsed;
            SimCases.Clear();
            LoadSettings(e);
            // LoadSimCases(e);
        }

        private void LoadSettings(UmiContext context)
        {
            if (context == null) return;
            var path = context.AuxiliaryFiles.GetFullPath("district_energy_scenarios.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                try
                {
                    DistrictControl.Instance.Scenarios = JsonConvert.DeserializeObject<ObservableCollection<SimCase>>(json, new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Populate,
                        TypeNameHandling = TypeNameHandling.Objects,
                        SerializationBinder = _knownTypesBinder
                    });
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine(e.Message);
                }
            }
            OnPropertyChanged(nameof(Instance.SimCases));
        }

        private void SaveSettings(UmiContext e)
        {
            var context = e;

            if (context == null) return;

            var dSjson = JsonConvert.SerializeObject(DistrictControl.Instance.Scenarios, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    SerializationBinder = _knownTypesBinder
                });
            context.AuxiliaryFiles.StoreText("district_energy_scenarios.json", dSjson);
        }

        private static readonly PlantSettingsViewModel.KnownTypesBinder _knownTypesBinder = new PlantSettingsViewModel.KnownTypesBinder
        {
            KnownTypes = new List<Type>
            {
                typeof(ObservableCollection<SimCase>),
                typeof(SimCase),
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}