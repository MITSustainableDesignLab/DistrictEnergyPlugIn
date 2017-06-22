using System;
using System.Runtime.InteropServices;

namespace TrnExeConsole
{
    public class GetTrnsysCallOutputs
    {
        public GetTrnsysCallOutputs(double[] parOut, double[] plotOut, int callType)
        {
            ParOut = parOut;
            PlotOut = plotOut;
            CallType = callType;
        }

        public double[] ParOut { get; }
        public double[] PlotOut { get; }
        public int CallType { get; }
    }

    public class TrnDllWrapper
    {
        // Delegate with function signature for the Trnsys function
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void TrnsysDelegate(
            [Out] [In] ref int callType,
            [Out] [In] double[] parout,
            [Out] [In] double[] plotout,
            [Out] [In] char[] labels,
            [Out] [In] char[] titles,
            [Out] [In] char[] deckn);

        public GetTrnsysCallOutputs Trnsys(int callType, string deckn)
        {
            double[] parOut;
            double[] plotOut;

            if (_trnsys != null)
            {
                //Arguments used when calling TRNSYS in TRNDll
                var lpa = 1000;
                var lpl = 1000;
                var parout = new double[lpa];
                var plotout = new double[lpl];
                var lengthLabels = 4000;
                var lengthTitles = 1500;
                var lengthDeckn = 300;
                var labels = new char[lengthLabels];
                var titles = new char[lengthTitles];
                var deckPath = new char[lengthDeckn];

                for (var i = 0; i < deckn.Length; i++)
                    deckPath[i] = deckn[i];

                //Trnsys call
                _trnsys(ref callType, parout, plotout, labels, titles, deckPath);

                parOut = parout;
                plotOut = plotout;

                // Return string
                return new GetTrnsysCallOutputs(parOut, plotOut, callType);
            }
            return new GetTrnsysCallOutputs(null, null, -2);
        }

        public TrnDllWrapper()
        {
            var trnDll =
                NativeLibrary.GetLibraryPathname(
                    @"C:\Trnsys\17-2-Bee\Compilers\Ivf15 (Vs2013)\TRNDll\Debug\TRNDll.dll");
            _dllhandle = NativeLibrary.LoadLibrary(trnDll);

            if (_dllhandle == IntPtr.Zero)
                return;

            IntPtr trnsysHandle = NativeLibrary.GetProcAddress(_dllhandle, "TRNSYS");

            if (trnsysHandle != IntPtr.Zero)
                _trnsys = (TrnsysDelegate) Marshal.GetDelegateForFunctionPointer(
                    trnsysHandle,
                    typeof(TrnsysDelegate));
            else
                Console.WriteLine("Fatal error. Routine named Trnsys not found in TRNDll");
        }

        ~TrnDllWrapper()
        {
            //Free ressources.
            //Probably should use SafeHandle or some similar class
            //But this will do for now.
            NativeLibrary.FreeLibrary(_dllhandle);
        }

        // Handles and delegates values


        private readonly IntPtr _dllhandle;
        private readonly TrnsysDelegate _trnsys;
    }
}