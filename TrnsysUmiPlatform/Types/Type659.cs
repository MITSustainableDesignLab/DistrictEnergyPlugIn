namespace TrnsysUmiPlatform.Types
{
    public class Type659 : TrnsysType
    {
        /// <summary>
        ///     Type659 models an external, proportionally controlled fluid heater. External proportional control (an
        ///     input signal between 0 and 1) is in effect as long as a fluid set point temperature is not exceeded.
        /// </summary>
        /// <param name="ratedCapacity">
        ///     The rated capacity (the maximum possible energy transfer to the fluid stream) of the
        ///     boiler
        /// </param>
        public Type659(double ratedCapacity) : base("Boiler", "31", 2, 7, "")
        {
            ParameterString = ratedCapacity + "\t\t! 1 Rated Capacity" + "CpFluid\t\t! 2 Specific Heat of Fluid\r\n";
            InputsString = "";
            InitialInputs = new[] {20.0, 10000.0, 1, 50.0, 0.0, 1.0, 20.0};
        }
    }
}