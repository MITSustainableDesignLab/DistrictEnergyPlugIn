using System;
using System.Linq;
using DistrictEnergy.Helpers;
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

        static void Main()
        {
            DHSimulateDistrictEnergy.Instance.PreSolve();
            DataModel data = new DataModel();
            // Create the linear solver with the CBC backend.
            Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");

            // Define Model Variables. Here each variable is the supply power of each available supply module
            var plants = DistrictControl.Instance.ListOfPlantSettings;
            Variable[] x = new Variable[plants.Count];
            foreach (var supplymodule in plants)
            {
                var i = plants.IndexOf(supplymodule);
                x[i] = solver.MakeNumVar(0.0, double.PositiveInfinity, String.Format($"x_{i} {supplymodule.Name}"));
                x[i].SetUb(supplymodule.Capacity);
            }

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            var cooling = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Sum();
            var heating = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum();
            var electricity = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Sum();

            solver.Add(x.Sum() == cooling);


            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            var objective = solver.Objective();
            foreach (var supplymodule in plants)
            {
                var i = plants.IndexOf(supplymodule);
                double dt = 8760;
                objective.SetCoefficient(x[i], supplymodule.F + supplymodule.V * dt);
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

            foreach (var supplymodule in plants)
            {
                var i = plants.IndexOf(supplymodule);
                RhinoApp.WriteLine($"{x[i].Name()} =  + {x[i].SolutionValue()}");
            }

            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
        }
    }
}