using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        /// Var: Input energy flow at each supply module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> P =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Var: Max input energy flow at each supply module of the energy hub"
        /// </summary>
        public Dictionary<IThermalPlantSettings, Variable> C =
            new Dictionary<IThermalPlantSettings, Variable>();

        /// <summary>
        /// Var: Storage state at each storage module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> S =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Var: Output energy flow at each storage module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> Qout =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Var: Input energy flow at each storage module of the energy hub at each time step"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> Qin =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Param: DistrictLoad Demand (Umi Buildings + Additional Loads + Losses)
        /// </summary>
        public Dictionary<(int, LoadTypes, IBaseLoad), double> Load =
            new Dictionary<(int, LoadTypes, IBaseLoad), double>();

        /// <summary>
        /// Var: Exports by LoadType
        /// </summary>
        public Dictionary<(int, Exportable), Variable> E =
            new Dictionary<(int, Exportable), Variable>();

        /// <summary>
        /// Var: Total Area (m2)
        /// </summary>
        public Dictionary<SolarInput, Variable> Area = new Dictionary<SolarInput, Variable>();

        /// <summary>
        /// Var: Number of Wind Turbines
        /// </summary>
        public Dictionary<WindInput, Variable> NWind = new Dictionary<WindInput, Variable>();

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
                LpModel = Solver.CreateSolver("SimpleLP", "GLOP_LINEAR_PROGRAMMING ");
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

                C[supplymodule] = LpModel.MakeNumVar(0.0, double.PositiveInfinity, $"Cap_{supplymodule.Name}");
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    P[(t, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity,
                        string.Format($"P_{t:0000}_{supplymodule.Name}"));
                }

                C[supplymodule] = LpModel.MakeNumVar(0.0, double.PositiveInfinity, $"Cap_{supplymodule.Name}");
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    P[(t, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity,
                        string.Format($"P_{t:0000}_{supplymodule.Name}"));
                }

                C[supplymodule] = LpModel.MakeNumVar(0.0, double.PositiveInfinity, $"Cap_{supplymodule.Name}");
            }

            foreach (var environment in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                Area[environment] = LpModel.MakeNumVar(0, Double.PositiveInfinity, $"Area_{environment.Name}");
            }

            foreach (var wind in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                NWind[wind] = LpModel.MakeNumVar(0, Double.PositiveInfinity, $"Number_of_{wind.Name}");
            }

            watch.Stop();
            RhinoApp.WriteLine($"Computed {P.Count} P variables in {watch.ElapsedMilliseconds} milliseconds");

            // Storage Variables
            RhinoApp.WriteLine("Computing storage module variables...");
            watch.Start();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                // Initial State
                S[(-dt, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity,
                    $"StoState_{-dt:0000}_{supplymodule.Name}");
                for (var t = 0; t < timeSteps * dt; t += dt)
                {
                    Qin[(t, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity,
                        $"StoIn_{t:0000}_{supplymodule.Name}");
                    Qout[(t, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity,
                        $"StoOut_{t:0000}_{supplymodule.Name}");
                    S[(t, supplymodule)] = LpModel.MakeNumVar(0.0, double.PositiveInfinity, 
                        $"StoState_{t:0000}_{supplymodule.Name}");
                } 
                C[supplymodule] = LpModel.MakeNumVar(0.0, double.PositiveInfinity, $"Cap_{supplymodule.Name}");
            }

            watch = Stopwatch.StartNew();
            RhinoApp.WriteLine(
                $"Computed {Qin.Count + Qout.Count + S.Count} S variables in {watch.ElapsedMilliseconds} milliseconds");


            RhinoApp.WriteLine("Number of variables = " + LpModel.NumVariables());

            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads)
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    Load[(t, load.LoadType, load)] = load.Input.ToList().GetRange(t, dt).Sum();
                }
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    E[(t, supplymodule)] =
                        LpModel.MakeNumVar(0, double.PositiveInfinity, $"E_{t:0000}_{supplymodule.Name}");
                }

                C[supplymodule] = LpModel.MakeNumVar(0.0, double.PositiveInfinity, $"Cap_{supplymodule.Name}");
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
                            E.Where(x => x.Key.Item2.OutputType == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                .ToArray().Sum());
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
                                .Sum() +
                            E.Where(x => x.Key.Item2.OutputType == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                .ToArray().Sum());
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
                                .Sum() +
                            E.Where(x => x.Key.Item2.OutputType == loadTypes && x.Key.Item1 == i).Select(o => o.Value)
                                .ToArray().Sum());
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
            LinearExpr TotalDemand(LoadTypes loadType, int t)
            {
                return Load.Where(x => x.Key.Item2 == loadType && x.Key.Item1 == t).Select(o => o.Value).Sum() +
                       E.Where(x => x.Key.Item2.OutputType == loadType && x.Key.Item1 == t).Select(o => o.Value)
                           .ToArray().Sum();
            }

            double TotalLoad(LoadTypes loadType, int t)
            {
                return Load.Where(x => x.Key.Item2 == loadType && x.Key.Item1 == t).Select(o => o.Value).Sum();
            }

            LinearExpr TotalAnnualDemand(LoadTypes loadType)
            {
                return Load.Where(x => x.Key.Item2 == loadType).Select(o => o.Value).Sum() +
                       E.Where(x => x.Key.Item2.OutputType == loadType).Select(o => o.Value).ToArray().Sum();
            }

            foreach (var loadType in new List<LoadTypes>() {LoadTypes.Heating, LoadTypes.Cooling, LoadTypes.Elec})
            {
                LpModel.Add(P.Where(x => x.Key.Item2.ConversionMatrix.ContainsKey(loadType))
                                .Select(o => o.Value * o.Key.Item2.ConversionMatrix[loadType]).ToArray().Sum() <=
                            TotalAnnualDemand(loadType));
            }

            // Forced Capacity Constraints
            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                if (plant.IsForced)
                {
                    var loadType = plant.OutputType;
                    LpModel.Add(
                        P.Where(o => o.Key.Item2 == plant).Select(x => x.Value * Math.Abs(plant.ConversionMatrix[loadType]))
                            .ToArray().Sum() == plant.CapacityFactor * TotalAnnualDemand(loadType));
                }
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings)
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    var outputType = plant.OutputType;
                    try
                    {
                        LpModel.Add(P[(t, plant)] * plant.ConversionMatrix[outputType] <= C[plant]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            // Solar Constraints
            foreach (var solarSupply in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                LoadTypes loadType = solarSupply.OutputType;
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    // Constraint linking the area of solar device and the amount of available solar radiation
                    LpModel.Add(P[(t, solarSupply)] == solarSupply.SolarAvailableInput(t, dt).Sum() *
                        Area[solarSupply]);
                }

                if (solarSupply.IsForced)
                {
                    LpModel.Add(
                        P.Where(x => x.Key.Item2 == solarSupply)
                            .Select(o => o.Value * solarSupply.ConversionMatrix[loadType]).ToArray().Sum() ==
                        solarSupply.CapacityFactor * TotalAnnualDemand(loadType));
                }
            }

            // Wind Constraints
            foreach (var windTurbine in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                LoadTypes loadType = windTurbine.OutputType;
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    // Constraint linking the number of wind turbines and the power available from the wind
                    LpModel.Add(P[(t, windTurbine)] == windTurbine.Power(t, dt) * NWind[windTurbine]);
                    if (windTurbine.IsForced)
                    {
                        LpModel.Add(P[(t, windTurbine)] * windTurbine.ConversionMatrix[loadType] ==
                                    windTurbine.CapacityFactor * TotalDemand(loadType, t));
                    }
                }
            }

            // Storage Rules
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                // storage content initial == final
                // storage state cyclicity rule
                LpModel.Add(S[(0, storage)] <= S[(timeSteps * dt - dt, storage)]);

                // 'storage content initial == and final >= storage.init * capacity'
                LpModel.Add(S[(0, storage)] == storage.StartingCapacity * C[storage]);
                //LpModel.Add(S[(timeSteps * dt, storage)] == storage.StartingCapacity * C[storage]);

                // Initial Capacity Constraint
                LpModel.Add(S[(-dt, storage)] <= C[storage]);

                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    // Storage Balance Rule
                    LpModel.Add(S[(t, storage)] ==
                                (1 - storage.StorageStandingLosses) *
                                S[(t - dt, storage)] +
                                storage.ChargingEfficiency * Qin[(t, storage)] -
                                (1 / storage.DischargingEfficiency) * Qout[(t, storage)]);

                    // Storage Capacity at time t Rule
                    LpModel.Add(S[(t, storage)] <= C[storage]);

                    // Input & Output Capacity Constraints
                    LpModel.Add(Qin[(t, storage)] <= 0.3 * C[storage]);
                    LpModel.Add(Qout[(t, storage)] <= 0.3 * C[storage]);
                }

                // Forced Capacity Constraint
                if (storage.IsForced)
                {
                    LpModel.Add(C[storage] == storage.CapacityFactor * TotalAnnualDemand(storage.OutputType) / 365);
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
                        supplymodule.V * Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
                }

                objective.SetCoefficient(C[supplymodule], supplymodule.F * DistrictEnergy.Settings.AnnuityFactor / dt *
                                                          Math.Abs(supplymodule.ConversionMatrix[
                                                              supplymodule.OutputType]));
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.V * Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
                }

                objective.SetCoefficient(C[supplymodule], supplymodule.F * DistrictEnergy.Settings.AnnuityFactor / dt *
                                                          Math.Abs(supplymodule.ConversionMatrix[
                                                              supplymodule.OutputType]));
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.V * Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
                }

                objective.SetCoefficient(C[supplymodule], supplymodule.F * DistrictEnergy.Settings.AnnuityFactor / dt *
                                                          Math.Abs(supplymodule.ConversionMatrix[
                                                              supplymodule.OutputType]));
            }

            foreach (var exportable in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(C[exportable],
                        exportable.F * DistrictEnergy.Settings.AnnuityFactor / dt *
                        Math.Abs(exportable.ConversionMatrix[exportable.OutputType]));
                    objective.SetCoefficient(E[(t, exportable)], exportable.V);
                }
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(C[storage], storage.F * DistrictEnergy.Settings.AnnuityFactor / dt);
                    objective.SetCoefficient(Qin[(t, storage)], storage.V);
                    objective.SetCoefficient(Qout[(t, storage)], storage.V);
                }
            }

            objective.SetMinimization();

            var lp = LpModel.ExportModelAsLpFormat(false);
            LpModel.EnableOutput();
            RhinoApp.WriteLine("Solving...");
            var resultStatus = LpModel.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("The problem does not have an optimal solution!");
                return Result.Failure;
            }

            var activities = LpModel.ComputeConstraintActivities();
            var stream = UmiContext.Current.AuxiliaryFiles.CreateNewFileStream("lp_solver_log.txt");
            foreach (var constraint in LpModel.constraints())
            {
                var index0 = constraint.Index();
                var message =
                    $"constraint{index0}: dual value = {constraint.DualValue()} activities = {activities[index0]}\n";
                var bytes = Encoding.UTF8.GetBytes(message);
                stream.Write(bytes, 0, bytes.Length);
            }

            RhinoApp.WriteLine(
                $"Constraints Activities Logged at {UmiContext.Current.AuxiliaryFiles.GetFullPath("lp_solver_log.txt")}");
            stream.Close();

            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine($"Optimal objective value = {LpModel.Objective().Value():C0}");

            double TotalActualDemand(LoadTypes outputType)
            {
                return P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(outputType))
                    .Select(k => k.Value.SolutionValue() * k.Key.Item2.ConversionMatrix[outputType]).ToArray().Sum();
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                var solutionValues = P.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                plant.Capacity = C[plant].SolutionValue(); //solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]);
                plant.Output = energy.ToDateTimePoint();
                plant.CapacityFactor = Math.Round(energy.Sum() / TotalActualDemand(plant.OutputType), 2);
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity} Peak ; {energy.Sum()} Annum");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                var solutionValues = P.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                plant.Capacity = C[plant].SolutionValue(); //solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]);
                plant.Output = energy.ToDateTimePoint();
                plant.CapacityFactor = Math.Round(energy.Sum() / TotalActualDemand(plant.OutputType), 2);
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity} Peak ; {energy.Sum()} Annum");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                var solutionValues = P.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                plant.Capacity = C[plant].SolutionValue(); //solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]);
                plant.Output = energy.ToDateTimePoint();
                plant.CapacityFactor = Math.Round(energy.Sum() / TotalActualDemand(plant.OutputType), 2);
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity} Peak ; {energy.Sum()} Annum");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                var solutionValues = E.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                plant.Capacity = C[plant].SolutionValue(); // solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]);
                plant.Output = energy.ToDateTimePoint();
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity} Peak ; {energy.Sum()} Annum");
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                storage.Output = Qout.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue())
                    .ToDateTimePoint();
                storage.Input = Qin.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue())
                    .ToDateTimePoint();
                storage.Stored = S.Where(x => x.Key.Item2 == storage).Select(v => v.Value.SolutionValue())
                    .ToDateTimePoint();
                storage.Capacity = C[storage].SolutionValue();
                var totalActualDemand = TotalActualDemand(storage.OutputType);
                storage.CapacityFactor = Math.Round(storage.Output.Sum() / totalActualDemand * 365, 2);
                RhinoApp.WriteLine(
                    $"{storage.Name} = Qin {storage.Input.Sum():N0}; Qout {storage.Output.Sum():N0}; Storage Balance {storage.Input.Sum() - storage.Output.Sum():N0}; Storage Capacity {storage.Capacity:N0}");
            }

            foreach (var area in Area)
            {
                var areaM2 = area.Value.SolutionValue();
                RhinoApp.WriteLine($"Area {area.Key.Name} = {areaM2:F0}");
            }

            foreach (var wind in NWind)
            {
                var nWind = wind.Value.SolutionValue();
                RhinoApp.WriteLine($"# of {wind.Key.Name} = {nWind:F0}");
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
            C.Clear();
            E.Clear();
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