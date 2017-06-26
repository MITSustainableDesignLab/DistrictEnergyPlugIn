namespace TrnsysUmiPlatform.Types
{
    public class Type15_3 : TrnsysType
    {
        /// <summary>
        ///     This component serves the purpose of reading data at regular time intervals from an external weather data file.
        /// </summary>
        /// <param name="weatherFile">Which file contains the Energy+ Weather Data</param>
        public Type15_3(string weatherFile) : base("Type15-3", "15", 9, 0, weatherFile)
        {
            ParameterString = "3\t\t! 1 File Type\r\n" +
                              ExternalFileNumber + "\t\t! 2 Logical unit\r\n" +
                              "3\t\t! 3 Tilted Surface Radiation Mode\r\n" +
                              "0.2\t\t! 4 Ground reflectance -no snow\r\n" +
                              "0.7\t\t! 5 Ground reflectance -snow cover\r\n" +
                              "1\t\t! 6 Number of surfaces\r\n" +
                              "1\t\t! 7 Tracking mode\r\n" +
                              "0.0\t\t! 8 Slope of surface\r\n" +
                              "0\t\t! 9 Azimut of surface\r\n";

            ProformaPath =
                ".\\Weather Data Reading and Processing\\Standard Format\\Energy+ Weather Files (EPW)\\Type15-3.tmf";
        }
    }
}