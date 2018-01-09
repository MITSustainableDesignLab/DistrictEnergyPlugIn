using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;
using Rhino;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {

        public DistrictSettingsViewModel()
        {
            RhinoDoc.EndSaveDocument += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        private DistrictSettings _districtSettings = new DistrictSettings();

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
                DistrictEnergyPlugIn.Instance.DistrictSettings = JsonConvert.DeserializeObject<DistrictSettings>(json);
                _districtSettings = DistrictEnergyPlugIn.Instance.DistrictSettings;
            }
            else
            {
                DistrictEnergyPlugIn.Instance.DistrictSettings = new DistrictSettings();
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

            var dSjson = JsonConvert.SerializeObject(_districtSettings);
            context.AuxiliaryFiles.StoreText("districtSettings.json", dSjson);

        }

        public double ElectricityGenerationCost
        {
            get { return _districtSettings.ElectricityGenerationCost; }
            set
            {
                _districtSettings.ElectricityGenerationCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElectricityGenerationCost)));
            }
        }

        public double PriceNaturalGas
        {
            get { return _districtSettings.PriceNaturalGas; }
            set
            {
                _districtSettings.PriceNaturalGas = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceNaturalGas)));
            }
        }

        public double EmissionsElectricGeneration
        {
            get { return _districtSettings.EmissionsElectricGeneration; }
            set
            {
                _districtSettings.EmissionsElectricGeneration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmissionsElectricGeneration)));
            }
        }

        public double LossesTransmission
        {
            get { return _districtSettings.LossesTransmission; }
            set
            {
                _districtSettings.LossesTransmission = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesTransmission)));
            }
        }

        public double LossesHeatHydronic
        {
            get { return _districtSettings.LossesHeatHydronic; }
            set
            {
                _districtSettings.LossesHeatHydronic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesHeatHydronic)));
            }
        }

        public double EfficPowerGen
        {
            get { return _districtSettings.EfficPowerGen; }
            set
            {
                _districtSettings.EfficPowerGen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EfficPowerGen)));
            }
        }

    }
}
