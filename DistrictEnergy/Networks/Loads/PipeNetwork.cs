﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using Deedle.Vectors;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts.Defaults;
using Umi.Core;

namespace DistrictEnergy.Networks.Loads
{
    internal class PipeNetwork : BaseLoad
    {
        public PipeNetwork(LoadTypes loadType, string name)
        {
            LoadType = loadType;
            Fill = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            if (name != null) Name = name;
        }

        /// <summary>
        ///     Relative distribution losses (%)
        /// </summary>
        public double RelDistCoolLoss { get; set; } = 0.05;

        /// <summary>
        ///     Should distribution heat losses be accounted for. yes = 1, no = 0
        /// </summary>
        public bool UseDistrictLosses { get; set; } = false;

        public override SolidColorBrush Fill { get; set; }

        [DataMember]
        [DefaultValue("Distribution Pipes")]
        public override string Name { get; set; } = "Distribution Losses";

        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            Input = new double[8760];
        }

        public override double[] Input
        {
            get => CalcInput();
            set { }
        }

        private double[] CalcInput()
        {
            var final = new double[8760];
            if (UseDistrictLosses)
            {
                foreach (var loads in DistrictControl.Instance.ListOfDistrictLoads.OfType<IBaseLoad>()
                    .Where(o => o.LoadType == LoadType))
                {
                    var adjustedLoad = loads.Input.Select(x => x * RelativeLoss).ToArray(); 
                    for (int i = 0; i < final.Length; i++)
                    {
                        final[i] += adjustedLoad[i];
                    }
                }
            }

            return final;
        }

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; }
        public double RelativeLoss { get; set; }
    }
}