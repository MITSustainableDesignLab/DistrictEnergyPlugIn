using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class PlanningSettingsViewModel : INotifyPropertyChanged
    {
        public static PlanningSettings PlanningSettings = new PlanningSettings();
        public PlanningSettingsViewModel()
        {
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        private void RhinoDoc_EndSaveDocument(object sender, UmiContext e)
        {
            SaveSettings(e);
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
                PlanningSettings = JsonConvert.DeserializeObject<PlanningSettings>(json);
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(String.Empty));
        }

        private void SaveSettings(UmiContext e)
        {
            var context = e;

            if (context == null)
            {
                return;
            }

            var pSjson = JsonConvert.SerializeObject(PlanningSettings);
            context.AuxiliaryFiles.StoreText("planningSettings.json", pSjson);

        }

        public double C1
        {
            get { return PlanningSettings.C1; }
            set
            {
                PlanningSettings.C1 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(C1)));
            }
        }

        public double C2
        {
            get { return PlanningSettings.C2; }
            set
            {
                PlanningSettings.C2 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(C2)));
            }
        }

        public double Rate
        {
            get { return PlanningSettings.Rate; }
            set
            {
                PlanningSettings.Rate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rate)));
                
            }
        }

        public double Periods
        {
            get { return PlanningSettings.Periods; }
            set
            {
                PlanningSettings.Periods = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Periods)));
                
            }
        }

        public double Annuity
        {
            get { return Metrics.Metrics.AnnuityPayment(PlanningSettings.Rate, PlanningSettings.Periods); }
        }

        public double PumpEfficiency
        {
            get { return PlanningSettings.PumpEfficiency; }
            set
            {
                PlanningSettings.PumpEfficiency = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PumpEfficiency)));
            }
        }
    }
}
