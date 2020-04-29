using System;
using System.Collections.Generic;
using System.Linq;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.Networks.ThermalPlants;
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

        private void Main()
        {
            DHSimulateDistrictEnergy.Instance.PreSolve();
            // Create the linear solver with the CBC backend.
            var solver = Solver.CreateSolver("SimpleMipProgram", "GLOP_LINEAR_PROGRAMMING");

            // Define Model Variables. Here each variable is the supply power of each available supply module
            int timeSteps = (int) DistrictControl.PlanningSettings.TimeSteps; // Number of Time Steps
            int dt = 8760 / timeSteps; // Duration of each Time Steps

            // Input Enerygy
            var P = new Dictionary<(int, IThermalPlantSettings), Variable>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    P[(t, supplymodule)] = solver.MakeNumVar(0.0, supplymodule.Capacity / supplymodule.Efficiency * dt,
                        string.Format($"P_{t}_{supplymodule.Name}"));
                }
            }

            var Qin = new Dictionary<(int, IThermalPlantSettings), Variable>();
            var Qout = new Dictionary<(int, IThermalPlantSettings), Variable>();
            var S = new Dictionary<(int, IThermalPlantSettings), Variable>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
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

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            var Load = new Dictionary<(int, LoadTypes, AbstractDistrictLoad), double>();
            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads)
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    Load[(t, load.LoadType, load)] = load.Input.ToList().GetRange(t, dt).Sum();
                }
            }

            // Set Load Balance Constraints
            foreach (LoadTypes loadTypes in Enum.GetValues(typeof(LoadTypes)))
            {
                if (loadTypes == LoadTypes.Cooling)
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
                                   Load.Where(x => x.Key.Item2 == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                       .Sum());
                    }
                }

                if (loadTypes == LoadTypes.Heating)
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
                                   Load.Where(x => x.Key.Item2 == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                       .Sum());
                    }
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
                                   Load.Where(x => x.Key.Item2 == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                       .Sum() + E[(i, loadTypes)]);
                    }
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
            }

            // Storage Rules
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                // solver.Add(S[(0, storage)]
                //            == (1 - storage.StoredStandingLosses) *
                //            S.Where(k => k.Key.Item2 == storage).Select(o => o.Value).Skip(1).First() +
                //            storage.ChargingEfficiency * Qin[(0, storage)] -
                //            (1 / storage.DischargingEfficiency) * Qout[(0, storage)]);

                // storage content initial <= final, both variable
                solver.Add(S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).Skip(1).First() <=
                           S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).Last());

                // 'storage content initial == and final >= storage.init * capacity'
                solver.Add(
                    S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).First() == storage.StartingCapacity);

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

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                for (int t = dt; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.F * DistrictEnergy.Settings.AnnuityFactor + supplymodule.V * dt);
                }
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
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

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                var solutionValues = P.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                var cap = solutionValues.Max();
                var energy = solutionValues.Sum();
                plant.Input = solutionValues.ToDateTimePoint();
                plant.Output = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType])
                    .ToDateTimePoint();
                RhinoApp.WriteLine($"{plant.Name} = {cap} Peak ; {energy} Annum");
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                storage.Output = Qout.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue())
                    .ToDateTimePoint();
                storage.Input = Qin.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue())
                    .ToDateTimePoint();
                storage.Stored = S.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue())
                    .ToDateTimePoint();
                RhinoApp.WriteLine(
                    $"{storage.Name} = Qin {storage.Input.Sum()}; Qout {storage.Output.Sum()}; EndStorageState {storage.Stored.Last().Value}");
            }

            // Write Exports
            RhinoApp.WriteLine($"Export_Electricity = {E.Select(x => x.Value.SolutionValue()).ToArray().Sum()}");


            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
            OnCompletion(new SimulationCompleted() {TimeSteps = timeSteps, Period = dt});
        }

        public event EventHandler Completion;

        protected virtual void OnCompletion(EventArgs e)
        {
            EventHandler handler = Completion;
            handler?.Invoke(this, e);
        }

        public class SimulationCompleted : EventArgs
        {
            public int TimeSteps { get; set; }
            public int Period { get; set; }
        }
    }
}