using Rhino;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using TrnsysUmiPlatform.TrnDll;
using TrnsysUmiPlatform.Types;

namespace TrnsysUmiPlatform
{
    public class TrnsysModel
    {
        /// <summary>
        /// A TrnsysModel sets information
        /// </summary>
        /// <param name="modelname">The name of the Project</param>
        /// <param name="hourlytimestep">The Timestep</param>
        /// <param name="weather"></param>
        /// <param name="projectcreator"></param>
        /// <param name="description"></param>
        /// <param name="directory">Where to save the dck file and performe the simulation</param>
        public TrnsysModel(string modelname, int hourlytimestep, string weather, string projectcreator, string description, string directory)
        {
            ModelName = modelname;
            HourlyTimestep = hourlytimestep;
            WeatherFile = weather;
            ProjectCreator = projectcreator;
            CreationDate = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            ModifiedDate = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            Description = description;
            WorkingDirectory = directory;
        }

        public string ModelName { get; set; }
        public double HourlyTimestep { get; set; }
        public string PlantSelection { get; set; }
        public string WeatherFile { get; set; }
        public string ProjectCreator { get; set; }
        public string CreationDate { get; set; }
        public string ModifiedDate { get; set; }
        public string Description { get; set; }
        public string WorkingDirectory { get; set; }

        public void WriteDckFile()
        {
            throw new NotImplementedException();
        }

        public void WriteDckFile(IEnumerable<Type31> pipes, IEnumerable<Type11> diverters)
        {
            var filename = Path.Combine(WorkingDirectory, ModelName + ".dck");

            using (var file = new StreamWriter(filename, false, Encoding.Default))
            {
                // Write the begining of the deck file.
                var intro = new Intro(ProjectCreator, CreationDate, ModifiedDate, Description);
                file.WriteLine(intro.WriteIntro());

                // Write the control cars.
                var controlcards = new ControlCards(0, 8760, HourlyTimestep);
                file.WriteLine(controlcards.WriteControlCards());

                var weather2 = new Type15_3(WeatherFile) { Position = new double[] { 179, 596 } };
                file.WriteLine(weather2.WriteType());


                var pump = new Type741(10, 4.19, 1000, 0) { Position = new double[] { 315, 596 } };
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
        }

        // ReSharper disable once UnusedMember.Global
        public void RunTrnsysExe()
        {
            var deckfilename = Path.Combine(WorkingDirectory, ModelName + ".dck");
            var command = " " + deckfilename + " /n";

            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = CreateDckFile.get_trnsys_exe(),
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                process.Start();

                // Read the streams
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                var exitCode = process.ExitCode;

                RhinoApp.WriteLine("output>>" + (string.IsNullOrEmpty(output) ? "(none)" : output));
                RhinoApp.WriteLine("error>>" + (string.IsNullOrEmpty(error) ? "(none)" : error));
                RhinoApp.WriteLine("ExitCode: " + exitCode, "ExeciteCommand");
                process.Close();
            }

        }

        public void RunTrnsys(bool silentMode)
        {
            Environment.CurrentDirectory = @"C:\Trnsys\17-2-Bee\Exe\"; //This hardcoded path will need to be changed
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            //Load library wrapper
            var trndll = new TrnDllWrapper();

            //var deckPath = Path.Combine(WorkingDirectory, ModelName + ".dck");
            var deckPath = @"C:\Trnsys\17-2-Bee\Examples\Restaurant\Restaurant.dck";


            //Initialization call to Trnsys
            TrnDllWrapper.GetTrnsysCallOutputs trnsysCallResult = InitialzingCallMethod(silentMode, trndll, deckPath);

            var callType = trnsysCallResult.CallType;
            var startTime = trnsysCallResult.ParOut[0];
            var stopTime = trnsysCallResult.ParOut[1];
            var timeStep = trnsysCallResult.ParOut[2];

            var nSteps = (stopTime - startTime) / timeStep + 1;

            //Progress Bar
            Rhino.UI.StatusBar.HideProgressMeter();
            Rhino.UI.StatusBar.ShowProgressMeter(0, Convert.ToInt32(nSteps), "TRNSYS simulation", true, true);

            LinePerLineCallMethod(silentMode, trndll, callType, startTime, stopTime, timeStep, deckPath);

            trnsysCallResult = FinalCallMethod(silentMode, trndll, deckPath);

            Rhino.UI.StatusBar.HideProgressMeter();

            stopWatch.Stop();
            Console.WriteLine(DateTime.Now + " - Trnsys simulation was completed successfully. Elapsed time : {0}",
                stopWatch.Elapsed);

            Console.WriteLine("Last CallType:");
            Console.WriteLine(trnsysCallResult.CallType);
            Console.WriteLine("Done library test");
        }

        private static TrnDllWrapper.GetTrnsysCallOutputs InitialzingCallMethod(bool silentMode, TrnDllWrapper trndll,
            string deckPath)
        {
            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Initializing TRNSYS");

            var callType = 0;

            TrnDllWrapper.GetTrnsysCallOutputs trnsysCallOutputs = trndll.Trnsys(callType, deckPath);

            return trnsysCallOutputs;
        }

        private static void LinePerLineCallMethod(bool silentMode, TrnDllWrapper trndll, int callType, double startTime,
            double stopTime, double timeStep, string deckPath)
        {
            var callNo = 2;
            var nSteps = (stopTime - startTime) / timeStep + 1;

            // Call trnsys once per time step

            if (!silentMode)
                RhinoApp.WriteLine(DateTime.Now + " - Running Trnsys Simulation");

            while (callType == 0 && callNo < nSteps + 1)
            {
                callNo++;
                callType = 1;

                if (!silentMode)
                    Rhino.UI.StatusBar.UpdateProgressMeter(callNo, true);

                TrnDllWrapper.GetTrnsysCallOutputs trnsysCallOutputs = trndll.Trnsys(callType, deckPath);

                callType = trnsysCallOutputs.CallType;

            }

            if (callType != 0)
                RhinoApp.WriteLine("Fatal error at time = {0}" + " - check log file for details",
                    startTime + (callNo - 2) * timeStep);
        }

        private static TrnDllWrapper.GetTrnsysCallOutputs FinalCallMethod(bool silentMode, TrnDllWrapper trndll, string deckPath)
        {
            if (!silentMode)
                RhinoApp.WriteLine(DateTime.Now + " - Performing final call to Trnsys");
            var callType = -1; //Final Call

            TrnDllWrapper.GetTrnsysCallOutputs trnsysCallOutputs = trndll.Trnsys(callType, deckPath);
            callType = trnsysCallOutputs.CallType;

            if (callType != 1000)
                RhinoApp.WriteLine("Fatal error during final call - check log file for details");

            return trnsysCallOutputs;
        }
    }
}
