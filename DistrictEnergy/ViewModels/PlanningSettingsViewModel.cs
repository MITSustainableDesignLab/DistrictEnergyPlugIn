using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using Newtonsoft.Json;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class PlanningSettingsViewModel : INotifyPropertyChanged
    {
        public PlanningSettingsViewModel()
        {
            Instance = this;
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

            UseDistrictLosses = DistrictControl.PlanningSettings.UseDistrictLosses;
            RelDistCoolLoss = DistrictControl.PlanningSettings.RelDistCoolLoss * 100;
            RelDistHeatLoss = DistrictControl.PlanningSettings.RelDistHeatLoss * 100;
            OnPropertyChanged(string.Empty);
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

        public bool UseDistrictLosses
        {
            get
            {
                if (DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>().Select(o => o.UseDistrictLosses).Any(b => b))
                    return true;
                return false;
            }
            set
            {
                foreach (var pipeNetwork in DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>())
                {
                    pipeNetwork.UseDistrictLosses = value;
                }

                DistrictControl.PlanningSettings.UseDistrictLosses = value;
                OnPropertyChanged();
            }
        }

        public double RelDistHeatLoss
        {
            get => DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>().Where(x => x.LoadType == LoadTypes.Heating).Select(o => o.RelativeLoss).Average() * 100;
            set
            {
                foreach (var pipeNetwork in DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>()
                    .Where(x => x.LoadType == LoadTypes.Heating))
                {
                    pipeNetwork.RelativeLoss = value / 100;
                }

                DistrictControl.PlanningSettings.RelDistHeatLoss = value / 100;
                OnPropertyChanged();
            }
        }

        public double RelDistCoolLoss
        {
            get => DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>()
                .Where(x => x.LoadType == LoadTypes.Cooling).Select(o => o.RelDistCoolLoss).Average() * 100;
            set
            {
                foreach (var pipeNetwork in DistrictControl.Instance.ListOfDistrictLoads.OfType<PipeNetwork>()
                    .Where(x => x.LoadType == LoadTypes.Cooling))
                {
                    pipeNetwork.RelativeLoss = value / 100;
                }

                DistrictControl.PlanningSettings.RelDistCoolLoss = value / 100;
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

        /// <summary>
        /// "12, 15, 20, 24, 30, 40, 60, 73, 120, 146, 219, 292, 365, 438, 584, 730, 876, 1095, 1460, 1752, 2190, 2920, 4380, 8760"
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
        public static PlanningSettingsViewModel Instance { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly List<int> availableTimeSteps = new List<int>()
        {
            12, 15, 20, 24, 30, 40, 60, 73, 120, 146, 219, 292, 365, 438, 584, 730, 876, 1095, 1460, 1752, 2190, 2920, 4380, 8760
        };

    }
}