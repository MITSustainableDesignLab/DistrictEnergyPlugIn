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
            var timeSteps = 8760;

            var P =
                new Dictionary<(int, IThermalPlantSettings), Variable>();
            foreach (var supplymodule in DistrictControl.Instance.ListOfPlantSettings.Where(o =>
                o.LoadType != LoadTypes.Transport))
                for (var t = 0; t < timeSteps; t++)
                    P[(t, supplymodule)] = solver.MakeNumVar(0.0, supplymodule.Capacity,
                        string.Format($"P_{t}_{supplymodule.Name}"));

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            var cooling = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Sum();
            var heating = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum();
            var electricity = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum();

            // Set Load Balance Constraints
            foreach (var loadTypes in P.Select(x => x.Key.Item2.LoadType).Distinct())
            {
                if (loadTypes == LoadTypes.Cooling)
                {
                    var pipe = DistrictControl.Instance.ListOfPlantSettings
                        .Where(o => o.LoadType == LoadTypes.Transport)
                        .Select(o => o.ConversionMatrix[loadTypes]).Aggregate((a, x) => a * x);
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes))
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               cooling / pipe);
                }

                if (loadTypes == LoadTypes.Heating)
                {
                    var pipe = DistrictControl.Instance.ListOfPlantSettings
                        .Where(o => o.LoadType == LoadTypes.Transport)
                        .Select(o => o.ConversionMatrix[loadTypes]).Aggregate((a, x) => a * x);
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes))
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               heating / pipe);
                }

                if (loadTypes == LoadTypes.Elec)
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes))
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               electricity);

                if (loadTypes == LoadTypes.Gas)
                    solver.Add(P.Where(k => k.Key.Item2.ConversionMatrix.ContainsKey(loadTypes))
                                   .Select(k => k.Value * k.Key.Item2.ConversionMatrix[loadTypes]).ToArray().Sum() ==
                               0);
            }

            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            // Set the Objective Function
            var objective = solver.Objective();


            double dt = 1;
            foreach (var supplymodule in P)
                objective.SetCoefficient(supplymodule.Value, supplymodule.Key.Item2.F + supplymodule.Key.Item2.V * dt);

            objective.SetMinimization();

            var lp = solver.ExportModelAsLpFormat(false);

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
                var cap = solutionValues.Max();
                var energy = solutionValues.Sum();
                RhinoApp.WriteLine($"{plant.Name} = {cap} Peak ; {energy} Annum");
            }

            // foreach (var variable in solver.variables())
            // {
            //     RhinoApp.WriteLine($"{variable.Name()} =  + {variable.SolutionValue()}");
            // }

            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
        }
    }
}