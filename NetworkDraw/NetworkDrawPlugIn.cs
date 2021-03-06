﻿using System.ComponentModel;
using Rhino.PlugIns;

namespace NetworkDraw
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class NetworkDrawPlugIn : PlugIn
    {
        public NetworkDrawPlugIn()
        {
            Instance = this;
        }

        public static NetworkDrawPlugIn Instance { get; private set; }

        public override PlugInLoadTime LoadTime
        {
            get { return PlugInLoadTime.AtStartup; }
        }
    }

}