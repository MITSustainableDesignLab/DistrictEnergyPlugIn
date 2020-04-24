using System;
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
        static DHRunLPModel _instance;

        public DHRunLPModel()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DHRunLPModel command.</summary>
        public static DHRunLPModel Instance => _instance;

        public override string EnglishName => "DHRunLPModel";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Main();
            return Result.Success;
        }

        class DataModel
        {
            public double[,] ConstraintCoeffs =
            {
                {5, 7, 9, 2, 1},
                {18, 4, -9, 10, 12},
                {4, 7, 3, 8, 5},
                {5, 13, 16, 3, -7},
            };

            public double[] Bounds = {250, 285, 211, 315};
            public double[] ObjCoeffs = {7, 8, 2, 9, 6};
            public int NumVars = 5;
            public int NumConstraints = 4;
        }

        static Variable[] To1DArray(Variable[,] input)
        {
            // Step 1: get total size of 2D array, and allocate 1D array.
            int size = input.Length;
            Variable[] result = new Variable[size];

            // Step 2: copy 2D array elements into a 1D array.
            int write = 0;
            for (int i = 0; i <= input.GetUpperBound(0); i++)
            {
                for (int z = 0; z <= input.GetUpperBound(1); z++)
                {
                    result[write++] = input[i, z];
                }
            }
            // Step 3: return the new array.
            return result;
        }

        static void Main()
        {
            DHSimulateDistrictEnergy.Instance.PreSolve();
            DataModel data = new DataModel();
            // Create the linear solver with the CBC backend.
            Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");

            // Define Model Variables. Here each variable is the supply power of each available supply module
            var xplants = DistrictControl.Instance.ListOfPlantSettings.Where(o=> o.LoadType == LoadTypes.Cooling).ToList();
            var yplants = DistrictControl.Instance.ListOfPlantSettings.Where(o => o.LoadType == LoadTypes.Heating).ToList();
            var zplants = DistrictControl.Instance.ListOfPlantSettings.Where(o => o.LoadType == LoadTypes.Elec).ToList();
            int timeSteps = 1;
            Variable[,] x = new Variable[xplants.Count, timeSteps];
            Variable[,] y = new Variable[yplants.Count, timeSteps];
            Variable[,] z = new Variable[zplants.Count, timeSteps];
            foreach (var supplymodule in xplants)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    var i = xplants.IndexOf(supplymodule);
                    x[i, t] = solver.MakeNumVar(0.0, supplymodule.Capacity, String.Format($"x_{i}_{t} {supplymodule.Name}"));
                }
            }
            foreach (var supplymodule in yplants)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    var i = yplants.IndexOf(supplymodule);
                    y[i, t] = solver.MakeNumVar(0.0, supplymodule.Capacity, String.Format($"y_{i}_{t} {supplymodule.Name}"));
                }
            }
            foreach (var supplymodule in zplants)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    var i = zplants.IndexOf(supplymodule);
                    z[i, t] = solver.MakeNumVar(0.0, supplymodule.Capacity, String.Format($"z_{i}_{t} {supplymodule.Name}"));
                }
            }

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            var cooling = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Sum();
            var heating = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum();
            var electricity = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum();

            // Set Constraints
            solver.Add(To1DArray(x).Sum() == cooling);
            solver.Add(To1DArray(y).Sum() == heating);
            solver.Add(To1DArray(z).Sum() == electricity);
            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            // Set the Objective Function
            var objective = solver.Objective();
            foreach (var supplymodule in xplants)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    var i = xplants.IndexOf(supplymodule);
                    double dt = 8760;
                    objective.SetCoefficient(x[i, t], supplymodule.F + supplymodule.V * dt);
                }
            }
            foreach (var supplymodule in yplants)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    var i = yplants.IndexOf(supplymodule);
                    double dt = 8760;
                    objective.SetCoefficient(y[i, t], supplymodule.F + supplymodule.V * dt);
                }
            }
            foreach (var supplymodule in zplants)
            {
                for (int t = 0; t < timeSteps; t++)
                {
                    var i = zplants.IndexOf(supplymodule);
                    double dt = 8760;
                    objective.SetCoefficient(z[i, t], supplymodule.F + supplymodule.V * dt);
                }
            }

            objective.SetMinimization();

            var lp = solver.ExportModelAsLpFormat(true);

            Solver.ResultStatus resultStatus = solver.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("The problem does not have an optimal solution!");
                return;
            }

            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine("Optimal objective value = " + solver.Objective().Value());

            foreach (var variable in solver.variables())
            {
                RhinoApp.WriteLine($"{variable.Name()} =  + {variable.SolutionValue()}");
            }

            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
        }
    }
}