using System.Collections.Generic;
using System.Linq;
using DistrictEnergy.Helpers;
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

        private static void Main()
        {
            DHSimulateDistrictEnergy.Instance.PreSolve();
            // Create the linear solver with the CBC backend.
            var solver = Solver.CreateSolver("SimpleMipProgram", "GLOP_LINEAR_PROGRAMMING");

            // Define Model Variables. Here each variable is the supply power of each available supply module
            int timeSteps = 4; // Number of Time Steps
            int dt = 8760 / timeSteps; // Duration of each Time Steps

            var P = new Dictionary<(int, IThermalPlantSettings), Variable>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.Where(o =>
                o.LoadType != LoadTypes.Transport))
            {
                for (var t = 0; t < timeSteps * dt; t+=dt)
                {
                    P[(t, supplymodule)] = solver.MakeNumVar(0.0, supplymodule.Capacity / supplymodule.Efficiency * dt,
                        string.Format($"P_{t}_{supplymodule.Name}"));
                }
                // Add Peak
                P[(8760, supplymodule)] = solver.MakeNumVar(0.0, supplymodule.Capacity / supplymodule.Efficiency * 1,
                    string.Format($"P_Peak_{supplymodule.Name}"));
            }

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            var cooling = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN;
            var heating = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN;
            var electricity = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN;

            // Set Load Balance Constraints
            foreach (var loadTypes in P.Select(x => x.Key.Item2.LoadType).Distinct())
            {
                if (loadTypes == LoadTypes.Cooling)
                {
                    var pipe = DistrictControl.Instance.ListOfPlantSettings
                        .Where(o => o.LoadType == LoadTypes.Transport)
                        .Select(o => o.ConversionMatrix[loadTypes]).Aggregate((a, x) => a * x);
                    for (int i = 0; i < timeSteps * dt; i+=dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                                   cooling.ToList().GetRange(i,dt).Sum() / pipe);
                    }
                    // Peak Condition
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               cooling.Max() / pipe);
                }

                if (loadTypes == LoadTypes.Heating)
                {
                    var pipe = DistrictControl.Instance.ListOfPlantSettings
                        .Where(o => o.LoadType == LoadTypes.Transport)
                        .Select(o => o.ConversionMatrix[loadTypes]).Aggregate((a, x) => a * x);
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                                   heating.ToList().GetRange(i, dt).Sum() / pipe);
                    }
                    // Peak Condition
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               heating.Max() / pipe);
                }

                if (loadTypes == LoadTypes.Elec)
                {
                    for (int i = 0; i < timeSteps * dt; i += dt)
                    {
                        solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == i)
                                       .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray()
                                       .Sum() ==
                                   electricity.ToList().GetRange(i, dt).Sum());
                    }

                    //Peak Condition
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               electricity.Max());
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
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes) && k.Key.Item1 == 8760)
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               0);
                }
            }

            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            // Set the Objective Function
            var objective = solver.Objective();

            foreach (var supplymodule in P)
                objective.SetCoefficient(supplymodule.Value, supplymodule.Key.Item2.F + supplymodule.Key.Item2.V * dt);

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

            foreach (var plant in DistrictControl.Instance.ListOfPlantSettings.Where(o =>
                o.LoadType != LoadTypes.Transport))
            {
                var solutionValues = P.Where(o => o.Key.Item2.Name == plant.Name).Select(v => v.Value.SolutionValue());
                var cap = solutionValues.Last();
                var energy = solutionValues.ToList().GetRange(0,solutionValues.Count()-1).Sum();
                plant.Output = solutionValues.ToArray();
                RhinoApp.WriteLine($"{plant.Name} = {cap} Peak ; {energy} Annum");
            }

            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
        }
    }
}