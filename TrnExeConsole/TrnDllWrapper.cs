using System;
using System.Runtime.InteropServices;

namespace TrnsysConsoleApp
{
    public class TrnDllWrapper
    {
        // Delegate with function signature for the Trnsys function
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        //[return: MarshalAs(UnmanagedType.FunctionPtr)]
        delegate void TrnsysDelegate(
            [Out] [In] ref int callType,
            [Out] [In] double[] parout,
            [Out] [In] double[] plotout,
            [Out] [In] char[] labels,
            [Out] [In] char[] titles,
            [Out] [In] char[] deckn);

        public int Trnsys(int callType, out double[] parOut, out double[] plotOut, string deckn)
        {

            if (_trnsys != null)
            {
                //Arguments used when calling TRNSYS in TRNDll
                var lpa = 1000;
                var lpl = 1000;
                double[] parout = new double[lpa];
                double[] plotout = new double[lpl];
                var lengthLabels = 4000;
                var lengthTitles = 1500;
                var lengthDeckn = 300;
                char[] labels = new char[lengthLabels];
                char[] titles = new char[lengthTitles];
                char[] deckPath = new char[lengthDeckn];

                for (int i = 0; i < deckn.Length; i++)
                {
                    deckPath[i] = deckn[i];
                }

                //Trnsys call
                _trnsys(ref callType, parout, plotout, labels, titles, deckPath);

                parOut = parout;
                plotOut = plotout;

                // Return string
                return callType;
            }
            parOut = null;
            plotOut = null;
            return -2;
        }

        public TrnDllWrapper(string filename)
        {
            var trnDll = NativeLibrary.GetLibraryPathname(@"C:\Trnsys\17-2-Bee\Compilers\Ivf15 (Vs2013)\TRNDll\Debug\TRNDll.dll");
            _dllhandle = NativeLibrary.LoadLibrary(trnDll);

            if (_dllhandle == IntPtr.Zero)
            {
                return;
            }

            var trnsysHandle = NativeLibrary.GetProcAddress(_dllhandle, "TRNSYS");

            if (trnsysHandle != IntPtr.Zero)
            {
                _trnsys = (TrnsysDelegate)Marshal.GetDelegateForFunctionPointer(
                    trnsysHandle,
                    typeof(TrnsysDelegate));
            }
            else
            {
                Console.WriteLine("Fatal error. Routine named Trnsys not found in TRNDll");
            }
        }

        ~TrnDllWrapper()
        {
            //Free ressources.
            //Probably should use SafeHandle or some similar class
            //But this will do for now.
            NativeLibrary.FreeLibrary(_dllhandle);
        }

        // Handles and delegates


        IntPtr _dllhandle = IntPtr.Zero;
        TrnsysDelegate _trnsys = null;

        public class GetTrnsysCallOutputs
        {
            public double[] parOut { get; set; }
            public double[] plotOut { get; set; }
        }
    }
}