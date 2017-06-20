using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace TrnExeConsole
{
    public static class NativeLibrary
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

        public static string GetLibraryPathname(string filename)
        {

            var lib1 = filename;

            return lib1;
        }
    }

    public class TrnDllWrapper
    {
        // Delegate with function signature for the Trnsys function
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U4)]
        delegate void TrnsysDelegate(
            [OutAttribute] [InAttribute] ref int callType,
            [OutAttribute] [InAttribute] ref double [] parout,
            [OutAttribute] [InAttribute] ref int lpa,
            [OutAttribute] [InAttribute] double []  plotout,
            [OutAttribute] [InAttribute] ref int lpl,
            [OutAttribute] [InAttribute] StringBuilder labels,
            [OutAttribute] [InAttribute] int lengthLabels,
            [OutAttribute] [InAttribute] StringBuilder titles,
            [OutAttribute] [InAttribute] int lengthTitles,
            [OutAttribute] [InAttribute] StringBuilder deckn,
            [OutAttribute] [InAttribute] int lengthDeckn);

        public string Trnsys()
        {
            
            if (_trnsys != null)
            {
                //Arguments used when calling TRNSYS in TRNDll
                var lpa = 1000;
                var lpl = 1000;
                double[] _parout = new double[lpa];
                double[] _plotout = new double[lpl];
                var lengthLabels = 4000;
                var lengthTitles = 1500;
                var lengthDeckn = 300;
                StringBuilder _labels = new StringBuilder(lengthLabels);
                StringBuilder _titles = new StringBuilder(lengthTitles);
                StringBuilder _deckn = new StringBuilder(lengthDeckn);

                var startTime = 0.0;
                var stopTime = 0.0;
                var timeStep = 0.0;
                var _callType = 0;
                //Trnsys string
                _trnsys(ref _callType, ref _parout, ref lpa, _plotout, ref lpl, _labels, lengthLabels, _titles, lengthTitles, _deckn, lengthDeckn);

                // Return string
                return _callType.ToString();
            }
            return "";
        }

        public TrnDllWrapper(string filename)
        {
            var trnDll = NativeLibrary.GetLibraryPathname(@"C:\Trnsys\17-2-Bee\Exe\TRNDll.dll");
            _dllhandle = NativeLibrary.LoadLibrary(trnDll);

            if (_dllhandle == IntPtr.Zero)
            {
                return;
            }

            var trnsysHandle = NativeLibrary.GetProcAddress(_dllhandle, "TRNSYS");

            if (trnsysHandle != IntPtr.Zero)
            {
                _trnsys = (TrnsysDelegate) Marshal.GetDelegateForFunctionPointer(
                    trnsysHandle,
                    typeof(TrnsysDelegate));
            }
            else
            {
                Console.WriteLine("Fatal error. Trnsys routine not found in TRNDll");
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


        readonly IntPtr _dllhandle = IntPtr.Zero;
        readonly TrnsysDelegate _trnsys = null;
    }

    class Program
    {
        static void Main(string[] args)
        {
            


            // Load library wrapper
            TrnDllWrapper trndll = new TrnDllWrapper(@"C:\Trnsys\17-2-Bee\Exe\TRNDll.dll");

            // Initialization call to Trnsys
            int callNo = 1;
            int callType = 0;

            trndll.Trnsys();

            // Print

            Console.WriteLine("handle:");
            //Console.WriteLine(handle);
            Console.WriteLine("Done library test");
        }
    }
}
