﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;
using DistrictEnergy.Views.DistrictSettings;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.Views.ResultViews
{
    /// <summary>
    /// Interaction logic for Demands.xaml
    /// </summary>
    public partial class Demands : UserControl
    {
        public Demands()
        {
            InitializeComponent();
            DataContext = new AnAdditionalLoadViewModel();
            UmiEventSource.Instance.ProjectClosed += ClearThis;
        }
        /// <summary>
        /// On project closed, clear the Parent StackPanel, except the buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearThis(object sender, EventArgs e)
        {

            StackOfAdditionalLoads.Children.Clear();
            
        }

        /// <summary>
        /// Create an AdditionalLoadsView of Type Cooling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddAdditionalLoad_Click(object sender, RoutedEventArgs e)
        {
            AnAdditionalLoadsView newBtn = new AnAdditionalLoadsView(LoadTypes.Cooling);
            StackOfAdditionalLoads.Children.Add(newBtn);
        }

        /// <summary>
        /// Create an AdditionalLoadsView of Type Heating
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddAdditionalHeatingLoad_Click(object sender, RoutedEventArgs e)
        {
            AnAdditionalLoadsView newBtn = new AnAdditionalLoadsView(LoadTypes.Heating);
            StackOfAdditionalLoads.Children.Add(newBtn);
        }

        /// <summary>
        /// Create an AdditionalLoadsView of Type Electricity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddAdditionalElecLoad_Click(object sender, RoutedEventArgs e)
        {
            AnAdditionalLoadsView newBtn = new AnAdditionalLoadsView(LoadTypes.Elec);
            StackOfAdditionalLoads.Children.Add(newBtn);
        }
    }
}
