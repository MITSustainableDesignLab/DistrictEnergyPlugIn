using Mit.Umi.RhinoServices;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {

        public static DistrictSettings backing = new DistrictSettings();


        public DistrictSettingsViewModel()
        {
            GlobalContext.ActiveProjectSwitched += (s, e) => PopulateFrom(e.NewProject);
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public void PopulateFrom(RhinoProject panel)
        {
            if (panel == null) { return; }
            var settingsPath = panel.AuxiliaryFiles.SingleOrDefault(aux => Path.GetFileName(aux) == "districtSettings.json");
            backing = File.Exists(settingsPath) != false
                                ? JsonConvert.DeserializeObject<DistrictSettings>(File.ReadAllText(settingsPath))
                                : new DistrictSettings();
            PropertyChanged(this, new PropertyChangedEventArgs(String.Empty));
        }

        public double ElectricityGenerationCost
        {
            get { return backing.ElectricityGenerationCost; }
            set
            {
                backing.ElectricityGenerationCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElectricityGenerationCost)));
            }
        }

        public double PriceNaturalGas
        {
            get { return backing.PriceNaturalGas; }
            set
            {
                backing.PriceNaturalGas = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceNaturalGas)));
            }
        }

        public double EmissionsElectricGeneration
        {
            get { return backing.EmissionsElectricGeneration; }
            set
            {
                backing.EmissionsElectricGeneration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmissionsElectricGeneration)));
            }
        }

        public double LossesTransmission
        {
            get { return backing.LossesTransmission; }
            set
            {
                backing.LossesTransmission = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesTransmission)));
            }
        }

        public double LossesHeatHydronic
        {
            get { return backing.LossesHeatHydronic; }
            set
            {
                backing.LossesHeatHydronic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesHeatHydronic)));
            }
        }

        public double EfficPowerGen
        {
            get { return backing.EfficPowerGen; }
            set
            {
                backing.EfficPowerGen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EfficPowerGen)));
            }
        }

    }
}
