using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Deedle;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class Dispatchable : IThermalPlantSettings
    {
        public abstract LoadTypes InputType { get; }
        public abstract double F { get; set; }
        public abstract double V { get; set; }
        public abstract double Capacity { get; }
        public abstract string Name { get; set; }
        public abstract Guid Id { get; set; }
        public abstract LoadTypes OutputType { get; }
        public abstract Dictionary<LoadTypes, double> ConversionMatrix { get; }
        public abstract List<DateTimePoint> Input { get; set; }
        public abstract List<DateTimePoint> Output { get; set; }
        public abstract double Efficiency { get; }
        public abstract SolidColorBrush Fill { get; set; }
        public GraphCost FixedCost => new FixedCost(this);
        public GraphCost VariableCost => new VariableCost(this, 200);
        public double TotalCost => FixedCost.Cost + VariableCost.Cost;
        public abstract double CapacityFactor { get; }
        public abstract bool IsForced { get; set; }
    }

    public class FixedCost : GraphCost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plant"></param>
        /// <param name="alpha">uses a default value of 255 for the alpha channel (opaque).</param>
        /// <param name="cost"></param>
        public FixedCost(IThermalPlantSettings plant, byte alpha = 255)
        {
            var color = plant.Fill.Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Fixed Cost";
            if (plant.Output != null)
                Cost = plant.Output.Max() * DistrictControl.PlanningSettings.AnnuityFactor * plant.F /
                       (8760 / DistrictControl.PlanningSettings.TimeSteps);
        }

        public FixedCost(Exportable plant, byte alpha = 255)
        {
            var color = plant.Fill.Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Fixed Cost";
            if (plant.Input != null)
                Cost = plant.Input.Max() * DistrictControl.PlanningSettings.AnnuityFactor * plant.F /
                       (8760 / DistrictControl.PlanningSettings.TimeSteps);
        }
    }

    public class VariableCost : GraphCost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plant"></param>
        /// <param name="alpha">uses a default value of 255 for the alpha channel (opaque).</param>
        /// <param name="cost"></param>
        public VariableCost(IThermalPlantSettings plant, byte alpha = 255)
        {
            var color = plant.Fill.Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Variable Cost";
            if (plant.Output != null)
                Cost = plant.Output.Select(x => x.Value * plant.V).Sum();
        }

        public VariableCost(Exportable plant, byte alpha = 255)
        {
            var color = plant.Fill.Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Variable Cost";
            if (plant.Input != null)
                Cost = plant.Input.Select(x => x * plant.V).Sum();
        }
    }

    public class GraphCost
    {
        public SolidColorBrush Fill { get; set; }
        public double Cost { get; set; }
        public string Name { get; set; }
    }
}