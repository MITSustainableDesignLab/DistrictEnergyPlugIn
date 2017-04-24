using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrnsysUmiPlatform
{
    public abstract class TrnSysType
    {
        /// <summary>
        /// This is the base class for a TRNSYS type.  It defines all the things that are common to types
        /// </summary>
        /// <param name="unit_name">The TRNSYS type unit name</param>
        /// <param name="type_number">The Type number</param>
        /// <param name="nParameters">Number of parameters the type has</param>
        /// <param name="nInputs">Number of inputs the type has</param>
        /// <param name="external_file">The external file associated to the type. The external file unit number is set auomatically.</param>
        public TrnSysType(string unit_name, string type_number, int nParameters, int nInputs, string external_file)
        {
            Unit_name = unit_name;
            Type_number = type_number;
            Unit_number = ++BaseUnit_number;
            NParameters = nParameters;
            NInputs = nInputs;
            External_file = external_file;
            External_file_number = ++Base_External_file_number;
            // The rest is set separetely
            Parameter_string = "";
            Inputs_string = "";
            Derivatives_string = "";
            Labels_string = "";
            Heading1_string = "";
            Heading2_string = "";
            Initial_inputs = new double[] { };
        }

        public string Parameter_string { get; set; }
        public string Inputs_string { get; set; }
        public string Derivatives_string { get; set; }
        public string Labels_string { get; set; }
        public string Heading1_string { get; set; }
        public string Heading2_string { get; set; }
        public double[] Initial_inputs { get; set; }
        public string Unit_name { get; set; }
        public string Type_number { get; set; }
        private static int BaseUnit_number = 1;
        public int Unit_number;
        public int NParameters { get; set; }
        public int NInputs { get; set; }
        public int NDerivatives { get; set; }
        public string External_file { get; set; }
        private static int Base_External_file_number = 29;
        public int External_file_number;
        public string ProformaPath { get; set; }
        public double[] Position { get; set; }

        public string WriteParameters()
        {
            return Parameter_string;
        }

        public string WriteInputs()
        {
            return Inputs_string;
        }

        public string WriteDerivatives()
        {
            return Derivatives_string;
        }

        /// <summary>
        /// Method that generates the text file. It collects the content of a type and depending on it's kind, writes to a string the necessary blocks of test.
        /// </summary>
        /// <returns>type_string: the string to be written to the dck file.</returns>
        public string WriteType()
        {
            string type_string = "* Model \"" + Unit_name + "\" (Type " + Type_number.ToString() + ")\r\n*\r\n\r\n";
            type_string += "UNIT " + Unit_number.ToString() + " TYPE " + Type_number.ToString() + "\t " + Unit_name.ToString() + "\r\n";
            type_string += "*$UNIT_NAME " + Unit_name + "\r\n";
            type_string += "*$MODEL " + ProformaPath + "\r\n";
            type_string += "*$POSITION " + String.Join(" ", Position) + "\r\n";
            type_string += "*$LAYER MAIN #\r\n";
            if (NParameters > 0)
            {
                type_string += "PARAMETERS " + NParameters.ToString() + "\r\n";
                type_string += WriteParameters();
            }

            if (NInputs > 0)
            {
                type_string += "INPUTS " + NInputs.ToString() + "\r\n";
                type_string += WriteInputs();
                type_string += "*** INITIAL INPUT VALUES\r\n";
                string input_str = String.Join(" ", Initial_inputs);
                type_string += input_str;
            }

            if (NDerivatives > 0)
            {
                type_string += "\r\nDERIVATIVES " + NDerivatives.ToString() + "\r\n";
                type_string += WriteDerivatives();
                type_string += Heading1_string;
                type_string += Heading2_string;
                type_string += Labels_string + "\r\n";
            }

            if (External_file != "")
            {
                type_string += "\r\n*** External files\r\nASSIGN \"" + External_file + "\" " + External_file_number + "\r\n";
            }

            type_string += "\r\n*------------------------------------------------------------------------------\r\n";

            return type_string;
        }

    }
    public class Type15_3 : TrnSysType
    {
        /// <summary>
        /// This component serves the purpose of reading data at regular time intervals from an external weather data file.
        /// </summary>
        /// <param name="weather_file">Which file contains the Energy+ Weather Data</param>
        public Type15_3(string weather_file) : base("Type15-3", "15", 9, 0, weather_file)
        {
            this.Parameter_string = "3\t\t! 1 File Type\r\n" +
                                    this.External_file_number + "\t\t! 2 Logical unit\r\n" +
                                    "3\t\t! 3 Tilted Surface Radiation Mode\r\n" +
                                    "0.2\t\t! 4 Ground reflectance -no snow\r\n" +
                                    "0.7\t\t! 5 Ground reflectance -snow cover\r\n" +
                                    "1\t\t! 6 Number of surfaces\r\n" +
                                    "1\t\t! 7 Tracking mode\r\n" +
                                    "0.0\t\t! 8 Slope of surface\r\n" +
                                    "0\t\t! 9 Azimut of surface\r\n";

            ProformaPath = ".\\Weather Data Reading and Processing\\Standard Format\\Energy+ Weather Files (EPW)\\Type15-3.tmf";
        }
    }
    public class Type25c : TrnSysType
    {
        /// <summary>
        /// The printer component is used to output (or print) selected system variables at specified (even) intervals of time.
        /// </summary>
        /// <param name="filename">Output file for integrated results</param>
        /// <param name="nInputs">Number of inputs to be integrated and printed</param>
        /// <param name="input_string">Inputs to be integrated and printed</param>
        /// <param name="heanding1_string">Labels</param>
        public Type25c(string filename, int nInputs, string input_string, string heanding1_string) :
            base(filename, "25", 10, nInputs, filename + ".txt")
        {
            this.Parameter_string = "1\t\t! 1 Printing interval\r\n0\t\t! 2 Start time\r\n8760\t\t! 3 Stop time\r\n" + this.External_file_number + "\t\t! 4 Logical unit\r\n1\t\t! 5 Units printing mode\r\n0\t\t! 6 Relative or absolute start time\r\n-1\t\t! 7 Overwrite or Append\r\n-1\t\t! 8 Print header\r\n0\t\t! 9 Delimiter\r\n1\t\t! 10 Print labels\r\n";
            this.Inputs_string = input_string;
            this.Heading1_string = heanding1_string;
            var hstring = "";
            for (int i = 0; i < nInputs; i++)
            {
                int a = i + 1;
                hstring += "unit" + a.ToString() + " ";
            }
            this.Heading2_string = hstring + "\r\n";
        }
    }
    public class Type46 : TrnSysType
    {
        /// <summary>
        /// The component is used to print the integrated values of the connected inputs to a user-specified data file.
        /// </summary>
        /// <param name="filename">Output file for integrated results</param>
        /// <param name="nInputs">Number of inputs to be integrated and printed</param>
        /// <param name="input_string">Inputs to be integrated and printed</param>
        /// <param name="heanding1_string">Labels</param>
        /// <param name="labels_string">Labels</param>
        public Type46(string filename, int nInputs, string input_string, string heanding1_string, string labels_string) :
            base(filename, "46", 5, nInputs, filename + ".out")
        {
            this.Parameter_string = this.External_file_number.ToString() + "\t\t! 1 Logical unit\r\n-1\t\t! 2 Logical unit for monthly summaries\r\n0\t\t! 3 Relative or absolute start time\r\nSTEP\t\t! 4 Printing & integrating interval\r\n0\t\t! 5 Number of inputs to avoid integration\r\n";
            this.Inputs_string = input_string;
            this.Heading1_string = heanding1_string;
            this.Labels_string = labels_string;
        }
    }
    public class Type31 : TrnSysType
    {
        int[,] _inputs;
        double _insideDiameter;
        double _pipeLength;
        double _lossCoefficient;
        double _fluidDensity;
        double _fluidSpecificHeat;
        double _initialFluidTemp;
        int _edgeId;
        /// <summary>
        /// This component models the thermal behavior of fluid flow in a pipe or duct using variable size segments of fluid.
        /// Entering fluid shifts the position of existing segments. The mass of the new segment is equal to the flow rate times the simulation timestep.
        /// The new segment's temperature is that of the incoming fluid. The outlet of this pipe is a collection of the elements that are pushed out by the inlet flow.
        /// This plug-flow model does not consider mixing or conduction between adjacent elements. A maximum of 25 segments is allowed in the pipe.
        /// When the maximum is reached, the two adjacent segments with the closest temperatures are combined to make one segment.
        /// </summary>
        /// <param name="inputs">0 Inlet Temperature; 1 Inlet Flow rate; 2 Environment temperature</param>
        /// <param name="insideDiameter">The inside diameter of the pipe.  If a square duct is to be modeled, this parameter should be set to an equivalent diameter which gives the same surface area.</param>
        /// <param name="pipeLength">The length of the pipe to be considered.</param>
        /// <param name="lossCoefficient">The heat transfer coefficient for thermal losses to the environment based on the inside pipe surface area.</param>
        /// <param name="fluidDensity">The density of the fluid in the pipe/duct.</param>
        /// <param name="fluidSpecificHeat">The specific heat of the fluid in the pipe/duct.</param>
        /// <param name="initialFluidTemp">The temperature of the fluid in the pipe at the beginning of the simulation.</param>
        public Type31(double insideDiameter, double pipeLength, double lossCoefficient, double fluidDensity, double fluidSpecificHeat, double initialFluidTemp) : base("Type31", "31", 6, 3, "")
        {
            _inputs = new int[3, 2];
            _insideDiameter = insideDiameter;
            _pipeLength = pipeLength;
            _lossCoefficient = lossCoefficient;
            _fluidDensity = fluidDensity;
            _fluidSpecificHeat = fluidSpecificHeat;
            _initialFluidTemp = initialFluidTemp;

            Parameter_string = insideDiameter.ToString() + "\t\t! 1 Inside diameter\r\n" +
                                    pipeLength.ToString() + "\t\t! 2 Pipe length\r\n" +
                                    lossCoefficient.ToString() + "\t\t! 3 Loss coefficient\r\n" +
                                    fluidDensity.ToString() + "\t\t! 4 Fluid density\r\n" +
                                    fluidSpecificHeat.ToString() + "\t\t! 5 Fluid specific heat\r\n" +
                                    initialFluidTemp.ToString() + "\t\t! 6 Initial fluid temperature\r\n";
            if (_inputs != null)
            {
                Inputs_string = SetInputString(_inputs);
            }

            ProformaPath = ".\\Hydronics\\Pipe_Duct\\Type31.tmf";

            Initial_inputs = new double[] { initialFluidTemp, 100, 10 };

        }
        public void SetInputs(int[] fromUnit, int[] fromOutput)
        {
            _inputs.SetValue(fromUnit[0], 0, 0);
            _inputs.SetValue(fromUnit[1], 1, 0);
            _inputs.SetValue(fromUnit[2], 2, 0);
            _inputs.SetValue(fromOutput[0], 0, 1);
            _inputs.SetValue(fromOutput[1], 1, 1);
            _inputs.SetValue(fromOutput[2], 2, 1);

            Inputs_string = SetInputString(_inputs);
        }

        private string SetInputString(int[,] _inputs)
        {
            Inputs_string = _inputs[0, 0].ToString() + "," + _inputs[0, 1].ToString() + "\t\t! Inlet temperature\r\n" +
                                _inputs[1, 0].ToString() + "," + _inputs[1, 1].ToString() + "\t\t! Inlet flow rate\r\n" +
                                _inputs[2, 0].ToString() + "," + _inputs[2, 1].ToString() + "\t\t! Environment temperature\r\n";
            return Inputs_string;
        }

        public int EdgeId
        {
            get
            {
                return _edgeId;
            }
            set
            {
                _edgeId = value;
            }
        }
        internal double PipeLength
        {
            get
            {
                return _pipeLength;
            }
            set
            {
                DoNotAllowNegativeValues(value);
                _pipeLength = value;
            }
        }
        internal double InsideDiameter
        {
            get
            {
                return _insideDiameter;
            }
            set
            {
                DoNotAllowNegativeValues(value);
                _insideDiameter = value;
            }
        }
        internal double LossCoefficient
        {
            get
            {
                return _lossCoefficient;
            }
            set
            {
                DoNotAllowNegativeValues(value);
                _lossCoefficient = value;
            }
        }
        internal double FluidDensity
        {
            get
            {
                return _fluidDensity;
            }
            set
            {
                DoNotAllowNegativeValues(value);
                _fluidDensity = value;
            }
        }
        internal double FluidSpecificHeat
        {
            get
            {
                return _fluidSpecificHeat;
            }
            set
            {
                DoNotAllowNegativeValues(value);
                _fluidSpecificHeat = value;
            }
        }
        internal double InitialFluidTemp
        {
            get
            {
                return _initialFluidTemp;
            }
            set
            {
                DoNotAllowNegativeValues(value);
                _initialFluidTemp = value;
            }
        }
        private void DoNotAllowNegativeValues(double value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value");
        }
    }


    public class Type11 : TrnSysType
    {
        int[,] _inputs;
        int _nodeId;
        /// <summary>
        /// This instance of the Type11 model uses mode 2 to model a flow diverter in which a single
        /// inlet liquid stream is split according to a user specified valve setting into two liquid outlet streams.
        /// </summary>
        public Type11() : base("Type 11", "11", 1, 3, "")
        {
            OutUsed = !OutUsed;
            _inputs = new int[3, 2];

            Parameter_string = "2\t\t! 1 Controlled flow diverter mode\r\n";

            if (_inputs != null)
            {
                Inputs_string = SetInputString(_inputs);
            }

            ProformaPath = ".\\Hydronics\\Flow Diverter\\Other Fluids\\Type11f.tmf";

            Initial_inputs = new double[] { 20, 100, 10 };
        }

        public void SetInputs(int[] fromUnit, int[] fromOutput)
        {
            _inputs.SetValue(fromUnit[0], 0, 0);
            _inputs.SetValue(fromUnit[1], 1, 0);
            _inputs.SetValue(0, 2, 0);
            _inputs.SetValue(fromOutput[0], 0, 1);
            _inputs.SetValue(fromOutput[1], 1, 1);
            _inputs.SetValue(0, 2, 1);

            Inputs_string = SetInputString(_inputs);
        }

        private string SetInputString(int[,] _inputs)
        {
            Inputs_string = _inputs[0, 0].ToString() + "," + _inputs[0, 1].ToString() + "\t\t! Inlet temperature\r\n" +
                                _inputs[1, 0].ToString() + "," + _inputs[1, 1].ToString() + "\t\t! Inlet flow rate\r\n" +
                                _inputs[2, 0].ToString() + "," + _inputs[2, 1].ToString() + "\t\t! Control signal\r\n";
            return Inputs_string;
        }

        public int NodeId
        {
            get
            {
                return _nodeId;
            }
            set
            {
                _nodeId = value;
            }
        }

        public bool OutUsed;


    }
    public class Type659 : TrnSysType
    {
        /// <summary>
        /// Type659 models an external, proportionally controlled fluid heater. External proportional control (an
        /// input signal between 0 and 1) is in effect as long as a fluid set point temperature is not exceeded.
        /// </summary>
        /// <param name="rated_capacity">The rated capacity (the maximum possible energy transfer to the fluid stream) of the boiler</param>
        public Type659(double rated_capacity) : base("Boiler", "31", 2, 7, "")
        {
            this.Parameter_string = rated_capacity.ToString() + "\t\t! 1 Rated Capacity" + "CpFluid\t\t! 2 Specific Heat of Fluid\r\n";
            this.Inputs_string = "";
            this.Initial_inputs = new double[] { 20.0, 10000.0, 1, 50.0, 0.0, 1.0, 20.0 };
        }
    }

    public class Type741 : TrnSysType

    {
        /// <summary>
        /// Type741 models a variable speed pump that is able to produce any mass flowrate between zero and its
        /// rated flowrate.
        /// </summary>
        /// <param name="rated_flowrate">The maximum (rated) flowrate of fluid through the pump.</param>
        /// <param name="fluid_specific_heat">The specific heat of the fluid flowing through the device.</param>
        /// <param name="fluid_density">The density of the fluid flowing through the device.</param>
        /// <param name="motor_heatloss_fraction">The fraction of the motor heat loss transferred to the fluid stream.</param>
        public Type741(double rated_flowrate, double fluid_specific_heat, double fluid_density, double motor_heatloss_fraction) : base("Type741", "741", 4, 6, "")
        {
            this.Parameter_string = rated_flowrate.ToString() + "\t\t! 1 Rated Flowrate\r\n" +
                                    fluid_specific_heat.ToString() + "\t\t! 2 Fluid Specific Heat\r\n" +
                                    fluid_density + "\t\t! 3 Fluid Density\r\n" +
                                    motor_heatloss_fraction.ToString() + "\t\t! 4 Motor Heat Loss Fraction\r\n";
            this.Inputs_string = "0,0\r\n0,0\r\n0,0\r\n0,0\r\n0,0\r\n0,0\r\n";
            this.Initial_inputs = new double[] { 20.0, 0.0, 1.0, 0.6, 0.9, 10.0 };

            ProformaPath = ".\\Hydronics Library (TESS)\\Pumps\\Sets the Mass Flow Rate\\Variable-Speed\\Power from Efficiency and Pressure Drop\\Type741.tmf";
        }
    }

    public class Type682 : TrnSysType
    {
        int[,] _inputs;
        Guid _bldId;
        string _bldName;
        double _fluidSpecificHeat;
        /// <summary>
        /// This model simply imposes a user-specified load (cooling = positive load, heating = negative load) 
        /// on a flow stream and calculates the resultant outlet fluid conditions
        /// </summary>
        /// <param name="fluidSpecificHeat">The specific heat of the fluid stream[kJ/kg-K]</param>
        public Type682(double fluidSpecificHeat) : base("Type682", "682", 1, 6, "")
        {
            _fluidSpecificHeat = fluidSpecificHeat;

            Parameter_string = fluidSpecificHeat.ToString() + "\t\t! 1 Fluid specific heat\r\n";
            if (_inputs != null)
                Inputs_string = SetInputString(_inputs);

            ProformaPath = ".\\Loads and Structures (TESS)\\Flowstream Loads\\Other Fluids\\Type682.tmf";

            Initial_inputs = new double[] { 10.0, 100.0, 0.0, -999, 999 };
        }

        public void SetInputs(int[] fromUnit, int[] fromOutput)
        {
            _inputs.SetValue(fromUnit[0], 0, 0);
            _inputs.SetValue(fromUnit[1], 1, 0);
            _inputs.SetValue(fromUnit[2], 2, 0);
            _inputs.SetValue(fromUnit[3], 3, 0);
            _inputs.SetValue(fromUnit[4], 4, 0);
            _inputs.SetValue(fromOutput[0], 0, 1);
            _inputs.SetValue(fromOutput[1], 1, 1);
            _inputs.SetValue(fromOutput[2], 2, 1);
            _inputs.SetValue(fromOutput[3], 3, 1);
            _inputs.SetValue(fromOutput[4], 4, 1);

            Inputs_string = SetInputString(_inputs);
        }

        private string SetInputString(int[,] _inputs)
        {
            Inputs_string = _inputs[0, 0].ToString() + "," + _inputs[0, 1].ToString() + "\t\t! Inlet Temperature\r\n" +
                            _inputs[1, 0].ToString() + "," + _inputs[1, 1].ToString() + "\t\t! Inlet flow rate\r\n" +
                            _inputs[2, 0].ToString() + "," + _inputs[2, 1].ToString() + "\t\t! Load\r\n" +
                            _inputs[3, 0].ToString() + "," + _inputs[3, 1].ToString() + "\t\t! Minimum Heating Temperature\r\n" +
                            _inputs[4, 0].ToString() + "," + _inputs[4, 1].ToString() + "\t\t! Maximum Cooling Temperature\r\n";
            return Inputs_string;
        }

        public Guid BuildingId
        {
            get
            {
                return _bldId;
            }
            set
            {
                _bldId = value;
            }
        }
        public string BuildingName
        {
            get
            {
                return _bldName;
            }
            set
            {
                _bldName = value;
            }
        }


    }



    //#########################################################################################################
    //# The following make up other components of a deck file that are not Types.
    //#########################################################################################################
    public class Intro
    {
        public Intro(string creator, string creation_date, string modified_date, string description)
        {
            Creator = creator;
            Creation_Date = creation_date;
            Modified_Date = modified_date;
            Description = description;
        }

        public string Creation_Date { get; set; }
        public string Creator { get; set; }
        public string Description { get; set; }
        public string Modified_Date { get; set; }

        public string WriteIntro()
        {
            return "VERSION 17\r\n*******************************************************************************\r\n*** TRNSYS input file (deck) generated by Trnweb\r\n*** Creator: " + Creator + "\r\n*** Created: " + Creation_Date + "\r\n*** Modified: " + Modified_Date + "\r\n*** Description: " + Description + "\r\n***\r\n*** If you edit this file, use the File/Import TRNSYS Input File function in\r\n*** TrnsysStudio to update the project.\r\n***\r\n*** If you have problems, questions or suggestions please contact your local\r\n*** TRNSYS distributor or mailto:software@cstb.fr\r\n***\r\n*******************************************************************************";

        }
    }
    public class ControlCards
    {
        public ControlCards(int start, int stop, double data_step)
        {
            Start = start;
            Stop = stop;
            Data_step = data_step;
        }

        public int Start { get; set; }
        public int Stop { get; set; }
        public double Data_step { get; set; }

        public string WriteControlCards()
        {
            // The value to be returned
            string value;
            {
                value = "*******************************************************************************\r\n*** Control cards\r\n*******************************************************************************\r\n* START, STOP and STEP\r\nCONSTANTS 3\r\nSTART=" + Start.ToString() + "\r\nSTOP=" + Stop.ToString() + "\r\nSTEP=" + Data_step.ToString() + "\r\nSIMULATION \t START\t STOP\t STEP\t! Start time\tEnd time\tTime step\r\nTOLERANCES 0.001 0.001\t\t\t! Integration\t Convergence\r\nLIMITS 50 1000 30\t\t\t\t! Max iterations\tMax warnings\tTrace limit\r\nDFQ 1\t\t\t\t\t! TRNSYS numerical integration solver method\r\nWIDTH 80\t\t\t\t! TRNSYS output file width, number of characters\r\nLIST \t\t\t\t\t! NOLIST statement\r\n\t\t\t\t\t\t! MAP statement\r\nSOLVER 0 1 1\t\t\t! Solver statement\tMinimum relaxation factor\tMaximum relaxation factor\r\nNAN_CHECK 0\t\t\t\t! Nan DEBUG statement\r\nOVERWRITE_CHECK 0\t\t! Overwrite DEBUG statement\r\nTIME_REPORT 0\t\t\t! disable time report\r\nEQSOLVER 0\t\t\t\t! EQUATION SOLVER statement\r\n";
            }
            return value;
        }
    }
}

