using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Documents;
using CsvHelper;
using DistrictEnergy.Helpers;
using DistrictEnergy.Metrics;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;
using Google.OrTools.LinearSolver;
using Rhino;
using Rhino.Commands;
using Umi.RhinoServices;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy
{
    public class DhRunLpModel : UmiCommand
    {
        public DhRunLpModel()
        {
            Instance = this;

            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;
        }

        private void OnProjectClosed(object sender, EventArgs e)
        {
            StaleResults = true;
        }

        /// <summary>
        /// Var: Input energy flow at each supply module of the energy hub at each time step (kWh)"
        /// </summary>
        public Dictionary<(int, IThermalPlantSettings), Variable> P =
            new Dictionary<(int, IThermalPlantSettings), Variable>();

        /// <summary>
        /// Var: Capacity of each supply module of the energy hub (kW); For storage capacities, units are kWh"
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
        public static DhRunLpModel Instance { get; private set; }

        public Solver LpModel { get; private set; }

        public override string EnglishName => "DHRunLPModel";
        public bool StaleResults { get; set; } = true;

        public override Result Run(RhinoDoc doc, UmiContext context, RunMode mode)
        {
            return Main(context);
        }

        private Result Main(UmiContext umiContext)
        {
            ClearVariables();
            PreSolve(umiContext);
            // Create the linear solver with the CBC backend.
            try
            {
                LpModel = Solver.CreateSolver("GLOP");
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
            var watch = Stopwatch.StartNew();
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
                NWind[wind] = LpModel.MakeIntVar(0, int.MaxValue, $"Number_of_{wind.Name}");
            }

            watch.Stop();
            RhinoApp.WriteLine($"Computed {P.Count} P variables in {watch.ElapsedMilliseconds} milliseconds");
            watch = Stopwatch.StartNew();
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

            RhinoApp.WriteLine(
                $"Computed {Qin.Count + Qout.Count + S.Count} S variables in {watch.ElapsedMilliseconds} milliseconds");


            RhinoApp.WriteLine($"Number of variables = {LpModel.NumVariables()}");

            RhinoApp.WriteLine("Computing constraints...");
            watch = Stopwatch.StartNew();
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

            // Total demand from Loads and exports
            LinearExpr TotalAnnualDemand(LoadTypes loadType)
            {
                return P
                           .Where(x => x.Key.Item2.InputType == loadType)
                           .Select(o => o.Value).ToArray().Sum() +
                       Load.Where(x => x.Key.Item2 == loadType).Select(o => o.Value).Sum() +
                       E.Where(x => x.Key.Item2.OutputType == loadType).Select(o => o.Value).ToArray().Sum();
            }

            foreach (var loadType in new List<LoadTypes> {LoadTypes.Heating, LoadTypes.Cooling, LoadTypes.Elec})
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
                        P.Where(o => o.Key.Item2 == plant)
                            .Select(x => x.Value * Math.Abs(plant.ConversionMatrix[loadType]))
                            .ToArray().Sum() == plant.CapacityFactor * TotalAnnualDemand(loadType));
                }
            }

            foreach (var customEnergySupplyModule in DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    // Constraint linking the area of solar device and the amount of available solar radiation
                    LpModel.Add(P[(t, customEnergySupplyModule)] == customEnergySupplyModule.HourlyCapacity.GetRange(t, dt).Sum());
                }
                C[customEnergySupplyModule].SetUb(customEnergySupplyModule.HourlyCapacity.Max());
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<NotStorage>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    var outputType = plant.OutputType;
                    try
                    {
                        LpModel.Add(P[(t, plant)] * plant.ConversionMatrix[outputType] / dt <= C[plant]);
                    }
                    catch (Exception e)
                    {
                        RhinoApp.WriteLine(e.Message);
                    }
                }
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    var outputType = plant.OutputType;
                    try
                    {
                        LpModel.Add(E[(t, plant)] * plant.ConversionMatrix[outputType] / dt <= C[plant]);
                    }
                    catch (Exception e)
                    {
                        RhinoApp.WriteLine(e.Message);
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
                if (solarSupply.IsForcedDimensionCapacity)
                {
                    // Constraint linking the user-defined max area
                    LpModel.Add(Area[solarSupply] <= solarSupply.RequiredArea);
                }

                if (solarSupply.IsForced)
                {
                    // Constraint linking the user-defined max area
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
                    LpModel.Add(P[(t, windTurbine)] == windTurbine.PowerPerTurbine(t, dt).Sum() * NWind[windTurbine]);
                }

                if (windTurbine.IsForcedDimensionCapacity)
                {
                    // Constraint linking the user-defined number of wind turbines
                    LpModel.Add(NWind[windTurbine] <= windTurbine.RequiredNumberOfWindTurbines);
                }

                if (windTurbine.IsForced)
                {
                    LpModel.Add(P.Where(x => x.Key.Item2 == windTurbine)
                                    .Select(o => o.Value * windTurbine.ConversionMatrix[loadType]).ToArray().Sum() ==
                                windTurbine.CapacityFactor * TotalAnnualDemand(loadType));
                }
            }

            // Storage Rules
            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                // storage content initial == final
                // LpModel.Add(S[(-dt, storage)] == S[(timeSteps * dt - dt, storage)]);
                // storage state cyclicity rule
                LpModel.Add(S[(-dt, storage)] <= S[(timeSteps * dt - dt, storage)]);

                // 'storage content initial == and final >= storage.init * capacity'
                LpModel.Add(S[(-dt, storage)] == storage.StartingCapacity * C[storage]);
                //LpModel.Add(S[(timeSteps * dt - dt, storage)] == storage.StartingCapacity * C[storage]);

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
                    LpModel.Add(Qin[(t, storage)] <= 0.3 * C[storage]);  // Can't charge more then 30% of total capacity in one time step
                    LpModel.Add(Qout[(t, storage)] <= 0.3 * C[storage]);  // Can't discharge more then 30% of total capacity in one time step
                }

                // Forced Capacity Constraint
                if (storage.IsForced)
                {
                    LpModel.Add(C[storage] == storage.CapacityFactor * TotalAnnualDemand(storage.OutputType) / 365);
                }
            }

            RhinoApp.WriteLine(
                $"Computed {LpModel.NumConstraints()} constraints in {watch.ElapsedMilliseconds} milliseconds");

            // Set the Objective Function
            var objective = LpModel.Objective();
            double carbonRatio =
                DistrictControl.PlanningSettings
                    .CarbonRatio; // Objective function ratio. How much to optimize for carbon

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                // Variable Costs (One per time step)
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    // carbon intensity is divided by 1e6 to have tonCO2/kWh, then multiplied by carbon cost $/tonCO2
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.V *
                        Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType])
                        + carbonRatio * supplymodule.CarbonIntensity / 1e6 * DistrictControl.PlanningSettings.CarbonPricePerTon *
                        Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
                }

                // Fixed Costs (One for whole timeSteps)
                objective.SetCoefficient(C[supplymodule],
                    supplymodule.F * DistrictControl.PlanningSettings.AnnuityFactor *
                    Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                // Variable Costs (One per time step)
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.V * Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
                }

                // Fixed Costs (One for whole timeSteps)
                objective.SetCoefficient(C[supplymodule],
                    supplymodule.F * DistrictControl.PlanningSettings.AnnuityFactor *
                    Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
            }

            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                // Variable Costs (One per time step)
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(P[(t, supplymodule)],
                        supplymodule.V * Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
                }

                // Fixed Costs (One for whole timeSteps)
                objective.SetCoefficient(C[supplymodule],
                    supplymodule.F * DistrictControl.PlanningSettings.AnnuityFactor *
                    Math.Abs(supplymodule.ConversionMatrix[supplymodule.OutputType]));
            }

            foreach (var exportable in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                // Variable Costs (One per time step)
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(E[(t, exportable)], exportable.V);
                }

                // Fixed Costs (One for whole timeSteps)
                objective.SetCoefficient(C[exportable],
                    exportable.F * DistrictControl.PlanningSettings.AnnuityFactor *
                    Math.Abs(exportable.ConversionMatrix[exportable.OutputType]));
            }

            foreach (var storage in DistrictControl.Instance.ListOfPlantSettings.OfType<Storage>())
            {
                // Variable Costs (One per time step)
                for (int t = 0; t < timeSteps * dt; t += dt)
                {
                    objective.SetCoefficient(Qin[(t, storage)], storage.V);
                    objective.SetCoefficient(Qout[(t, storage)], storage.V);
                }
                // Fixed storage cost
                // Only 30% of Capacity can be used each timestep therefore fixes the kW capacity for the fixed capacity;
                objective.SetCoefficient(C[storage], storage.F * DistrictControl.PlanningSettings.AnnuityFactor / (0.3 * dt));
            }

            RhinoApp.WriteLine("Solving...");
            objective.SetMinimization();
            var lp = LpModel.ExportModelAsLpFormat(false);
            umiContext.AuxiliaryFiles.StoreText("lp_problem.lp", lp);
            LpModel.EnableOutput();
            var resultStatus = LpModel.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus == Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("Optimal Solution Found!");
            }

            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("The problem does not have an optimal solution!");
                return Result.Failure;
            }

            var activities = LpModel.ComputeConstraintActivities();
            var stream = umiContext.AuxiliaryFiles.CreateNewFileStream("lp_solver_log.txt");
            foreach (var constraint in LpModel.constraints())
            {
                var index0 = constraint.Index();
                var message =
                    $"constraint{index0}: dual value = {constraint.DualValue()} activities = {activities[index0]}\n";
                var bytes = Encoding.UTF8.GetBytes(message);
                stream.Write(bytes, 0, bytes.Length);
            }

            RhinoApp.WriteLine(
                $"Constraints Activities Logged at {umiContext.AuxiliaryFiles.GetFullPath("lp_solver_log.txt")}");
            stream.Close();

            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine($"Optimal objective value = {LpModel.Objective().Value():f0}");

            double TotalActualDemand(LoadTypes outputLoadType)
            {
                var demandMetByHub = P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(outputLoadType) 
                        ? k.Key.Item2.ConversionMatrix[outputLoadType] > 0 : false)
                    .Select(k => k.Value.SolutionValue() * Math.Abs(k.Key.Item2.ConversionMatrix[outputLoadType])).ToArray().Sum();
                return demandMetByHub;
            }

            foreach (LoadTypes loadType in Enum.GetValues(typeof(LoadTypes)))
            {
                var total = TotalActualDemand(loadType);
                RhinoApp.WriteLine($"Actual {loadType} demand is {total:N0} kWh");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Dispatchable>())
            {
                var solutionValues = P.Where(o => o.Key.Item2 == plant).Select(v => v.Value.SolutionValue()).ToArray();
                plant.Capacity = C[plant].SolutionValue(); //solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]).ToArray();
                plant.Output = energy.ToDateTimePoint();
                var totalActualDemand = TotalActualDemand(plant.OutputType);
                plant.CapacityFactor = totalActualDemand > 1e-3 ? Math.Round(energy.Sum() / totalActualDemand, 2) : 0;
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity:N0} kW Peak ; {energy.Sum():N0} kWh Annum");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<SolarInput>())
            {
                var solutionValues = P.Where(o => o.Key.Item2 == plant).Select(v => v.Value.SolutionValue()).ToArray();
                plant.Capacity = C[plant].SolutionValue(); //solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]).ToArray();
                plant.Output = energy.ToDateTimePoint();
                plant.RequiredArea = Area[plant].SolutionValue();
                var totalActualDemand = TotalActualDemand(plant.OutputType);
                plant.CapacityFactor = totalActualDemand > 1e-3 ? Math.Round(energy.Sum() / totalActualDemand, 2) : 0;
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity:N0} kW Peak ; {energy.Sum():N0} kWh Annum");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<WindInput>())
            {
                var solutionValues = P.Where(o => o.Key.Item2 == plant).Select(v => v.Value.SolutionValue()).ToArray();
                plant.Capacity = C[plant].SolutionValue(); //solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]).ToArray();
                plant.Output = energy.ToDateTimePoint();
                plant.RequiredNumberOfWindTurbines = NWind[plant].SolutionValue();
                var totalActualDemand = TotalActualDemand(plant.OutputType);
                plant.CapacityFactor = totalActualDemand > 1e-3 ? Math.Round(energy.Sum() / totalActualDemand, 2) : 0;
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity:N0} kW Peak ; {energy.Sum():N0} kWh Annum");
            }

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.OfType<Exportable>())
            {
                var solutionValues = E.Where(o => o.Key.Item2 == plant).Select(v => v.Value.SolutionValue()).ToArray();
                plant.Capacity = C[plant].SolutionValue(); // solutionValues.Max();
                plant.Input = solutionValues.ToDateTimePoint();
                var energy = solutionValues.Select(x => x * plant.ConversionMatrix[plant.OutputType]).ToArray();
                plant.Output = energy.ToDateTimePoint();
                RhinoApp.WriteLine($"{plant.Name} = {plant.Capacity:N0} kW Peak ; {energy.Sum():N0} kWh Annum");
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
                storage.CapacityFactor = totalActualDemand > 1e-3 ? Math.Round(storage.Capacity / totalActualDemand * 365, 2) : 0;
                RhinoApp.WriteLine(
                    $"{storage.Name} = Qin {storage.Input.Sum():N0}; Qout {storage.Output.Sum():N0}; Storage Balance {storage.Input.Sum() - storage.Output.Sum():N0}; Storage Capacity {storage.Capacity:N0} kWh");
                RhinoApp.WriteLine($"{storage.Name} initial Storage Level = {storage.Stored.First().Value:N0} kWh");
                RhinoApp.WriteLine($"{storage.Name} final Storage Level = {storage.Stored.Last().Value:N0} kWh");
            }

            foreach (var area in Area)
            {
                var areaM2 = area.Value.SolutionValue();
                RhinoApp.WriteLine($"Area {area.Key.Name} = {areaM2:F0} m²");
            }

            foreach (var wind in NWind)
            {
                var nWind = wind.Value.SolutionValue();
                RhinoApp.WriteLine($"# of {wind.Key.Name} = {nWind:F2}");
            }


            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + LpModel.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + LpModel.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + LpModel.Nodes() + " branch-and-bound nodes");
            OnCompletion(new SimulationCompleted() {TimeSteps = timeSteps, Period = dt});
            SaveResults(umiContext, timeSteps, dt);
            return Result.Success;
        }

        /// <summary>
        /// Save inputs and outputs to csv
        /// </summary>
        /// <param name="umiContext"></param>
        /// <param name="timeSteps"></param>
        /// <param name="dt"></param>
        /// <param name="filename"></param>
        private void SaveResults(UmiContext umiContext, int timeSteps, int dt, string filename = "lp_run.csv")
        {
            var stream = umiContext.AuxiliaryFiles.CreateNewFileStream(filename);
            // var records = P.GroupBy(i => i.Key.Item2,
            //     (key, values) => new { Name=key.Name, Values=values.Select(o => o.Value.SolutionValue()).ToArray() });
            var records =
                DistrictControl.Instance.ListOfPlantSettings.Select(o => new
                    {
                        Name = o.Name, Values = o.Input.Select(p => p.Value).ToArray(), 
                        Type = o.InputType,
                        Direction = "In"
                    })
                    .Concat(DistrictControl.Instance.ListOfPlantSettings.Select(o => new
                    {
                        Name = o.Name, Values = o.Output.Select(p => p.Value).ToArray(), 
                        Type = o.OutputType,
                        Direction = "Out"
                    })).Concat(DistrictControl.Instance.ListOfDistrictLoads.Select(o => new
                    {
                        Name = o.Name,
                        Values = o.Input.Select((s, i) => new { Value = s, Index = i }).GroupBy(x => x.Index / dt)
                            .Select(grp => grp.Select(x => x.Value).ToArray().Sum()).ToArray(),
                        Type = o.LoadType,
                        Direction = "In"
                    }));
            using (var writer = new StreamWriter(stream))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // Write header
                    csv.WriteField("TimeStamp");
                    foreach (var record in records)
                    {
                        csv.WriteField($"{record.Direction}_{record.Type}_{record.Name}");
                    }

                    csv.NextRecord();
                    // Write rows
                    for (int i = 0; i < timeSteps; i++)
                    {
                        csv.WriteField(i.ToString());

                        foreach (var record in records)
                        {
                            csv.WriteField(record.Values[i].ToString("F0"));
                        }

                        csv.NextRecord();
                    }
                }
            } // Flush is also called here.
        }

        private void ClearVariables()
        {
            NWind.Clear();
            Area.Clear();
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

        /// <summary>
        /// PreSolves the model by calculating the load profiles
        /// </summary>
        /// <returns></returns>
        public Result PreSolve(UmiContext umiContext)
        {
            var contextBuilding = AbstractDistrictLoad.ContextBuildings(UmiContext.Current);
            foreach (var load in DistrictControl.Instance.ListOfDistrictLoads)
            {
                if (StaleResults)
                {
                    load.GetUmiLoads(contextBuilding, umiContext);

                    SolarInput.GetHourlyLocationSolarRadiation(umiContext);
                    WindInput.GetHourlyLocationWind(umiContext);
                }
            }

            Instance.StaleResults = false;

            return Result.Success;
        }
    }
}