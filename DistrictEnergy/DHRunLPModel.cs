using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DistrictEnergy.Annotations;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using Google.OrTools.LinearSolver;
using Rhino;
using Rhino.Commands;

namespace DistrictEnergy
{
    public class DHRunLPModel : Command
    {
        public DHRunLPModel()
        {
            Instance = this;
        }

        ///<summary>The only instance of the DHRunLPModel command.</summary>
        public static DHRunLPModel Instance { get; private set; }

        public override string EnglishName => "DHRunLPModel";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Main();
            return Result.Success;
        }

        private static Variable[] To1DArray(Variable[,] input)
        {
            // Step 1: get total size of 2D array, and allocate 1D array.
            var size = input.Length;
            var result = new Variable[size];

            // Step 2: copy 2D array elements into a 1D array.
            var write = 0;
            for (var i = 0; i <= input.GetUpperBound(0); i++)
            for (var z = 0; z <= input.GetUpperBound(1); z++)
                result[write++] = input[i, z];

            // Step 3: return the new array.
            return result;
        }

        private void Main()
        {
            DHSimulateDistrictEnergy.Instance.PreSolve();
            // Create the linear solver with the CBC backend.
            var solver = Solver.CreateSolver("SimpleMipProgram", "GLOP_LINEAR_PROGRAMMING");

            // Define Model Variables. Here each variable is the supply power of each available supply module
            int timeSteps = 60; // Number of Time Steps
            int dt = 8760 / timeSteps; // Duration of each Time Steps

            // Input Enerygy
            var P = new Dictionary<(int, IThermalPlantSettings), Variable>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<IDispatchable>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    P[(t, supplymodule)] = solver.MakeNumVar(0.0, supplymodule.Capacity / supplymodule.Efficiency * dt,
                        string.Format($"P_{t}_{supplymodule.Name}"));
                }

