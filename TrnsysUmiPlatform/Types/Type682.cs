using System;

namespace TrnsysUmiPlatform.Types
{
    public class Type682 : TrnsysType
    {
        private int[,] _inputs;
        private double _fluidSpecificHeat;

        /// <summary>
        ///     This model simply imposes a user-specified load (cooling = positive load, heating = negative load)
        ///     on a flow stream and calculates the resultant outlet fluid conditions
        /// </summary>
        /// <param name="fluidSpecificHeat">The specific heat of the fluid stream[kJ/kg-K]</param>
        public Type682(double fluidSpecificHeat) : base("Type682", "682", 1, 6, "")
        {
            _fluidSpecificHeat = fluidSpecificHeat;

            ParameterString = fluidSpecificHeat + "\t\t! 1 Fluid specific heat\r\n";
            if (_inputs != null)
                InputsString = SetInputString(_inputs);

            ProformaPath = ".\\Loads and Structures (TESS)\\Flowstream Loads\\Other Fluids\\Type682.tmf";

            InitialInputs = new[] {10.0, 100.0, 0.0, -999, 999};
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

            InputsString = SetInputString(_inputs);
        }

        private string SetInputString(int[,] inputs)
        {
            InputsString = inputs[0, 0] + "," + inputs[0, 1] + "\t\t! Inlet Temperature\r\n" +
                           inputs[1, 0] + "," + inputs[1, 1] + "\t\t! Inlet flow rate\r\n" +
                           inputs[2, 0] + "," + inputs[2, 1] + "\t\t! Load\r\n" +
                           inputs[3, 0] + "," + inputs[3, 1] + "\t\t! Minimum Heating Temperature\r\n" +
                           inputs[4, 0] + "," + inputs[4, 1] + "\t\t! Maximum Cooling Temperature\r\n";
            return InputsString;
        }

        public Guid BuildingId { get; set; }

        public string BuildingName { get; set; }
    }
}