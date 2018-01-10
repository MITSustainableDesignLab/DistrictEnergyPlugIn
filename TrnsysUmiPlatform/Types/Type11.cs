namespace TrnsysUmiPlatform.Types
{
    public class Type11 : TrnsysType
    {
        private readonly int[,] _inputs;

        /// <summary>
        ///     This instance of the Type11 model uses mode 2 to model a flow diverter in which a single
        ///     inlet liquid stream is split according to a user specified valve setting into two liquid outlet streams.
        /// </summary>
        public Type11() : base("Type 11", "11", 1, 3, "")
        {
            OutUsed = !OutUsed;
            _inputs = new int[3, 2];

            ParameterString = "2\t\t! 1 Controlled flow diverter mode\r\n";

            if (_inputs != null)
                InputsString = SetInputString(_inputs);

            ProformaPath = ".\\Hydronics\\Flow Diverter\\Other Fluids\\Type11f.tmf";

            InitialInputs = new double[] {20, 100, 10};
        }

        public void SetInputs(int[] fromUnit, int[] fromOutput)
        {
            _inputs.SetValue(fromUnit[0], 0, 0);
            _inputs.SetValue(fromUnit[1], 1, 0);
            _inputs.SetValue(0, 2, 0);
            _inputs.SetValue(fromOutput[0], 0, 1);
            _inputs.SetValue(fromOutput[1], 1, 1);
            _inputs.SetValue(0, 2, 1);

            InputsString = SetInputString(_inputs);
        }

        private string SetInputString(int[,] inputs)
        {
            InputsString = inputs[0, 0] + "," + inputs[0, 1] + "\t\t! Inlet temperature\r\n" +
                           inputs[1, 0] + "," + inputs[1, 1] + "\t\t! Inlet flow rate\r\n" +
                           inputs[2, 0] + "," + inputs[2, 1] + "\t\t! Control signal\r\n";
            return InputsString;
        }

        public int NodeId { get; set; }

        public readonly bool OutUsed;
    }
}