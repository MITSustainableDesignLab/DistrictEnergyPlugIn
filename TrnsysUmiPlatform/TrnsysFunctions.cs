using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Rhino;
using TrnsysUmiPlatform.Types;

namespace TrnsysUmiPlatform
{
    public class RunTrnsys
    {
        public RunTrnsys(TrnsysModel trnsys_model)
        {
            var deckfilename = Path.Combine(trnsys_model.WorkingDirectory, trnsys_model.ModelName + ".dck");
            var command = " " + deckfilename + " /n";
            int exitCode;

            var process = new Process();
            process.StartInfo.FileName = CreateDckFile.get_trnsys_exe();
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            // Read the streams
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            exitCode = process.ExitCode;

            RhinoApp.WriteLine("output>>" + (string.IsNullOrEmpty(output) ? "(none)" : output));
            RhinoApp.WriteLine("error>>" + (string.IsNullOrEmpty(error) ? "(none)" : error));
            RhinoApp.WriteLine("ExitCode: " + exitCode, "ExeciteCommand");
            process.Close();
        }
    }

    public class WriteDckFile
    {
        private TrnsysModel _trnsysModel;

        public WriteDckFile(TrnsysModel trnsysModel)
        {
            _trnsysModel = trnsysModel;
        }

        public WriteDckFile(TrnsysModel trnsysModel, IEnumerable<Type31> pipes, IEnumerable<Type11> diverters)
        {
            var filename = Path.Combine(trnsysModel.WorkingDirectory, trnsysModel.ModelName + ".dck");

            using (var file = new StreamWriter(filename, false, Encoding.Default))
            {
                // Write the begining of the deck file.
                var intro = new Intro(trnsysModel.ProjectCreator, trnsysModel.CreationDate, trnsysModel.ModifiedDate,
                    trnsysModel.Description);
                file.WriteLine(intro.WriteIntro());

                // Write the control cars.
                var controlcards = new ControlCards(0, 8760, trnsysModel.HourlyTimestep);
                file.WriteLine(controlcards.WriteControlCards());

                var weather2 = new Type15_3(trnsysModel.WeatherFile) {Position = new double[] {179, 596}};
                file.WriteLine(weather2.WriteType());


                var pump = new Type741(0, 0, 0, 0) {Position = new double[] {315, 596}};
                file.WriteLine(pump.WriteType());

                // Write Type31 (pipes)
                foreach (Type31 pipe in pipes)
                    file.WriteLine(pipe.WriteType());

                // Write Diverters
                foreach (Type11 diverter in diverters)
                    file.WriteLine(diverter.WriteType());

                file.WriteLine("END\r\n");
                file.Close();
            }
            //return;
        }
    }
}