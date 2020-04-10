using System;
using System.Collections;
using System.Collections.Generic;
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
            // Create the linear solver with the GLOP backend.
            Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");
            var piplines = 3;
            var N_nodes = 4;
            var gen = 2;
            var press_ref = 10;

            int[,] P_pipes = new int[piplines, N_nodes];
            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < N_nodes; j++)
                {
                    P_pipes[i, j] = 0;
                }
            }

            P_pipes[0, 0] = 1;
            P_pipes[0, 1] = -1;

            P_pipes[1, 1] = 1;
            P_pipes[1, 2] = -1;

            P_pipes[2, 1] = 1;
            P_pipes[2, 3] = -1;

            int[] P_dem = new int[N_nodes];
            for (int i = 0; i < N_nodes; i++)
            {
                P_dem[i] = 0;
            }

            P_dem[2] = 300;
            P_dem[3] = 400;

            // Supply source matrix
            var Sup_num = 1;
            int[] S_supply = new int[N_nodes];
            for (int i = 0; i < N_nodes; i++)
            {
                S_supply[i] = 0;
            }

            S_supply[0] = 1;

            var Sup_max = 800; // min limit 700
            var cost_supply = 25;

            // Pipes lines max capacities/ flow limits

            int[,] flow_lim = new int[piplines, 2];
            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (j == 0) flow_lim[i, j] = 0; // lb
                    else flow_lim[i, j] = 1000; // ub
                }
            }

            // Flow constant

            var K_flow = 10000;

            // Pressure limit

            int[,] pres_lim = new int[N_nodes, 2];
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (j == 0) pres_lim[i, j] = 0;
                    else pres_lim[i, j] = 1000;
                }
            }

            // Defining the variable and function value points

            var Num = 15; // Number of evaluation points
            var Psec = Num - 1; // Number of linear sections

            double[,] pres_eval = new double[N_nodes, Num]; // nodes in rows and values in columns
            double[,] flow_eval = new double[N_nodes, Num]; // nodes in rows and values in columns
            double[,] pres_value = new double[N_nodes, Num];
            double[,] flow_value = new double[N_nodes, Num];
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < Num; j++)
                {
                    pres_eval[i, j] = (
                        pres_lim[i, 0] + ((pres_lim[i, 1] - pres_lim[i, 0]) / Psec) * j
                    );
                    pres_value[i, j] = Math.Pow(pres_eval[i, j], 2);
                }
            }

            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < Num; j++)
                {
                    flow_eval[i, j] = (
                        flow_lim[i, 0] + ((flow_lim[i, 1] - flow_lim[i, 0]) / Psec) * j
                    );
                    flow_value[i, j] = Math.Pow(flow_eval[i, j], 2);
                }
            }

            List<Variable> pres_var = new List<Variable>();
            // Defining Model Variables
            for (int i = 0; i < N_nodes; i++)
            {
                pres_var.Add(solver.MakeNumVar(pres_lim[i, 0], pres_lim[i, 1], $"Pres Var {i}"));
            }

            // pressure variable for each node quadratic
            List<Variable> flow_var = new List<Variable>();
            for (int i = 0; i < piplines; i++)
            {
                flow_var.Add(solver.MakeNumVar(flow_lim[i, 0], flow_lim[i, 1], $"Flow Var {i}"));
            }

            List<Variable> pres_var_sq = new List<Variable>();
            for (int i = 0; i < N_nodes; i++)
            {
                pres_var_sq.Add(solver.MakeNumVar(Math.Pow(pres_lim[i, 0], 2), Math.Pow(pres_lim[i, 1], 2),
                    $"Pres Var Squared {i}"));
            }

            List<Variable> flow_var_sq = new List<Variable>();
            for (int i = 0; i < piplines; i++)
            {
                flow_var_sq.Add(solver.MakeNumVar(Math.Pow(flow_lim[i, 0], 2), Math.Pow(flow_lim[i, 1], 2),
                    $"Flow Var Squared {i}")); 
            }

            Dictionary<(int, int), Variable> pres_lin = new Dictionary<(int, int), Variable>();
            Dictionary<(int, int), Variable> flow_lin = new Dictionary<(int, int), Variable>();
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < Psec; j++)
                {
                    pres_lin[(i, j)] = solver.MakeNumVar(0, 1, $"pres_lin_p{i}_sec{j}");
                }
            }

            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < Psec; j++)
                {
                    flow_lin[(i, j)] = solver.MakeNumVar(0, 1, $"flow_lin_p{i}_sec{j}");
                }
            }

            Dictionary<(int, int), Variable> pres_fill = new Dictionary<(int, int), Variable>();
            Dictionary<(int, int), Variable> flow_fill = new Dictionary<(int, int), Variable>();
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < Psec - 1; j++)
                {
                    pres_fill[(i, j)] = solver.MakeBoolVar($"pres_fill_n{i}_sec{j}");
                }
            }

            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < Psec - 1; j++)
                {
                    flow_fill[(i, j)] = solver.MakeBoolVar($"flow_fill_p{i}_sec{j}");
                }
            }

            Dictionary<int, Variable> Sup_var = new Dictionary<int, Variable>();
            for (int i = 0; i < Sup_num; i++)
            {
                Sup_var[i] = solver.MakeNumVar(0, Sup_max, $"Supplied_amount_{i}");
            }

            // Constraints

            Dictionary<int, Constraint> pres_con = new Dictionary<int, Constraint>();
            Dictionary<int, Constraint> pres_con_sq = new Dictionary<int, Constraint>();

            LinearExpr[] sum_array = new LinearExpr[Psec];
            LinearExpr[] sum_sq_array = new LinearExpr[Psec];
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < Psec; j++)
                {
                    sum_array[j] = (pres_eval[i, j + 1] - pres_eval[i, j]) * pres_lin[(i, j)];
                    sum_sq_array[j] = (pres_value[i, j + 1] - pres_value[i, j]) * pres_lin[(i, j)];
                }
                pres_con[i] = solver.Add(pres_var[i] == pres_lim[i, 0] + sum_array.Sum());
                pres_con_sq[i] = solver.Add(pres_var_sq[i] == pres_value[i, 0] + sum_array.Sum());
            }

            Dictionary<int, Constraint> flow_con = new Dictionary<int, Constraint>();
            Dictionary<int, Constraint> flow_con_sq = new Dictionary<int, Constraint>();

            LinearExpr[] sum_flow = new LinearExpr[Psec];
            LinearExpr[] sum_flow_sq = new LinearExpr[Psec];
            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < Psec; j++)
                {
                    sum_flow[j] = (flow_eval[i, j + 1] - flow_eval[i, j]) * flow_lin[(i, j)];
                    sum_flow_sq[j] = (flow_value[i, j + 1] - flow_value[i, j]) * flow_lin[(i, j)];
                }
                flow_con[i] = solver.Add(flow_var[i] == flow_lim[i, 0] + sum_flow.Sum());
                flow_con_sq[i] = solver.Add(flow_var_sq[i] == flow_value[i, 0] + sum_flow_sq.Sum());
            }

            Dictionary<(int, int), Constraint> pres_lin_con = new Dictionary<(int, int), Constraint>();
            Dictionary<(int, int), Constraint> pres_lin_cona = new Dictionary<(int, int), Constraint>();
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < Psec - 1; j++)
                {
                    pres_lin_con[(i, j)] = solver.Add(pres_lin[(i, j + 1)] <= pres_fill[(i, j)]);
                    pres_lin_cona[(i, j)] = solver.Add(pres_fill[(i, j)] <= pres_lin[(i, j)]);
                }
            }

            Dictionary<(int, int), Constraint> flow_lin_con = new Dictionary<(int, int), Constraint>();
            Dictionary<(int, int), Constraint> flow_lin_cona = new Dictionary<(int, int), Constraint>();
            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < Psec - 1; j++)
                {
                    flow_lin_con[(i, j)] = solver.Add(flow_lin[(i, j + 1)] <= flow_fill[(i, j)]);
                    flow_lin_cona[(i, j)] = solver.Add(flow_fill[(i, j)] <= flow_lin[(i, j)]);
                }
            }

            // Reference pressure constraint
            Constraint pres_ref_con = solver.Add(pres_var[0] == press_ref);

            Dictionary<int, Constraint> balance_con = new Dictionary<int, Constraint>();
            LinearExpr[] sum_supply = new LinearExpr[piplines];
            for (int i = 0; i < N_nodes; i++)
            {
                for (int j = 0; j < piplines; j++)
                {
                    sum_supply[j] = P_pipes[j, i] * (-1) * flow_var[j];
                }

                balance_con[i] = solver.Add(Sup_var[0] * S_supply[i] + sum_supply.Sum() == P_dem[i]);
            }

            Dictionary<int, Constraint> transmis_con = new Dictionary<int, Constraint>();
            LinearExpr[] sum_transmis = new LinearExpr[N_nodes];
            for (int i = 0; i < piplines; i++)
            {
                for (int j = 0; j < N_nodes; j++)
                {
                    sum_transmis[j] = P_pipes[i, j] * pres_var_sq[j];
                }

                transmis_con[i] = solver.Add(flow_var_sq[i] <= K_flow * sum_transmis.Sum());
            }

            RhinoApp.WriteLine("Number of constraints = " + solver.NumConstraints());

            // Create the objective function, a_e * P_e**2 + b_e * P_e + g_e + b_g*P_g + g_g
            Objective objective = solver.Objective();
            objective.SetCoefficient(Sup_var[0], cost_supply);
            objective.SetMinimization();

            solver.Solve();

            solver.EnableOutput();

            RhinoApp.WriteLine("Solution:");
            RhinoApp.WriteLine("Objective value = " + solver.Objective().Value());
            //RhinoApp.WriteLine("x = " + x.SolutionValue());
            //RhinoApp.WriteLine("y = " + y.SolutionValue());
        }
    }
}