using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Context;
using DistrictEnergy.Networks;
using DistrictEnergy.NetworkSettings;
using DistrictEnergy.Services;
using Rhino;
using Rhino.DocObjects;

namespace DistrictEnergy.ViewModels
{
    public class PlantSettingsViewModel : INotifyPropertyChanged
    {
        private readonly HashSet<Guid> _selectedObjectIds;

        public PlantSettingsViewModel()
        {
            if (ServiceContainer.Instance != null)
            {
                ConnectToServices(ServiceContainer.Instance);
            }
            else
            {
                ServiceContainer.Init();
                ConnectToServices(ServiceContainer.Instance);
            }

            PropertyChanged += PlantSettingsViewModel_PropertyChanged;
        }

        private IEnumerable<RhinoObject> SelectedObjects
        {
            get
            {
                if (CurrentContext == null)
                    return Enumerable.Empty<RhinoObject>();
                var selectedObjectIds = _selectedObjectIds;
                var objects = RhinoDoc.ActiveDoc.Objects;
                Func<Guid, Guid>
                    func1 = id => id;
                var func2 = (Func<RhinoObject, Guid>) (o => o.Id);
                var outerKeySelector = func1;
                var innerKeySelector = func2;
                return selectedObjectIds.Join(objects, outerKeySelector, innerKeySelector, (_, o) => o);
            }
        }

        private PluginContext CurrentContext
        {
            get
            {
                var contextService = ContextService;
                if (contextService != null)
                    return contextService.Context;
                return contextService.Context;
            }
        }

        internal IPluginContextService ContextService { get; set; }
        internal PlugInEventSource PlugInEventSource { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private void PlantSettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || CurrentContext == null)
                return;
            OnSettingChangedByUser();
        }

        private void OnSettingChangedByUser()
        {
            CurrentContext.UpdatePlants(SelectedObjects);
        }

        public void ConnectToServices(ServiceContainer container)
        {
            ContextService = container.ContextService;
            PlugInEventSource = container.EventSource;
            if (PlugInEventSource == null)
                return;
            PlugInEventSource.ProjectOpened +=
                (EventHandler<PluginContext>) ((s, e) => PropertyChanged(this, new PropertyChangedEventArgs(null)));
            PlugInEventSource.ProjectClosed +=
                (EventHandler) ((s, e) => PropertyChanged(this, new PropertyChangedEventArgs(null)));
        }

        public DisplaySetting<double> Capacity
        {
            get
            {
                var currentContext = CurrentContext;
                return DisplaySetting<double>.Get(currentContext !=null ? currentContext.PlantSettings : null, 
                    s => s.Capacity, _selectedObjectIds);
            }
            set
            {
                SetSettings(s => s.Capacity = value.CurrentValue);
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Capacity)));
            }
        }

        // TODO : This one is also is temporary and should have a template in the future
        private void SetSettings(Action<PlugInPlantSettings> setter)
        {
            if (CurrentContext == null)
                return;
            new NetworkSettingsUpdater().ApplyUpdateIfMissing(setter, SelectedObjects, CurrentContext.PlantSettings, CurrentContext.ThermalPlants);
        }
    }
}