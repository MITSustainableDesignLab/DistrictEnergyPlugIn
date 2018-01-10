namespace TrnsysUmiPlatform.Types
{
    public class Type25C : TrnsysType
    {
        /// <summary>
        ///     The printer component is used to output (or print) selected system variables at specified (even) intervals of time.
        /// </summary>
        /// <param name="filename">Output file for integrated results</param>
        /// <param name="nInputs">Number of inputs to be integrated and printed</param>
        /// <param name="inputString">Inputs to be integrated and printed</param>
        /// <param name="heanding1String">Labels</param>
        public Type25C(string filename, int nInputs, string inputString, string heanding1String) :
            base(filename, "25", 10, nInputs, filename + ".txt")
        {
            ParameterString = "1\t\t! 1 Printing interval\r\n0\t\t! 2 Start time\r\n8760\t\t! 3 Stop time\r\n" +
                              ExternalFileNumber +
                              "\t\t! 4 Logical unit\r\n1\t\t! 5 Units printing mode\r\n0\t\t! 6 Relative or absolute start time\r\n-1\t\t! 7 Overwrite or Append\r\n-1\t\t! 8 Print header\r\n0\t\t! 9 Delimiter\r\n1\t\t! 10 Print labels\r\n";
            InputsString = inputString;
            Heading1String = heanding1String;
            var hstring = "";
            for (var i = 0; i < nInputs; i++)
            {
                var a = i + 1;
                hstring += "unit" + a + " ";
            }
            Heading2String = hstring + "\r\n";
        }
    }
}