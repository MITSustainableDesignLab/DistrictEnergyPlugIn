using Mit.Umi.RhinoServices;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace DistrictEnergy.ViewModels
{
    public class PlanningSettingsViewModel : INotifyPropertyChanged
    {

        public static PlanningSettings backing = new PlanningSettings();


        public PlanningSettingsViewModel()
        {
            GlobalContext.ActiveProjectSwitched += (s, e) => PopulateFrom(e.NewProject);
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public void PopulateFrom(RhinoProject panel)
        {
            if (panel == null) { return; }
            var settingsPath = panel.AuxiliaryFiles.SingleOrDefault(aux => Path.GetFileName(aux) == "planingSettings.json");
            backing = File.Exists(settingsPath) != false
                                ? JsonConvert.DeserializeObject<PlanningSettings>(File.ReadAllText(settingsPath))
                                : new PlanningSettings();
            PropertyChanged(this, new PropertyChangedEventArgs(String.Empty));
        }

        public double C1
        {
            get { return backing.C1; }
            set
            {
                backing.C1 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(C1)));
            }
        }

        public double C2
        {
            get { return backing.C2; }
            set
            {
                backing.C2 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(C2)));
            }
        }

        public double AnnuityFactor
        {
            get { return backing.AnnuityFactor; }
            set
            {
                backing.AnnuityFactor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnnuityFactor)));
            }
        }

        public double PumpEfficiency
        {
            get { return backing.PumpEfficiency; }
            set
            {
                backing.PumpEfficiency = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PumpEfficiency)));
            }
        }
    }
}
