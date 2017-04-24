using System;
using System.IO;
using System.Diagnostics;
using Rhino;
using System.Collections.Generic;

namespace TrnsysUmiPlatform
{
    public class RunTrnsys
    {
        public RunTrnsys(TrnsysModel trnsys_model)
        {
            string deckfilename = Path.Combine(trnsys_model.WorkingDirectory, trnsys_model.ModelName + ".dck");
            string command = " " + deckfilename + " /n";
            int exitCode;

            Process process = new Process();
            process.StartInfo.FileName = CreateDckFile.get_trnsys_exe();
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Read the streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            RhinoApp.WriteLine("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            RhinoApp.WriteLine("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            RhinoApp.WriteLine("ExitCode: " + exitCode.ToString(), "ExeciteCommand");
            process.Close();
        }
    }

    public class WriteDckFile
    {
        private TrnsysModel trnsys_model;

        public WriteDckFile(TrnsysModel trnsys_model)
        {
            this.trnsys_model = trnsys_model;
        }

        public WriteDckFile(TrnsysModel trnsys_model, List<Type31> pipes, List<Type11> diverters)
        {
            string filename = Path.Combine(trnsys_model.WorkingDirectory, trnsys_model.ModelName + ".dck");

            using (StreamWriter file = new StreamWriter(filename, false, System.Text.Encoding.Default))
            {
                // Write the begining of the deck file.
                Intro intro = new Intro(trnsys_model.ProjectCreator, trnsys_model.CreationDate, trnsys_model.ModifiedDate, trnsys_model.Description);
                file.WriteLine(intro.WriteIntro());

                // Write the control cars.
                ControlCards controlcards = new ControlCards(0, 8760, trnsys_model.HourlyTimestep);
                file.WriteLine(controlcards.WriteControlCards());

                Type15_3 weather2 = new Type15_3(trnsys_model.WeatherFile);
                weather2.Position = new double[2] { 179, 596 };
                file.WriteLine(weather2.WriteType());


                Type741 pump = new Type741(0, 0, 0, 0);
                pump.Position = new double[2] { 315, 596 };
                file.WriteLine(pump.WriteType());

                // Write Type31 (pipes)
                foreach (Type31 _pipe in pipes)
                {
                    file.WriteLine(_pipe.WriteType());
                }

                // Write Diverters
                foreach (Type11 _diverter in diverters)
                {
                    file.WriteLine(_diverter.WriteType());
                }

                file.WriteLine("END\r\n");
                file.Close();
            }
            //return;
        }
    }
}