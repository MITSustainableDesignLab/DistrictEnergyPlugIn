namespace TrnsysUmiPlatform
{
    public abstract class TrnsysType
    {
        /// <summary>
        ///     This is the base class for a TRNSYS type.  It defines all the things that are common to types
        /// </summary>
        /// <param name="unitName">The TRNSYS type unit name</param>
        /// <param name="typeNumber">The Type number</param>
        /// <param name="nParameters">Number of parameters the type has</param>
        /// <param name="nInputs">Number of inputs the type has</param>
        /// <param name="externalFile">
        ///     The external file associated to the type. The external file unit number is set
        ///     auomatically.
        /// </param>
        protected TrnsysType(string unitName, string typeNumber, int nParameters, int nInputs, string externalFile)
        {
            UnitName = unitName;
            TypeNumber = typeNumber;
            UnitNumber = ++_baseUnitNumber;
            NParameters = nParameters;
            NInputs = nInputs;
            ExternalFile = externalFile;
            ExternalFileNumber = ++_baseExternalFileNumber;
            // The rest is set separetely
            ParameterString = "";
            InputsString = "";
            DerivativesString = "";
            LabelsString = "";
            Heading1String = "";
            Heading2String = "";
            InitialInputs = new double[] { };
        }

        protected string ParameterString { get; set; }
        protected string InputsString { get; set; }
        private string DerivativesString { get; }
        protected string LabelsString { get; set; }
        protected string Heading1String { get; set; }
        protected string Heading2String { get; set; }
        protected double[] InitialInputs { get; set; }
        public string UnitName { get; set; }
        private string TypeNumber { get; }
        private static int _baseUnitNumber = 1;
        public readonly int UnitNumber;
        private int NParameters { get; }
        private int NInputs { get; }
        private int NDerivatives { get; set; }
        private string ExternalFile { get; }
        private static int _baseExternalFileNumber = 29;
        protected readonly int ExternalFileNumber;
        protected string ProformaPath { get; set; }
        public double[] Position { get; set; }

        private string WriteParameters()
        {
            return ParameterString;
        }

        private string WriteInputs()
        {
            return InputsString;
        }

        private string WriteDerivatives()
        {
            return DerivativesString;
        }

        /// <summary>
        ///     Method that generates the text file. It collects the content of a type and depending on it's kind, writes to a
        ///     string the necessary blocks of test.
        /// </summary>
        /// <returns>type_string: the string to be written to the dck file.</returns>
        public string WriteType()
        {
            var typeString = "* Model \"" + UnitName + "\" (Type " + TypeNumber + ")\r\n*\r\n\r\n";
            typeString += "UNIT " + UnitNumber + " TYPE " + TypeNumber + "\t " + UnitName + "\r\n";
            typeString += "*$UNIT_NAME " + UnitName + "\r\n";
            typeString += "*$MODEL " + ProformaPath + "\r\n";
            typeString += "*$POSITION " + string.Join(" ", Position) + "\r\n";
            typeString += "*$LAYER MAIN #\r\n";
            if (NParameters > 0)
            {
                typeString += "PARAMETERS " + NParameters + "\r\n";
                typeString += WriteParameters();
            }

            if (NInputs > 0)
            {
                typeString += "INPUTS " + NInputs + "\r\n";
                typeString += WriteInputs();
                typeString += "*** INITIAL INPUT VALUES\r\n";
                var inputStr = string.Join(" ", InitialInputs);
                typeString += inputStr;
            }

            if (NDerivatives > 0)
            {
                typeString += "\r\nDERIVATIVES " + NDerivatives + "\r\n";
                typeString += WriteDerivatives();
                typeString += Heading1String;
                typeString += Heading2String;
                typeString += LabelsString + "\r\n";
            }

            if (ExternalFile != "")
                typeString += "\r\n*** External files\r\nASSIGN \"" + ExternalFile + "\" " + ExternalFileNumber +
                              "\r\n";

            typeString += "\r\n*------------------------------------------------------------------------------\r\n";

            return typeString;
        }
    }


    //#########################################################################################################
    //# The following make up other components of a deck file that are not Types.
    //#########################################################################################################
    public class Intro
    {
        public Intro(string creator, string creationDate, string modifiedDate, string description)
        {
            Creator = creator;
            CreationDate = creationDate;
            ModifiedDate = modifiedDate;
            Description = description;
        }

        private string CreationDate { get; set; }
        private string Creator { get; set; }
        private string Description { get; set; }
        private string ModifiedDate { get; set; }

        public string WriteIntro()
        {
            return
                "VERSION 17\r\n*******************************************************************************\r\n*** TRNSYS input file (deck) generated by Trnweb\r\n*** Creator: " +
                Creator + "\r\n*** Created: " + CreationDate + "\r\n*** Modified: " + ModifiedDate +
                "\r\n*** Description: " + Description +
                "\r\n***\r\n*** If you edit this file, use the File/Import TRNSYS Input File function in\r\n*** TrnsysStudio to update the project.\r\n***\r\n*** If you have problems, questions or suggestions please contact your local\r\n*** TRNSYS distributor or mailto:software@cstb.fr\r\n***\r\n*******************************************************************************";
        }
    }

    public class ControlCards
    {
        public ControlCards(int start, int stop, double dataStep)
        {
            Start = start;
            Stop = stop;
            DataStep = dataStep;
        }

        private int Start { get; set; }
        private int Stop { get; set; }
        private double DataStep { get; set; }

        public string WriteControlCards()
        {
            // The value to be returned
            string value;
            {
                value =
                    "*******************************************************************************\r\n*** Control cards\r\n*******************************************************************************\r\n* START, STOP and STEP\r\nCONSTANTS 3\r\nSTART=" +
                    Start + "\r\nSTOP=" + Stop + "\r\nSTEP=" + DataStep +
                    "\r\nSIMULATION \t START\t STOP\t STEP\t! Start time\tEnd time\tTime step\r\nTOLERANCES 0.001 0.001\t\t\t! Integration\t Convergence\r\nLIMITS 50 1000 30\t\t\t\t! Max iterations\tMax warnings\tTrace limit\r\nDFQ 1\t\t\t\t\t! TRNSYS numerical integration solver method\r\nWIDTH 80\t\t\t\t! TRNSYS output file width, number of characters\r\nLIST \t\t\t\t\t! NOLIST statement\r\n\t\t\t\t\t\t! MAP statement\r\nSOLVER 0 1 1\t\t\t! Solver statement\tMinimum relaxation factor\tMaximum relaxation factor\r\nNAN_CHECK 0\t\t\t\t! Nan DEBUG statement\r\nOVERWRITE_CHECK 0\t\t! Overwrite DEBUG statement\r\nTIME_REPORT 0\t\t\t! disable time report\r\nEQSOLVER 0\t\t\t\t! EQUATION SOLVER statement\r\n";
            }
            return value;
        }
    }
}

