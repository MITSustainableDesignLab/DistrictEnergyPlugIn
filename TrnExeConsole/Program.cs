using System;
using System.Diagnostics;
using TrnsysConsoleApp;

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
            var deckPath = @"C:\Trnsys\17-2-Bee\Examples\Restaurant\Restaurant.dck";

            callType = trndll.Trnsys(callType, out parout, out plotout, deckPath);

            var startTime = parout[0];
            var stopTime = parout[1];
            var timeStep = parout[2];

            callType = LinePerLineCallMethod(trndll, silentMode, callNo, callType,
                startTime, stopTime, timeStep, deckPath);

            callNo = -1;
            callType = FinalCall(trndll, silentMode,
                deckPath);


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

        private static int LinePerLineCallMethod(TrnDllWrapper trndll, bool silentMode, int callNo, int callType, double startTime, double stopTime, double timeStep, string deckPath)
        {
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
                        progress.Report((double)callNo / nSteps);

                    double[] parOut;
                    double[] plotOut;
                    callType = trndll.Trnsys(callType, out parOut, out plotOut, deckPath);
                }

            }

            if (callType != 0)
                Console.WriteLine("Fatal error at time = {0}" + " - check log file for details",
                    startTime + (callNo - 2) * timeStep);

            return callType;
        }

        private static int FinalCall(TrnDllWrapper trndll, bool silentMode, string deckPath)
        {
            double[] parout;
            double[] plotout;
            // Final call
            if (!silentMode)
                Console.WriteLine(DateTime.Now + " - Performing final call to Trnsys");
            var callType = -1; // Final call

            callType = trndll.Trnsys(callType, out parout, out plotout, deckPath);

            if (callType != 1000)
                Console.WriteLine("Fatal error during final call - check log file for details");
            return callType;
        }
    }
}
