using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.TRNSYS
{
    class TrnSysType
    {
        // This is the base class for a TRNSYS type.  It defines all the things that are comon to types."""
        public TrnSysType(string unit_name, string type_number, string unit_number, int nParameters, int nInputs, string external_file, string external_file_number)
        {
            Unit_name = unit_name;
            Type_number = type_number;
            Unit_number = unit_number;
            NParameters = nParameters;
            NInputs = nInputs;
            External_file = external_file;
            External_file_number = external_file_number;
            // The rest is set separetely
            Parameter_string = "";
            Inputs_string = "";
            Derivatives_string = "";
            Labels_string = "";
            Heading1_string = "";
            Heading2_string = "";
            Initial_inputs = 0;
        }

        public string Parameter_string { get; set; }
        public string Inputs_string { get; set; }
        public string Derivatives_string { get; set; }
        public string Labels_string { get; set; }
        public string Heading1_string { get; set; }
        public string Heading2_string { get; set; }
        public int Initial_inputs { get; set; }
        public string Unit_name { get; set; }
        public string Type_number { get; set; }
        public string Unit_number { get; set; }
        public int NParameters { get; set; }
        public int NInputs { get; set; }
        public int NDerivatives { get; set; }
        public string External_file { get; set; }
        public string External_file_number { get; set; }

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

        public string WriteType()
        {
            string type_string = "* Model \"" + Unit_name + "\" (Type " + Type_number.ToString() + ")\n*\n\n";
            type_string += "UNIT " + Unit_number.ToString() + " TYPE " + Type_number.ToString() + "\t " + Unit_name.ToString() + "\n";
            if (NParameters > 0)
            {
                type_string += "PARAMETERS " + NParameters.ToString() + "\n";
                type_string += WriteParameters();
            }

            if (NInputs > 0)
            {
                type_string += "INPUTS " + NInputs.ToString() + "\n";
                type_string += WriteInputs();
                type_string += "*** INITIAL INPUT VALUES\n";
                string input_str = String.Join(" ", Initial_inputs);
                type_string += input_str;
            }

            if (NDerivatives > 0)
            {
                type_string += "\nDERIVATIVES " + NDerivatives.ToString() + "\n";
                type_string += WriteDerivatives();
                type_string += Heading1_string;
                type_string += Heading2_string;
                type_string += Labels_string + "\n";
            }

            if (External_file != "")
            {
                type_string += "\n*** External files\nASSIGN \"" + External_file + "\" " + External_file_number + "\n";
                type_string += "\n*------------------------------------------------------------------------------\n";
            }

            return type_string;
        }

    }

    class Type15 : TrnSysType
    {
        public Type15(string weather_file) : base("Weather", "15", "15", 27, 0, weather_file, "153")
        {
            this.Parameter_string = "5\t\t! 1 File Type\n153\t\t! 2 Logical unit\n3\t\t! 3 Tilted Surface Radiation Mode\n0.2\t\t! 4 Ground reflectance - no snow\n0.7\t\t! 5 Ground reflectance - snow cover\n7\t\t! 6 Number of surfaces\n1\t\t! 7 Tracking mode-1\n90\t\t! 8 Slope of surface-1\nFront_Azimuth\t\t! 9 Azimuth of surface-1\n1\t\t! 10 Tracking mode-2\n90\t\t! 11 Slope of surface-2\nBack_Azimuth\t\t! 12 Azimuth of surface-2\n1\t\t! 13 Tracking mode-3\n90\t\t! 14 Slope of surface-3\nLeft_Azimuth\t\t! 15 Azimuth of surface-3\n1\t\t! 16 Tracking mode-4\n90\t\t! 17 Slope of surface-4\nRight_Azimuth\t\t! 18 Azimuth of surface-4\n1\t\t! 19 Tracking mode-5\nPV0_Slope\t\t! 20 Slope of surface-5\nPV0_Azimuth\t\t! 21 Azimuth of surface-5\n1\t\t! 22 Tracking mode-6\nPV1_Slope\t\t! 23 Slope of surface-6\nPV1_Azimuth\t\t! 24 Azimuth of surface-6\n1\t\t! 25 Tracking mode-7\nPV2_Slope\t\t! 26 Slope of surface-7\nPV2_Azimuth\t\t! 27 Azimuth of surface-7\n";
        }

    }
    // Printer
    class Type25 : TrnSysType
    {
        public Type25(string unit_number, string filename, string logical_unit_number, int nInputs, string input_string, string heanding1_string) :
            base(filename, "25", unit_number, 10, nInputs, filename + ".txt", logical_unit_number)
        {
            this.Parameter_string = "1\t\t! 1 Printing interval\n0\t\t! 2 Start time\n8760\t\t! 3 Stop time\n" + logical_unit_number + "\t\t! 4 Logical unit\n1\t\t! 5 Units printing mode\n0\t\t! 6 Relative or absolute start time\n-1\t\t! 7 Overwrite or Append\n-1\t\t! 8 Print header\n0\t\t! 9 Delimiter\n1\t\t! 10 Print labels\n";
            this.Inputs_string = input_string;
            this.Heading1_string = heanding1_string;
            var hstring = "";
            for (int i = 0; i < nInputs; i++)
            {
                int a = i + 1;
                hstring += "unit" + a.ToString() + " ";
            }
            this.Heading2_string = hstring + "\n";
        }
    }

    // Integrator
    class Type46 : TrnSysType
    {
        public Type46(string unit_number, string filename, string logical_unit_number, int nInputs, string input_string, string heanding1_string, string labels_string) :
            base(filename, "46", unit_number, 5, nInputs, filename + ".out", logical_unit_number)
        {
            this.Parameter_string = logical_unit_number.ToString() + "\t\t! 1 Logical unit\n-1\t\t! 2 Logical unit for monthly summaries\n0\t\t! 3 Relative or absolute start time\n-1\t\t! 4 Printing & integrating interval\n0\t\t! 5 Number of inputs to avoid integration\n";
            this.Inputs_string = input_string;
            this.Heading1_string = heanding1_string;
            this.Labels_string = labels_string;
        }
    }

    //#########################################################################################################
    //# The following make up other components of a deck file that are not Types.
    //#########################################################################################################
    class Intro
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
            return "VERSION 17\n*******************************************************************************\n*** TRNSYS input file (deck) generated by Trnweb\n*** Creator: " + Creator + "\n*** Created: " + Creation_Date + "\n*** Modified: " + Modified_Date + "\n*** Description: " + Description + "\n***\n*** If you edit this file, use the File/Import TRNSYS Input File function in\n*** TrnsysStudio to update the project.\n***\n*** If you have problems, questions or suggestions please contact your local\n*** TRNSYS distributor or mailto:software@cstb.fr\n***\n*******************************************************************************";

        }
    }


    class ControlCards
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
                value = "*******************************************************************************\n*** Control cards\n*******************************************************************************\n* START, STOP and STEP\nCONSTANTS 3\nSTART=" + Start.ToString() + "\nSTOP=" + Stop.ToString() + "\nSTEP=" + Data_step.ToString() + "\nSIMULATION \t START\t STOP\t STEP\t! Start time\tEnd time\tTime step\nTOLERANCES 0.001 0.001\t\t\t! Integration\t Convergence\nLIMITS 50 1000 30\t\t\t\t! Max iterations\tMax warnings\tTrace limit\nDFQ 1\t\t\t\t\t! TRNSYS numerical integration solver method\nWIDTH 80\t\t\t\t! TRNSYS output file width, number of characters\nLIST \t\t\t\t\t! NOLIST statement\n\t\t\t\t\t! MAP statement\nSOLVER 0 1 1\t\t\t\t! Solver statement\tMinimum relaxation factor\tMaximum relaxation factor\nNAN_CHECK 0\t\t\t\t! Nan DEBUG statement\nOVERWRITE_CHECK 0\t\t\t! Overwrite DEBUG statement\nTIME_REPORT 0\t\t\t! disable time report\nEQSOLVER 0\t\t\t\t! EQUATION SOLVER statement\n";
            }
            return value;
        }
    }

}

