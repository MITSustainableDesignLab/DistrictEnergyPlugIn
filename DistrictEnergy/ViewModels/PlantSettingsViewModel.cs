using System;
using System.ComponentModel;
using System.IO;
using DistrictEnergy.Networks.ThermalPlants;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;
using Newtonsoft.Json;

namespace DistrictEnergy.ViewModels
{
    public class PlantSettingsViewModel : INotifyPropertyChanged
    {
        private CombinedHeatNPower backingCombinedHeatNPower = new CombinedHeatNPower();
        private ElectricHeatPump backingElectricHeatPump = new ElectricHeatPump();
        private HotWaterStorage backingHotWaterStorage = new HotWaterStorage();
        private NatGasBoiler backingNatGasBoiler = new NatGasBoiler();
        private SolarThermalCollector backingSolarThermalCollector = new SolarThermalCollector();

        public PlantSettingsViewModel()
        {
            UmiEventSource.Instance.ProjectClosed += (EventHandler) ((s, e) => this.PopulateFrom((UmiContext) null));
            UmiEventSource.Instance.ProjectOpened += (EventHandler<UmiContext>) ((s, e) => this.PopulateFrom(e));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void PopulateFrom(object sender, UmiContext e)
        {
            try
            {
                LoadSettings(e);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw new ArgumentException("A project settings viewmodel cannot be instantiated from a project with no instantiated settings object");
            }
        }

        private void LoadSettings(UmiContext context)
        {
            if (context == null) return;
            var path = context.AuxiliaryFiles.GetFullPath("CombinedHeatNPower.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                backingCombinedHeatNPower = JsonConvert.DeserializeObject<CombinedHeatNPower>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

            path = context.AuxiliaryFiles.GetFullPath("ElectricHeatPump.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                backingElectricHeatPump = JsonConvert.DeserializeObject<ElectricHeatPump>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

            path = context.AuxiliaryFiles.GetFullPath("HotWaterStorage.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                backingHotWaterStorage = JsonConvert.DeserializeObject<HotWaterStorage>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

            path = context.AuxiliaryFiles.GetFullPath("NatGasBoiler.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                backingNatGasBoiler = JsonConvert.DeserializeObject<NatGasBoiler>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

            path = context.AuxiliaryFiles.GetFullPath("SolarThermalCollector.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                backingSolarThermalCollector = JsonConvert.DeserializeObject<SolarThermalCollector>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void SaveSettings()
        {
            var context = UmiContext.Current;

            if (context == null) return;

            var dSjson = JsonConvert.SerializeObject(backingCombinedHeatNPower);
            context.AuxiliaryFiles.StoreText("CombinedHeatNPower.json", dSjson);
            dSjson = JsonConvert.SerializeObject(backingElectricHeatPump);
            context.AuxiliaryFiles.StoreText("ElectricHeatPump.json", dSjson);
            dSjson = JsonConvert.SerializeObject(backingHotWaterStorage);
            context.AuxiliaryFiles.StoreText("HotWaterStorage.json", dSjson);
            dSjson = JsonConvert.SerializeObject(backingNatGasBoiler);
            context.AuxiliaryFiles.StoreText("NatGasBoiler.json", dSjson);
            dSjson = JsonConvert.SerializeObject(backingSolarThermalCollector);
            context.AuxiliaryFiles.StoreText("SolarThermalCollector.json", dSjson);
        }
    }
}