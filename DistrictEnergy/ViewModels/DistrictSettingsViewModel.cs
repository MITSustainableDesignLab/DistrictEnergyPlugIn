using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using Rhino;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {
        public DistrictSettingsViewModel()
        {
            RhinoDoc.EndSaveDocument += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        public static DistrictSettings DistrictSettings = new DistrictSettings();

        private void RhinoDoc_EndSaveDocument(object sender, DocumentSaveEventArgs e)
        {
            SaveSettings();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void PopulateFrom(object sender, UmiContext e)
        {
            LoadSettings(e);
        }

        private void LoadSettings(UmiContext context)
        {
            if (context == null) { return; }
            var path = context.AuxiliaryFiles.GetFullPath("districtSettings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                DistrictSettings = JsonConvert.DeserializeObject<DistrictSettings>(json);
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(String.Empty));
        }

        private void SaveSettings()
        {
            var context = UmiContext.Current;

            if (context == null)
            {
                return;
            }

            var dSjson = JsonConvert.SerializeObject(DistrictSettings);
            context.AuxiliaryFiles.StoreText("districtSettings.json", dSjson);

        }

        public double ElectricityGenerationCost
        {
            get { return DistrictSettings.ElectricityGenerationCost; }
            set
            {
                DistrictSettings.ElectricityGenerationCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElectricityGenerationCost)));
            }
        }

        public double PriceNaturalGas
        {
            get { return DistrictSettings.PriceNaturalGas; }
            set
            {
                DistrictSettings.PriceNaturalGas = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceNaturalGas)));
            }
        }

        public double EmissionsElectricGeneration
        {
            get { return DistrictSettings.EmissionsElectricGeneration; }
            set
            {
                DistrictSettings.EmissionsElectricGeneration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmissionsElectricGeneration)));
            }
        }

        public double LossesTransmission
        {
            get { return DistrictSettings.LossesTransmission; }
            set
            {
                DistrictSettings.LossesTransmission = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesTransmission)));
            }
        }

        public double LossesHeatHydronic
        {
            get { return DistrictSettings.LossesHeatHydronic; }
            set
            {
                DistrictSettings.LossesHeatHydronic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesHeatHydronic)));
            }
        }

        public double EfficPowerGen
        {
            get { return DistrictSettings.EfficPowerGen; }
            set
            {
                DistrictSettings.EfficPowerGen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EfficPowerGen)));
            }
        }

    }
}
