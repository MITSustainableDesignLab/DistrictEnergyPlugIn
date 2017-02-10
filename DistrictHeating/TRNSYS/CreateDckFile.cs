using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.TRNSYS
{
    class CreateDckFile
    {
        public static string get_trnsys_exe()
        {
            return "C:\\Trnsys\\17-2-Bee\\Exe\\TRNExe.exe";
        }
        public static string get_trnsys_file_path()
        {
            return "C:\\Trnsys17web\\MyProjects\\Sumaran";
        }
        public string get_templates_file_path()
        {
            return "C:\\Trnsys17web\\MyProjects\\Sumaran\\SupportFiles\\Templates";
        }
        public CreateDckFile(TrnsysModel trnsys_model)
        {
            // "This creates a new deck file (filename) by replacing %TURN% with azimuth_string, 
            // %APPLICATION% with hvac_string, and %HOUSE_FILE% with building_file in the deck template file."

            string templatefilename = Path.Combine(get_templates_file_path(), "ParametrizedVersion_a.dck");

            // Assign Plant where: 
            // Ideal (1), Conventional(2), HeatPump(3), VRF-HP-SingleZone (4), VRF-HP-Multizone(5)
            char hvac = '1'; //Ideal
            if (trnsys_model.PlantSelection == "Plant 1")
                hvac = '1';
            else if (trnsys_model.PlantSelection == "Plant 2")
                hvac = '2';
            else if (trnsys_model.PlantSelection == "Plant 3")
                hvac = '3';
            else if (trnsys_model.PlantSelection == "Plant 4")
                hvac = '4';
            else
                Console.WriteLine("HVAC selection not valid - using Ideal");
            Console.WriteLine("template: " + templatefilename);

            string deckfilename = Path.Combine(get_trnsys_file_path(), trnsys_model.ModelName + ".dck");

            // Open template deck file and read all lines
            string[] template_deck_file = File.ReadAllLines(templatefilename);

            // Replacements to include in file
            string[,] replacements = new string[,]
            {
            { "%DATASTEP%", trnsys_model.HourlyTimestep.ToString() },
            { "%WEATHER_FILE%" , trnsys_model.WeatherFile + ".epw" },
            { "%PLOTTER%", "-1" }
            };

            // Go through the template file searching for placeholders to replace with selected values.
            string line;
            using (StreamReader file = new StreamReader(@templatefilename))
            using (StreamWriter file2 = new StreamWriter(@deckfilename))
                while ((line = file.ReadLine()) != null)
                {
                    string line2 = line;
                    file2.WriteLine(line2);
                }

            var writer = new StreamWriter(deckfilename);
            for (int i = 0; i < template_deck_file.Length; i++)
            {
                for (int j = 0; j < replacements.GetLength(0) - 1; j++)
                {
                    string line3 = template_deck_file[i];
                    line3.Replace(replacements[j, 0], replacements[j, 0]);
                    writer.WriteLine(line3);
                }
            }
        }
    }
}