                // Add Peak
                /*P[(8760, supplymodule)] = solver.MakeNumVar(0.0, supplymodule.Capacity / supplymodule.Efficiency * 1,
                    string.Format($"P_Peak_{supplymodule.Name}"))*/
                ;
            }

            var Qin = new Dictionary<(int, IThermalPlantSettings), Variable>();
            var Qout = new Dictionary<(int, IThermalPlantSettings), Variable>();
            var S = new Dictionary<(int, IThermalPlantSettings), Variable>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<IStorage>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    Qin[(t, supplymodule)] =
                        solver.MakeNumVar(0.0, supplymodule.MaxChargingRate, $"StoIn_{t}_{supplymodule.Name}");
                    Qout[(t, supplymodule)] =
                        solver.MakeNumVar(0.0, supplymodule.MaxDischargingRate, $"StoOut_{t}_{supplymodule.Name}");
                    S[(t, supplymodule)] =
                        solver.MakeNumVar(0.0, supplymodule.Capacity, $"StoState_{t}_{supplymodule.Name}");
                }
            }

            // Exports (per supply module)
            var E = new Dictionary<(int, LoadTypes), Variable>();
            for (var t = 0; t < timeSteps * dt; t += dt)
            {
                E[(t, LoadTypes.Elec)] = solver.MakeNumVar(0.0, double.PositiveInfinity, $"Export{t}_Electricity");
            }

            //Exp[(8760, LoadTypes.Elec)] = solver.MakeNumVar(0.0, double.PositiveInfinity, $"Peak_Export_Electricity");

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            var cooling = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN;
            var heating = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN;
            var electricity = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN;

            // Set Load Balance Constraints
            foreach (LoadTypes loadTypes in Enum.GetValues(typeof(LoadTypes)))
            {
                if (loadTypes == LoadTypes.Cooling)
                {
                    var pipe = DistrictControl.Instance.ListOfPlantSettings
                        .Where(o => o.OutputType == LoadTypes.Transport)
                        .Select(o => o.ConversionMatrix[loadTypes]).Aggregate((a, x) => a * x);
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() -
                                   Qin.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                       .Select(x => x.Value).ToArray()
                                       .Sum() +
                                   Qout.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                       .Select(x => x.Value).ToArray()
                                       .Sum() ==
                                   cooling.ToList().GetRange(i, dt).Sum() / pipe);
                    }

                    // Peak Condition
                    // solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                    //                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                    //            cooling.Max() / pipe);
                }

                if (loadTypes == LoadTypes.Heating)
                {
                    var pipe = DistrictControl.Instance.ListOfPlantSettings
                        .Where(o => o.OutputType == LoadTypes.Transport)
                        .Select(o => o.ConversionMatrix[loadTypes]).Aggregate((a, x) => a * x);
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() -
                                   Qin.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                       .Select(x => x.Value).ToArray()
                                       .Sum() +
                                   Qout.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                       .Select(x => x.Value).ToArray()
                                       .Sum() ==
                                   heating.ToList().GetRange(i, dt).Sum() / pipe);
                    }

                    // Peak Condition
                    // solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                    //                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                    //            heating.Max() / pipe);
                }

                if (loadTypes == LoadTypes.Elec)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() -
                                   Qin.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                       .Select(x => x.Value).ToArray()
                                       .Sum() +
                                   Qout.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                       .Select(x => x.Value).ToArray()
                                       .Sum() ==
                                   electricity.ToList().GetRange(i, dt).Sum() + E[(i, loadTypes)]);
                    }

                    //Peak Condition
                    // solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                    //                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                    //            electricity.Max());
                }

                if (loadTypes == LoadTypes.Gas)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray()
                                       .Sum() ==
                                   0);
                    }

                    //Peak Condition
                    // solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                    //                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                    //            0);
                }
            }

            // Solar & Wind Constraints
            foreach (var solarSupply in DistrictControl.Instance.ListOfPlantSettings.OfType<ISolar>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    solver.Add(P[(t, solarSupply)] == solarSupply.SolarAvailableInput.ToList().GetRange(t, dt).Sum() *
                        solarSupply.AvailableArea);
                }

                //solver.Add(P[(8760, solarSupply)] == solarSupply.SolarAvailableInput.ToList().Average() * solarSupply.AvailableArea);
            }

            // Storage Rules
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<IStorage>())
            {
                // solver.Add(S[(0, storage)]
                //            == (1 - storage.StorageStandingLosses) *
                //            S.Where(k => k.Key.Item2 == storage).Select(o => o.Value).Skip(1).First() +
                //            storage.ChargingEfficiency * Qin[(0, storage)] -
                //            (1 / storage.DischargingEfficiency) * Qout[(0, storage)]);

                // storage content initial <= final, both variable
                solver.Add(S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).Skip(1).First() <=
                           S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).Last());

                // 'storage content initial == and final >= storage.init * capacity'
                solver.Add(S.Where(x => x.Key.Item2 == storage).Select(o=>o.Value).First() == storage.StartingCapacity);

                // Storage Balance Rule
                for (int t = dt; t < timeSteps * dt; t += dt)
                {
                    solver.Add(S[(t, storage)] ==
                               (1 - storage.StorageStandingLosses) *
                               S[(t - dt, storage)] +
                               storage.ChargingEfficiency * Qin[(t, storage)] -
                               (1 / storage.DischargingEfficiency) * Qout[(t, storage)]);
                }
            }

            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            // Set the Objective Function
            var objective = solver.Objective();

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<IDispatchable>())
            {
                for (int t = dt; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.F * DistrictEnergy.Settings.AnnuityFactor + supplymodule.V * dt);
                }
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<IStorage>())
            {
                for (int t = dt; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(S[(t, storage)],
                        storage.F * DistrictEnergy.Settings.AnnuityFactor + storage.V * dt);
                }
            }

            foreach (var variable in E)
            {
                objective.SetCoefficient(variable.Value, 1000000);
            }

            objective.SetMinimization();

            var lp = solver.ExportModelAsLpFormat(false);
            solver.EnableOutput();
            var resultStatus = solver.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("The problem does not have an optimal solution!");
                return;
            }

            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine("Optimal objective value = " + solver.Objective().Value());

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<IDispatchable>())
            {
                var solutionValues = P.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                var cap = solutionValues.Max();
                var energy = solutionValues.Sum();
                plant.Input = solutionValues.ToList().GetRange(0, timeSteps).ToArray();
                plant.Output = solutionValues.ToList().GetRange(0, timeSteps)
                    .Select(x => x * plant.ConversionMatrix[plant.OutputType]).ToArray();
                RhinoApp.WriteLine($"{plant.Name} = {cap} Peak ; {energy} Annum");
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<IStorage>())
            {
                storage.Output = Qout.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue()).ToArray();
                storage.Input = Qin.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue()).ToArray();
                storage.Storage = S.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue()).ToArray();
                RhinoApp.WriteLine(
                    $"{storage.Name} = Qin {storage.Input.Sum()}; Qout {storage.Output.Sum()}; EndStorageState {storage.Storage.Last()}");
            }

            // Write Exports
            RhinoApp.WriteLine($"Export_Electricity = {E.Select(x => x.Value.SolutionValue()).ToArray().Sum()}");


            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
            OnCompletion(new SimulationCompleted() {TimeStep = timeSteps});
        }

        public event EventHandler Completion;

        protected virtual void OnCompletion(EventArgs e)
        {
            EventHandler handler = Completion;
            handler?.Invoke(this, e);
        }

        public class SimulationCompleted : EventArgs
        {
            public int TimeStep { get; set; }
            public int Threshold { get; set; }
            public DateTime TimeReached { get; set; }
        }
    }
}