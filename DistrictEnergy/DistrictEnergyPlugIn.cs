using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Controls;
using Rhino.PlugIns;
using Umi.RhinoServices;

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
        public DistrictEnergyPlugIn()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the DistrictEnergyPlugIn plug-in.</summary>
        public static DistrictEnergyPlugIn Instance
        {
            get; private set;
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and mantain plug-in wide options in a document.

        // public DistrictControl SimulatePanel { get; set; } = new DistrictControl();

        /// <summary>Gets the GUI</summary>
        protected override UserControl ModuleControl
        {
            get { return new DistrictControl(); }
        }

        protected override string TabHeaderToolTip
        {
            get { return "District Energy"; }
        }

        protected override Tuple<Bitmap, ImageFormat> TabHeaderIcon
        {
            get
            {
                return Tuple.Create(Properties.Resources.DistrictPluginIcon.ToBitmap(), ImageFormat.Png);
            }

        }

        public override PlugInLoadTime LoadTime
        {
            get { return PlugInLoadTime.AtStartup; }
        }
    }
}