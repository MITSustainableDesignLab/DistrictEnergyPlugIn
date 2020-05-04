using System;
using System.Collections.Generic;
using System.Linq;
using DistrictEnergy.Helpers;
using DistrictEnergy.Metrics;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.Networks.ThermalPlants;
using Google.OrTools.LinearSolver;
using Rhino;
using Rhino.Commands;
using Umi.RhinoServices.Context;

namespace DistrictEnergy
{
    public class DHRunLPModel : Command
    {
        public DHRunLPModel()
        {
            Instance = this;
        }

        /// <summary>
        /// Input energy flow at each supply module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> P =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Storage state at each storage module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> S =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Output energy flow at each storage module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> Qout =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Input energy flow at each storage module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> Qin =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// DistrictLoad Demand (Umi Buildings + Losses)
        /// </summary>
        public Dictionary<(int, LoadTypes, AbstractDistrictLoad), double> Load =
            new Dictionary<(int, LoadTypes, AbstractDistrictLoad), double>();

        /// <summary>
        /// Exports
        /// </summary>
        public Dictionary<(int, Exportable), Variable> E =
            new Dictionary<(int, Exportable), Variable>();

        ///<summary>The only instance of the DHRunLPModel command.</summary>
        public static DHRunLPModel Instance { get; private set; }

        public Solver LpModel { get; private set; }

        public override string EnglishName => "DHRunLPModel";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            return Main();
        }

        private Result Main()
        {
            ClearVariables();
            DHSimulateDistrictEnergy.Instance.PreSolve();
            // Create the linear solver with the CBC backend.
            try
            {
                LpModel = Solver.CreateSolver("SimpleLP", "GLOP_LINEAR_PROGRAMMING");
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
                return Result.Failure;
            }

            RhinoApp.WriteLine("Created Solver");

            // Define Model Variables. Here each variable is the supply power of each available supply module
            int timeSteps = (int) DistrictControl.PlanningSettings.TimeSteps; // Number of Time Steps
            int dt = 8760 / timeSteps; // Duration of each Time Steps

            // Input Energy
            RhinoApp.WriteLine("Computing input energy flow variables...");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    P[(t, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity,
                        string.Format($"P_{t:0000}_{supplymodule.Name}"));
                }
            }

            watch.Stop();
            RhinoApp.WriteLine($"Computed {P.Count} P variables in {watch.ElapsedMilliseconds} milliseconds");

