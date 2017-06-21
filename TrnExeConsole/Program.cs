﻿using System;
using System.Diagnostics;

namespace TrnExeConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var silentMode = false;

            //Load library wrapper
            var trndll = new TrnDllWrapper(@"C:\Trnsys\17-2-Bee\Exe\TRNDll.dll");

            //Initialization call to Trnsys
            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Initializing TRNSYS");

            var callNo = 1;
            var callType = 0;
            double[] parout;
            double[] plotout;
            var deckPath = @"C:\Trnsys\17-2-Bee\Examples\Photovoltaics\IVcurve.dck";
            callType = trndll.Trnsys(callType, out parout, out plotout, deckPath);

            var startTime = parout[0];
            var stopTime = parout[1];
            var timeStep = parout[2];

            GetTrnsysCallOutputs linePerLineOutputs = LinePerLineCallMethod(trndll, silentMode, callNo, callType,
                startTime, stopTime, timeStep, @"C:\Trnsys\17-2-Bee\Examples\Photovoltaics\IVcurve.dck");
            parout = linePerLineOutputs.parOut;
            plotout = linePerLineOutputs.plotOut;

            GetTrnsysCallOutputs finalCallOutputs = FinalCall(trndll, silentMode, callNo, callType,
                @"C:\Trnsys\17-2-Bee\Examples\Photovoltaics\IVcurve.dck");
            parout = finalCallOutputs.plotOut;
            plotout = finalCallOutputs.parOut;


            stopWatch.Stop();
            Console.WriteLine(DateTime.Now + " - Trnsys simulation was completed successfully. Elapsed time : {0}",
                stopWatch.Elapsed);

            Console.WriteLine("CallType:");
            Console.WriteLine(callType);
            Console.WriteLine("Done library test");

#if DEBUG
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
#endif
        }

        private static GetTrnsysCallOutputs LinePerLineCallMethod(TrnDllWrapper trndll, bool silentMode, int callNo,
            int callType, double startTime, double stopTime, double timeStep, string deckPath)
        {
            double[] parOut;
            double[] plotOut;
            var nSteps = (stopTime - startTime) / timeStep + 1;

            // Call trnsys once per time step

            var percentCompletedPrinted = 0.0;
            var percentCompleted = 0.0;

            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Running Trnsys Simulation");

            while (callType == 0 && callNo < nSteps + 1)
            {
                callNo = callNo++;
                callType = 1;

                if (!silentMode)
                    if (percentCompleted > percentCompletedPrinted)
                        ProgressBar.DrawTextProgressBar(callNo, Convert.ToInt32(nSteps));

                trndll.Trnsys(callType, out parOut, out plotOut, deckPath);
            }

            if (callType != 0)
                Console.WriteLine("Fatal error at time = {0}" + " - check log file for details",
                    startTime + (callNo - 2) * timeStep);

            return new GetTrnsysCallOutputs();
        }

        private static GetTrnsysCallOutputs FinalCall(TrnDllWrapper trndll, bool silentMode, int callNo, int callType,
            string deckPath)
        {
            double[] parout;
            double[] plotout;
            // Final call
            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Performing final call to Trnsys");
            callNo = callNo++;
            callType = -1; // Final call

            trndll.Trnsys(callType, out parout, out plotout, deckPath);

            if (callType != 1000)
                Console.WriteLine("Fatal error during final call - check log file for details");
            return new GetTrnsysCallOutputs();
        }

        private class GetTrnsysCallOutputs
        {
            public double[] parOut { get; set; }
            public double[] plotOut { get; set; }
        }
    }
}
