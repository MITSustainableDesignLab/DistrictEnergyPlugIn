using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

using Rhino.PlugIns;

using Mit.Umi.Core;
using Mit.Umi.RhinoServices;

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
    public class DistrictEnergyPlugIn : Mit.Umi.RhinoServices.UmiModule
    {
        private UserControl moduleControl;

        private readonly MemoryStream tabHeaderIconStream = new MemoryStream();

        public DistrictEnergyPlugIn()
        {
            //ModuleControl = new ModuleControl();
            SimulateTabHeaderIconSource = DistrictEnergy.Properties.Resources.TabHeaderIcon.ToBitmap().ToImageSource(ImageFormat.Png, tabHeaderIconStream);
            Instance = this;
        }

        ///<summary>Gets the only instance of the Example2PlugIn plug-in.</summary>
        public static DistrictEnergyPlugIn Instance
        {
            get; private set;
        }

        protected override UserControl ModuleControl
        {
            get
            {
                return moduleControl;
            }
        }

        protected override ImageSource SimulateTabHeaderIconSource
        {
            get;
        }

        protected override string SimulateTabHeaderToolTip
        {
            get
            {
                return "District Energy";
            }
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and mantain plug-in wide options in a document.
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            moduleControl = new DistrictEnergy.ModuleControl();
            return base.OnLoad(ref errorMessage);
        }
    }
}