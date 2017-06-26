namespace TrnsysUmiPlatform.Types
{
    public class Type741 : TrnsysType

    {
        /// <summary>
        ///     Type741 models a variable speed pump that is able to produce any mass flowrate between zero and its
        ///     rated flowrate.
        /// </summary>
        /// <param name="ratedFlowrate">The maximum (rated) flowrate of fluid through the pump.</param>
        /// <param name="fluidSpecificHeat">The specific heat of the fluid flowing through the device.</param>
        /// <param name="fluidDensity">The density of the fluid flowing through the device.</param>
        /// <param name="motorHeatlossFraction">The fraction of the motor heat loss transferred to the fluid stream.</param>
        public Type741(double ratedFlowrate, double fluidSpecificHeat, double fluidDensity,
            double motorHeatlossFraction) : base("Type741", "741", 4, 6, "")
        {
            ParameterString = ratedFlowrate + "\t\t! 1 Rated Flowrate\r\n" +
                              fluidSpecificHeat + "\t\t! 2 Fluid Specific Heat\r\n" +
                              fluidDensity + "\t\t! 3 Fluid Density\r\n" +
                              motorHeatlossFraction + "\t\t! 4 Motor Heat Loss Fraction\r\n";
            InputsString = "0,0\r\n0,0\r\n0,0\r\n0,0\r\n0,0\r\n0,0\r\n";
            InitialInputs = new[] {20.0, 0.0, 1.0, 0.6, 0.9, 10.0};

            ProformaPath =
                ".\\Hydronics Library (TESS)\\Pumps\\Sets the Mass Flow Rate\\Variable-Speed\\Power from Efficiency and Pressure Drop\\Type741.tmf";
        }
    }
}