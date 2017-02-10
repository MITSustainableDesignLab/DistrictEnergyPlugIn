using System;
using System.IO;

namespace DistrictEnergy
{
    public class ModelParameters
    {
        // Create a new ModelParameters object
        // There's only one model parameter right now
        // (transportation tons of CO2 per capita)
        public ModelParameters(double transportCarbonTonsPerCapita)
        {
            TransportCarbonTonsPerCapita = transportCarbonTonsPerCapita;
        }

        // This defines a ModelParameters property
        // The { get; } syntax means that its value can be set exactly
        // one time and then never changed
        public double TransportCarbonTonsPerCapita { get; }

        public static ModelParameters LoadFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length != 1)
            {
                throw new Exception("The model parameters file had the wrong number of lines");
            }
            var lineContents = lines[0].Split(',');
            if (lineContents.Length != 3)
            {
                throw new Exception("The model parameters file was incorrectly formatted");
            }

            // For now, we'll assume the first two values are correct, but more error checking
            // would be a good idea here
            var perCapitaCost = Double.Parse(lineContents[2].Trim());
            return new ModelParameters(perCapitaCost);
        }
    }
}
