namespace TrnsysUmiPlatform.Types
{
    public class Type31 : TrnsysType
    {
        private readonly int[,] _inputs;
        private double _insideDiameter;
        private double _pipeLength;
        private double _lossCoefficient;
        private double _fluidDensity;
        private double _fluidSpecificHeat;
        private double _initialFluidTemp;

        /// <summary>
        ///     This component models the thermal behavior of fluid flow in a pipe or duct using variable size segments of fluid.
        ///     Entering fluid shifts the position of existing segments. The mass of the new segment is equal to the flow rate
        ///     times the simulation timestep.
        ///     The new segment's temperature is that of the incoming fluid. The outlet of this pipe is a collection of the
        ///     elements that are pushed out by the inlet flow.
        ///     This plug-flow model does not consider mixing or conduction between adjacent elements. A maximum of 25 segments is
        ///     allowed in the pipe.
        ///     When the maximum is reached, the two adjacent segments with the closest temperatures are combined to make one
        ///     segment.
        /// </summary>
        /// <param name="insideDiameter">
        ///     The inside diameter of the pipe.  If a square duct is to be modeled, this parameter should
        ///     be set to an equivalent diameter which gives the same surface area.
        /// </param>
        /// <param name="pipeLength">The length of the pipe to be considered.</param>
        /// <param name="lossCoefficient">
        ///     The heat transfer coefficient for thermal losses to the environment based on the inside
        ///     pipe surface area.
        /// </param>
        /// <param name="fluidDensity">The density of the fluid in the pipe/duct.</param>
        /// <param name="fluidSpecificHeat">The specific heat of the fluid in the pipe/duct.</param>
        /// <param name="initialFluidTemp">The temperature of the fluid in the pipe at the beginning of the simulation.</param>
        public Type31(double insideDiameter, double pipeLength, double lossCoefficient, double fluidDensity,
            double fluidSpecificHeat, double initialFluidTemp) : base("Type31", "31", 6, 3, "")
        {
            _inputs = new int[3, 2];
            _insideDiameter = insideDiameter;
            _pipeLength = pipeLength;
            _lossCoefficient = lossCoefficient;
            _fluidDensity = fluidDensity;
            _fluidSpecificHeat = fluidSpecificHeat;
            _initialFluidTemp = initialFluidTemp;

            ParameterString = insideDiameter + "\t\t! 1 Inside diameter\r\n" +
                              pipeLength + "\t\t! 2 Pipe length\r\n" +
                              lossCoefficient + "\t\t! 3 Loss coefficient\r\n" +
                              fluidDensity + "\t\t! 4 Fluid density\r\n" +
                              fluidSpecificHeat + "\t\t! 5 Fluid specific heat\r\n" +
                              initialFluidTemp + "\t\t! 6 Initial fluid temperature\r\n";
            if (_inputs != null)
                InputsString = SetInputString(_inputs);

            ProformaPath = ".\\Hydronics\\Pipe_Duct\\Type31.tmf";

            InitialInputs = new[] {initialFluidTemp, 100, 10};
        }

        public void SetInputs(int[] fromUnit, int[] fromOutput)
        {
            _inputs.SetValue(fromUnit[0], 0, 0);
            _inputs.SetValue(fromUnit[1], 1, 0);
            _inputs.SetValue(fromUnit[2], 2, 0);
            _inputs.SetValue(fromOutput[0], 0, 1);
            _inputs.SetValue(fromOutput[1], 1, 1);
            _inputs.SetValue(fromOutput[2], 2, 1);

            InputsString = SetInputString(_inputs);
        }

        private string SetInputString(int[,] inputs)
        {
            InputsString = inputs[0, 0] + "," + inputs[0, 1] + "\t\t! Inlet temperature\r\n" +
                           inputs[1, 0] + "," + inputs[1, 1] + "\t\t! Inlet flow rate\r\n" +
                           inputs[2, 0] + "," + inputs[2, 1] + "\t\t! Environment temperature\r\n";
            return InputsString;
        }

        public int EdgeId { get; set; }
    }
}