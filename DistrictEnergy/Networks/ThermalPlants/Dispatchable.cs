﻿using System.Linq;
using System.Windows.Media;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class Dispatchable : NotStorage
    {
        /// <summary>
        ///     Carbon intensity of the Energy Module (g CO2 eq/kWh) in terms of the primary energy use.
        /// </summary>
        public abstract double CarbonIntensity { get; set; }
    }

    public class FixedCost : GraphCost
    {
        /// <summary>
        /// </summary>
        /// <param name="plant"></param>
        /// <param name="alpha">uses a default value of 255 for the alpha channel (opaque).</param>
        /// <param name="cost"></param>
        public FixedCost(IThermalPlantSettings plant, byte alpha = 255)
        {
            var color = plant.Fill[plant.OutputType].Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Fixed Cost";
            if (plant.Output != null)
                Cost = plant.Capacity * DistrictControl.PlanningSettings.AnnuityFactor * plant.F;
        }

        public FixedCost(Exportable plant, byte alpha = 255)
        {
            var color = plant.Fill[plant.OutputType].Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Fixed Cost";
            if (plant.Output != null)
                Cost = plant.Capacity * DistrictControl.PlanningSettings.AnnuityFactor * plant.F;
        }

        public FixedCost(Storage storage, byte alpha = 255)
        {
            var color = storage.Fill[storage.OutputType].Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = storage.Name + " Fixed Cost";
            if (storage.CapacityFactor > 0)
                Cost = storage.Capacity * 0.3 / (storage.CapacityFactor * 24) * DistrictControl.PlanningSettings.AnnuityFactor * storage.F;
        }
    }

    public class VariableCost : GraphCost
    {
        /// <summary>
        /// </summary>
        /// <param name="plant"></param>
        /// <param name="alpha">uses a default value of 255 for the alpha channel (opaque).</param>
        /// <param name="cost"></param>
        public VariableCost(IThermalPlantSettings plant, byte alpha = 255)
        {
            var color = plant.Fill[plant.OutputType].Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Variable Cost";
            if (plant.Output != null)
                Cost = plant.Output.Sum() * plant.V;
        }

        public VariableCost(Exportable plant, byte alpha = 255)
        {
            var color = plant.Fill[plant.OutputType].Color;
            Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Name = plant.Name + " Variable Cost";
            if (plant.Output != null)
                Cost = plant.Output.Sum() * plant.V;
        }
    }

    public class GraphCost
    {
        public SolidColorBrush Fill { get; set; }
        public double Cost { get; set; }
        public string Name { get; set; }
    }
}