            // Storage Variables
            RhinoApp.WriteLine("Computing storage module variables...");
            watch.Start();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    Qin[(t, supplymodule)] = LpModel.MakeNumVar(0.0, supplymodule.MaxChargingRate, $"StoIn_{t:0000}_{supplymodule.Name}");
                    Qout[(t, supplymodule)] = LpModel.MakeNumVar(0.0, supplymodule.MaxDischargingRate, $"StoOut_{t:0000}_{supplymodule.Name}");
                    S[(t, supplymodule)] = LpModel.MakeNumVar(0.0, supplymodule.Capacity, $"StoState_{t:0000}_{supplymodule.Name}");
                }
            }

            watch = System.Diagnostics.Stopwatch.StartNew();
            RhinoApp.WriteLine(
                $"Computed {Qin.Count + Qout.Count + S.Count} S variables in {watch.ElapsedMilliseconds} milliseconds");


            RhinoApp.WriteLine("Number of variables = " + LpModel.NumVariables());

            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads.OfType<BaseLoad>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    Load[(t, load.LoadType, load)] = load.Input.ToList().GetRange(t, dt).Sum();
                }
            }

            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads.OfType<Exportable>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    E[(t, load)] = LpModel.MakeNumVar(0, double.PositiveInfinity, $"E_{t:0000}_{load.Name}");
                }
            }

            // Set Load Balance Constraints
            foreach (LoadTypes loadTypes in Enum.GetValues(typeof(LoadTypes)))
            {
                if (loadTypes == LoadTypes.Cooling)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        LpModel.Add(
                            P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() -
                            Qin.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                .Select(x => x.Value).ToArray()
                                .Sum() +
                            Qout.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                .Select(x => x.Value).ToArray()
                                .Sum() ==
                            Load.Where(x => x.Key.Item2 == loadTypes && x.Key.Item1 == i).Select(o => o.Value).Sum() + 
                            E.Where(x => x.Key.Item2.LoadType == loadTypes && x.Key.Item1 == i).Select(o => o.Value).ToArray().Sum());
                    }
                }

                if (loadTypes == LoadTypes.Heating)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        LpModel.Add(
                            P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() -
                            Qin.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                .Select(x => x.Value).ToArray()
                                .Sum() +
                            Qout.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                .Select(x => x.Value).ToArray()
                                .Sum() ==
                            Load.Where(x => x.Key.Item2 == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                .Sum()+
                            E.Where(x => x.Key.Item2.LoadType == loadTypes && x.Key.Item1 == i).Select(o => o.Value).ToArray().Sum());
                    }
                }

                if (loadTypes == LoadTypes.Elec)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        LpModel.Add(
                            P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() -
                            Qin.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                .Select(x => x.Value).ToArray()
                                .Sum() +
                            Qout.Where(k => k.Key.Item2.OutputType == loadTypes && k.Key.Item1 == i)
                                .Select(x => x.Value).ToArray()
                                .Sum() ==
                            Load.Where(x => x.Key.Item2 == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                .Sum()+
                            E.Where(x => x.Key.Item2.LoadType == loadTypes && x.Key.Item1 == i).Select(o => o.Value).ToArray().Sum());
                    }
                }

                if (loadTypes == LoadTypes.Gas)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        LpModel.Add(
                            P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray()
                                .Sum() ==
                            0);
                    }
                }
            }

            // Capacity Constraints
            LinearExpr TotalDemand(LoadTypes loadType1, int t)
            {
                return Load.Where(x => x.Key.Item2 == loadType1 && x.Key.Item1 == t).Select(o => o.Value).Sum() +
                       E.Where(x => x.Key.Item2.LoadType == loadType1 && x.Key.Item1 == t).Select(o => o.Value).ToArray().Sum();
            }

            foreach (var inputFlow in P.Where(x => x.Key.Item2.Capacity != Double.PositiveInfinity).Where(x =>
                x.Key.Item2.OutputType == LoadTypes.Elec || x.Key.Item2.OutputType == LoadTypes.Heating ||
                x.Key.Item2.OutputType == LoadTypes.Cooling))
            {
                var i = inputFlow.Key.Item1;
                var plant = inputFlow.Key.Item2;
                var loadType = plant.OutputType;
                LpModel.Add(inputFlow.Value * plant.ConversionMatrix[loadType] <= plant.CapacityFactor * TotalDemand(loadType, i));
            }

            // Forced Capacity Constraints
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                if (plant.IsForced)
                {
                    for (int t = 0; t < timeSteps * dt; t += dt)
                    {
                        var loadType = plant.OutputType;
                        LpModel.Add(P[(t, plant)] * plant.ConversionMatrix[loadType] == plant.CapacityFactor * TotalDemand(loadType, t));
                    }
                }
            }

            // Solar Constraints
            foreach (var solarSupply in DistrictControl.Instance.ListOfPlantSettings.OfType<ISolar>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    LpModel.Add(P[(t, solarSupply)] == solarSupply.SolarAvailableInput.ToList().GetRange(t, dt).Sum() *
                        solarSupply.AvailableArea);
                }
            }
            // Wind Constraints
            foreach (var windTurbine in DistrictControl.Instance.ListOfPlantSettings.OfType<IWind>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    LpModel.Add(P[(t, windTurbine)] == windTurbine.Power(t, dt) * windTurbine.NumWnd);
                }
            }

            // Storage Rules
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {

                // storage content initial <= final, both variable
                // Must skip first timestep
                LpModel.Add(S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).Skip(1).First() <=
                            S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).Last());

                // 'storage content initial == and final >= storage.init * capacity'
                LpModel.Add(
                    S.Where(x => x.Key.Item2 == storage).Select(o => o.Value).First() == storage.StartingCapacity);

                // Storage Balance Rule
                for (int t = dt; t < timeSteps * dt; t += dt)
                {
                    LpModel.Add(S[(t, storage)] ==
                                (1 - storage.StorageStandingLosses) *
                                S[(t - dt, storage)] +
                                storage.ChargingEfficiency * Qin[(t, storage)] -
                                (1 / storage.DischargingEfficiency) * Qout[(t, storage)]);
                }
            }

            RhinoApp.WriteLine("Number of constraints = " + LpModel.NumConstraints());

            // Set the Objective Function
            var objective = LpModel.Objective();

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.F * DistrictEnergy.Settings.AnnuityFactor / dt + supplymodule.V);
                }
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(S[(t, storage)],
                        storage.F * DistrictEnergy.Settings.AnnuityFactor / dt + storage.V);
                }
            }

            foreach (var exportable in DistrictControl.Instance.ListOfDistrictLoads.OfType<Exportable>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(E[(t, exportable)],
                        exportable.F * DistrictEnergy.Settings.AnnuityFactor / dt + exportable.V);
                }
            }

            objective.SetMinimization();

            var lp = LpModel.ExportModelAsLpFormat(false);
            LpModel.EnableOutput();
            var resultStatus = LpModel.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("The problem does not have an optimal solution!");
                return Result.Failure;
            }

            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine("Optimal objective value = " + LpModel.Objective().Value());

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

            foreach (var plant in DistrictControl.Instance.ListOfDistrictLoads.OfType<Exportable>())
            {
                var solutionValues = E.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                var cap = solutionValues.Max();
                var energy = solutionValues.Sum();
                plant.Input = solutionValues.ToArray();
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
                    $"{storage.Name} = Qin {storage.Input.Sum()}; Qout {storage.Output.Sum()}; Storage Balance {storage.Input.Sum() - storage.Output.Sum()}");
            }


            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + LpModel.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + LpModel.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + LpModel.Nodes() + " branch-and-bound nodes");
            OnCompletion(new SimulationCompleted() {TimeSteps = timeSteps, Period = dt});
            return Result.Success;
        }

        private void ClearVariables()
        {
            P.Clear();
            Qin.Clear();
            Qout.Clear();
            S.Clear();
            Load.Clear();
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