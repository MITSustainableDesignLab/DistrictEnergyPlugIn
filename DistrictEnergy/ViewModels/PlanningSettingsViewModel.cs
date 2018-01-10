using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;
using Rhino;

namespace DistrictEnergy.ViewModels
{
    public class PlanningSettingsViewModel : INotifyPropertyChanged
    {
        public PlanningSettingsViewModel()
        {
            RhinoDoc.EndSaveDocument += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        private PlanningSettings _planningSettings = new PlanningSettings();

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
            var path = context.AuxiliaryFiles.GetFullPath("planningSettings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                DistrictEnergyPlugIn.Instance.PlanningSettings = JsonConvert.DeserializeObject<PlanningSettings>(json);
                _planningSettings = DistrictEnergyPlugIn.Instance.PlanningSettings;
            }
            else
            {
                DistrictEnergyPlugIn.Instance.PlanningSettings = new PlanningSettings();
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

            var pSjson = JsonConvert.SerializeObject(DistrictEnergyPlugIn.Instance.PlanningSettings);
            context.AuxiliaryFiles.StoreText("planningSettings.json", pSjson);

        }

        public double C1
        {
            get { return _planningSettings.C1; }
            set
            {
                _planningSettings.C1 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(C1)));
            }
        }

        public double C2
        {
            get { return _planningSettings.C2; }
            set
            {
                _planningSettings.C2 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(C2)));
            }
        }

        public double Rate
        {
            get { return _planningSettings.Rate; }
            set
            {
                _planningSettings.Rate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rate)));
                
            }
        }

        public double Periods
        {
            get { return _planningSettings.Periods; }
            set
            {
                _planningSettings.Periods = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Periods)));
                
            }
        }

        public double Annuity
        {
            get { return Metrics.Metrics.AnnuityPayment(_planningSettings.Rate, _planningSettings.Periods); }
        }

        public double PumpEfficiency
        {
            get { return _planningSettings.PumpEfficiency; }
            set
            {
                _planningSettings.PumpEfficiency = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PumpEfficiency)));
            }
        }
    }
}
