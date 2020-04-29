using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using Newtonsoft.Json;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class PlanningSettingsViewModel : INotifyPropertyChanged
    {
        public PlanningSettingsViewModel()
        {
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        private void RhinoDoc_EndSaveDocument(object sender, UmiContext e)
        {
            SaveSettings(e);
        }

        private void PopulateFrom(object sender, UmiContext e)
        {
            LoadSettings(e);
        }

        private void LoadSettings(UmiContext context)
        {
            if (context == null)
            {
                return;
            }

            var path = context.AuxiliaryFiles.GetFullPath("planningSettings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                DistrictControl.PlanningSettings = JsonConvert.DeserializeObject<PlanningSettings>(json);
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

            var pSjson = JsonConvert.SerializeObject(DistrictControl.PlanningSettings);
            context.AuxiliaryFiles.StoreText("planningSettings.json", pSjson);
        }

        public double C1
        {
            get { return DistrictControl.PlanningSettings.C1; }
            set
            {
                DistrictControl.PlanningSettings.C1 = value;
                OnPropertyChanged();
            }
        }

        public double C2
        {
            get { return DistrictControl.PlanningSettings.C2; }
            set
            {
                DistrictControl.PlanningSettings.C2 = value;
                OnPropertyChanged();
            }
        }

        public double Rate
        {
            get { return DistrictControl.PlanningSettings.Rate; }
            set
            {
                DistrictControl.PlanningSettings.Rate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AnnuityFactor));
            }
        }

        public double Periods
        {
            get { return DistrictControl.PlanningSettings.Periods; }
            set
            {
                DistrictControl.PlanningSettings.Periods = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AnnuityFactor));
            }
        }

        public double AnnuityFactor => DistrictControl.PlanningSettings.AnnuityFactor;

        public double PumpEfficiency
        {
            get { return DistrictControl.PlanningSettings.PumpEfficiency; }
            set
            {
                DistrictControl.PlanningSettings.PumpEfficiency = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// "365, 438, 584, 730, 876, 1095, 1460, 1752, 2190, 2920, 4380, 8760"
        /// </summary>
        public int TimeSteps
        {
            get { return availableTimeSteps.IndexOf((int) DistrictControl.PlanningSettings.TimeSteps); }
            set
            {
                DistrictControl.PlanningSettings.TimeSteps = availableTimeSteps[value];
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTimeSteps));
            }
        }

        public int DisplayTimeSteps => availableTimeSteps[TimeSteps];

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly List<int> availableTimeSteps = new List<int>()
        {
            365, 438, 584, 730, 876, 1095, 1460, 1752, 2190, 2920, 4380, 8760
        };

    }
}