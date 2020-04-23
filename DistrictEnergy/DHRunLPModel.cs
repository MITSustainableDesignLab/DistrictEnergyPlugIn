using System;
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
            DataModel data = new DataModel();
            // Create the linear solver with the CBC backend.
            Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");
            Variable[] x = new Variable[data.NumVars];
            for (int j = 0; j < data.NumVars; j++)
            {
                x[j] = solver.MakeIntVar(0.0, double.PositiveInfinity, String.Format("x_{0}", j));
            }

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            for (int i = 0; i < data.NumConstraints; ++i)
            {
                Constraint constraint = solver.MakeConstraint(0, data.Bounds[i], "");
                for (int j = 0; j < data.NumVars; ++j)
                {
                    constraint.SetCoefficient(x[j], data.ConstraintCoeffs[i, j]);
                }
            }

            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            var objective = solver.Objective();
            for (int j = 0; j < data.NumVars; ++j)
            {
                objective.SetCoefficient(x[j], data.ObjCoeffs[j]);
                objective.SetMaximization();

                Solver.ResultStatus resultStatus = solver.Solve();

                // Check that the problem has an optimal solution.
                if (resultStatus != Solver.ResultStatus.OPTIMAL)
                {
                    RhinoApp.WriteLine("The problem does not have an optimal solution!");
                    return;
                }

                RhinoApp.WriteLine("Solution:");
                RhinoApp.WriteLine("Optimal objective value = " + solver.Objective().Value());

                for (int k = 0; k < data.NumVars; ++k)
                {
                    RhinoApp.WriteLine("x[" + k + "] = " + x[k].SolutionValue());
                }

                RhinoApp.WriteLine("\nAdvanced usage:");
                RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
                RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
                RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
            }
        }
    }
}