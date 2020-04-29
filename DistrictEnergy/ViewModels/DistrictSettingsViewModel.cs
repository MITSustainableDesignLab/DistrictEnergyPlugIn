using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using Newtonsoft.Json;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {
        public DistrictSettingsViewModel()
        {
            UmiEventSource.Instance.ProjectSaving += RhinoDoc_EndSaveDocument;
            UmiEventSource.Instance.ProjectOpened += PopulateFrom;
        }

        public ObservableCollection<SimCase> SimCases
        {
            get => DistrictControl.DistrictSettings.SimCases;
            set
            {
                DistrictControl.DistrictSettings.SimCases = value;
                OnPropertyChanged();
            }
        }

        public SimCase ASimCase
        {
            get => DistrictControl.DistrictSettings.ASimCase;
            set
            {
                if (Equals(value, DistrictControl.DistrictSettings.ASimCase = value)) return;
                DistrictControl.DistrictSettings.ASimCase = value;
                OnPropertyChanged();
            }
        }

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
                DistrictControl.DistrictSettings = JsonConvert.DeserializeObject<DistrictSettings>(json);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistrictSettings)));
        }

        private void SaveSettings(UmiContext e)
        {
            var context = e;

            if (context == null) return;

            var dSjson = JsonConvert.SerializeObject(DistrictControl.DistrictSettings);
            context.AuxiliaryFiles.StoreText("districtSettings.json", dSjson);
        }

        private void LoadSimCases(UmiContext context)
        {
            if (context == null) return;
            SimCases = new ObservableCollection<SimCase>
            {
                new SimCase {Id = 1, DName = "Net Zero Community"},
                new SimCase {Id = 2, DName = "Business As Usual"},
                new SimCase {Id = 3, DName = "TriGeneration (all gas)"}
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}