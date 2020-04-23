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

        static void Main()
        {
            // Create the linear solver with the CBC backend.
            Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");

            // x and y are integer non-negative variables.
            Variable x = solver.MakeIntVar(0.0, double.PositiveInfinity, "x");
            Variable y = solver.MakeIntVar(0.0, double.PositiveInfinity, "y");

            RhinoApp.WriteLine("Number of variables = " + solver.NumVariables());

            // x + 7 * y <= 17.5.
            solver.Add(x + 7 * y <= 17.5);

            // x <= 3.5.
            solver.Add(x <= 3.5);

            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            // Maximize x + 10 * y.
            solver.Maximize(x + 10 * y);

            Solver.ResultStatus resultStatus = solver.Solve();

            // Check that the problem has an optimal solution.
            if (resultStatus != Solver.ResultStatus.OPTIMAL)
            {
                RhinoApp.WriteLine("The problem does not have an optimal solution!");
                return;
            }
            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine("Objective value = " + solver.Objective().Value());
            RhinoApp.WriteLine("x = " + x.SolutionValue());
            RhinoApp.WriteLine("y = " + y.SolutionValue());

            RhinoApp.WriteLine("\nAdvanced usage:");
            RhinoApp.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
            RhinoApp.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
            RhinoApp.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
        }
    }
}