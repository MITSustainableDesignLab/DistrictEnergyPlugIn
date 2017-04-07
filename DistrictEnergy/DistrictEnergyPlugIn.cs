using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;

using Rhino.PlugIns;
using Mit.Umi.RhinoServices;
using Rhino;
using Newtonsoft.Json;
using System.Linq;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class DistrictEnergyPlugIn : UmiModule
    {

        private readonly MemoryStream tabHeaderIconStream = new MemoryStream();

        /// <summary>The District Energy PlugIn class</summary>
        public DistrictEnergyPlugIn()
        {
            ModuleControl = new DistrictControl();
            SimulateTabHeaderIconSource = DistrictEnergy.Properties.Resources.TabHeaderIcon.ToBitmap().ToImageSource(ImageFormat.Png, tabHeaderIconStream);
            Instance = this;
        }

        ///<summary>Gets the only instance of the Example2PlugIn plug-in.</summary>
        public static DistrictEnergyPlugIn Instance
        {
            get; private set;
        }

        /// <summary>Gets the GUI</summary>
        protected override UserControl ModuleControl
        {
            get;
        }
        /// <summary>Gets the panel icon</summary>
        protected override ImageSource SimulateTabHeaderIconSource
        {
            get;
        }

        /// <summary>Get's the tooltip content for the panel's tab header</summary>
        protected override string SimulateTabHeaderToolTip
        {
            get
            {
                return "District Energy";
            }
        }

        /// <summary></summary>
        public static DistrictSettings activeSettings
        {
            get; set;
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and mantain plug-in wide options in a document.
        /// <summary>
        /// What happens when Rhino loads the plugIn. +OnActiveProjectSwitched
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            //moduleControl = new DistrictEnergy.ModuleControl();
            GlobalContext.ActiveProjectSwitched += OnActiveProjectSwitched;
            return base.OnLoad(ref errorMessage);

        }

        // The thing to do when a project is saved
        private void OnDocumentSaved(object sender, DocumentSaveEventArgs e)
        {
            DistrictSettings settings = DistrictSettingsViewModel.backing;
            var serialized = JsonConvert.SerializeObject(settings);
            GlobalContext.AuxiliaryFileStore.StoreText(DistrictSettingsPath.SettingsFilePathInBundle, serialized);
        }


        // The thing to do when a new project is loaded
        private void OnActiveProjectSwitched(object sender, ProjectSwitchEventArgs e)
        {
            if (e.NewProject != null)
            {
                var settingsPath = e.NewProject.AuxiliaryFiles.SingleOrDefault(aux => Path.GetFileName(aux) == "districtSettings.json");
                activeSettings = File.Exists(settingsPath) != false
                                    ? JsonConvert.DeserializeObject<DistrictSettings>(File.ReadAllText(settingsPath))
                                    : new DistrictSettings();

                DistrictSettingsViewModel.backing = activeSettings;
                

                // If OldProject is null, then we need to register the save handler so
                // our settings actually get saved
                if (e.OldProject == null)
                {
                    RhinoDoc.EndSaveDocument += OnDocumentSaved;
                }
            }
            else
            {
                // If NewProject is null, then we need to deregister our save handler so
                // that UMI doesn't try to do UMI things when there isn't a project open
                RhinoDoc.EndSaveDocument -= OnDocumentSaved;
            }
        }
    }
}