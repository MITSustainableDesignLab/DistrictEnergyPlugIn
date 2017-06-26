namespace TrnsysUmiPlatform.Types
{
    public class Type46 : TrnsysType
    {
        /// <summary>
        ///     The component is used to print the integrated values of the connected inputs to a user-specified data file.
        /// </summary>
        /// <param name="filename">Output file for integrated results</param>
        /// <param name="nInputs">Number of inputs to be integrated and printed</param>
        /// <param name="inputString">Inputs to be integrated and printed</param>
        /// <param name="heanding1String">Labels</param>
        /// <param name="labelsString">Labels</param>
        public Type46(string filename, int nInputs, string inputString, string heanding1String,
            string labelsString) :
            base(filename, "46", 5, nInputs, filename + ".out")
        {
            ParameterString = ExternalFileNumber +
                              "\t\t! 1 Logical unit\r\n-1\t\t! 2 Logical unit for monthly summaries\r\n0\t\t! 3 Relative or absolute start time\r\nSTEP\t\t! 4 Printing & integrating interval\r\n0\t\t! 5 Number of inputs to avoid integration\r\n";
            InputsString = inputString;
            Heading1String = heanding1String;
            LabelsString = labelsString;
        }
    }
}