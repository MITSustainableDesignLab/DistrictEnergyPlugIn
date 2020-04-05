using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {
        public static DistrictSettings DistrictSettings = new DistrictSettings();


        public DistrictSettingsViewModel()
        {
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        public double ElectricityGenerationCost
        {
            get => DistrictSettings.ElectricityGenerationCost;
            set
            {
                DistrictSettings.ElectricityGenerationCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElectricityGenerationCost)));
            }
        }

        public double PriceNaturalGas
        {
            get => DistrictSettings.PriceNaturalGas;
            set
            {
                DistrictSettings.PriceNaturalGas = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceNaturalGas)));
            }
        }

        public double EmissionsElectricGeneration
        {
            get => DistrictSettings.EmissionsElectricGeneration;
            set
            {
                DistrictSettings.EmissionsElectricGeneration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmissionsElectricGeneration)));
            }
        }

        public double LossesTransmission
        {
            get => DistrictSettings.LossesTransmission;
            set
            {
                DistrictSettings.LossesTransmission = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesTransmission)));
            }
        }

        public double LossesHeatHydronic
        {
            get => DistrictSettings.LossesHeatHydronic;
            set
            {
                DistrictSettings.LossesHeatHydronic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesHeatHydronic)));
            }
        }

        public double EfficPowerGen
        {
            get => DistrictSettings.EfficPowerGen;
            set
            {
                DistrictSettings.EfficPowerGen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EfficPowerGen)));
            }
        }

        public ObservableCollection<SimCase> SimCases { get; set; }

        public SimCase ASimCase { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RhinoDoc_EndSaveDocument(object sender, UmiContext e)
        {
            SaveSettings(e);
        }

        private void PopulateFrom(object sender, UmiContext e)
        {
            LoadSettings(e);
            LoadSimCases(e);
        }

        private void LoadSettings(UmiContext context)
        {
            if (context == null) return;
            var path = context.AuxiliaryFiles.GetFullPath("districtSettings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                DistrictSettings = JsonConvert.DeserializeObject<DistrictSettings>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistrictSettings)));
        }

        private void SaveSettings(UmiContext e)
        {
            var context = e;

            if (context == null) return;

            var dSjson = JsonConvert.SerializeObject(DistrictSettings);
            context.AuxiliaryFiles.StoreText("districtSettings.json", dSjson);
        }

        private void LoadSimCases(UmiContext context)
        {
            if (context == null) return;
            SimCases = new ObservableCollection<SimCase>
            {
                new SimCase {Id = 1, DName = "Net Zero Community"},
                new SimCase {Id = 2, DName = "Business As Usual"},
                new SimCase {Id = 3, DName = "All Gas"}
            };
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SimCases)));
        }
    }
}