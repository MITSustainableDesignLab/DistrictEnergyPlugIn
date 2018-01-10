using System;
using System.Diagnostics;

namespace TrnExeConsole
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var silentMode = false;

            //Load library wrapper
            var trndll = new TrnDllWrapper();

            var deckPath = @"C:\Trnsys\17-2-Bee\Examples\Restaurant\Restaurant.dck";

            //Initialization call to Trnsys
            GetTrnsysCallOutputs trnsysCallResult = InitialzingCallMethod(silentMode, trndll, deckPath);

            var startTime = trnsysCallResult.ParOut[0];
            var stopTime = trnsysCallResult.ParOut[1];
            var timeStep = trnsysCallResult.ParOut[2];
            var callType = trnsysCallResult.CallType;

            LinePerLineCallMethod(silentMode, trndll, callType, startTime, stopTime, timeStep, deckPath);

            trnsysCallResult = FinalCallMethod(silentMode, trndll, deckPath);

            stopWatch.Stop();
            Console.WriteLine(DateTime.Now + " - Trnsys simulation was completed successfully. Elapsed time : {0}",
                stopWatch.Elapsed);

            Console.WriteLine("Last CallType:");
            Console.WriteLine(trnsysCallResult.CallType);
            Console.WriteLine("Done library test");

#if DEBUG
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
#endif
        }

        private static GetTrnsysCallOutputs InitialzingCallMethod(bool silentMode, TrnDllWrapper trndll,
            string deckPath)
        {
            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Initializing TRNSYS");

            var callType = 0;

            GetTrnsysCallOutputs trnsysCallOutputs = trndll.Trnsys(callType, deckPath);

            return trnsysCallOutputs;
        }

        private static void LinePerLineCallMethod(bool silentMode, TrnDllWrapper trndll, int callType, double startTime,
            double stopTime, double timeStep, string deckPath)
        {
            var callNo = 2;
            var nSteps = (stopTime - startTime) / timeStep + 1;

            // Call trnsys once per time step

            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Running Trnsys Simulation");

            using (var progress = new ProgressBar())
            {
                while (callType == 0 && callNo < nSteps + 1)
                {
                    callNo++;
                    callType = 1;

                    if (!silentMode)
                        progress.Report(callNo / nSteps);

                    GetTrnsysCallOutputs trnsysCallOutputs = trndll.Trnsys(callType, deckPath);

                    callType = trnsysCallOutputs.CallType;
                }
            }

            if (callType != 0)
                Console.WriteLine("Fatal error at time = {0}" + " - check log file for details",
                    startTime + (callNo - 2) * timeStep);
        }

        private static GetTrnsysCallOutputs FinalCallMethod(bool silentMode, TrnDllWrapper trndll, string deckPath)
        {
            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Performing final call to Trnsys");
            var callType = -1; //Final Call

            GetTrnsysCallOutputs trnsysCallOutputs = trndll.Trnsys(callType, deckPath);
            callType = trnsysCallOutputs.CallType;

            if (callType != 1000)
                Console.WriteLine("Fatal error during final call - check log file for details");

            return trnsysCallOutputs;
        }
    }
}
