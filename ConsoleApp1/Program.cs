using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistrictEnergy;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new List<object>();

            var f1 = new ControlCards(0, 8760, 1);
            a.Add(f1);

            Console.WriteLine("There are " + a.Count + " items");
            
            

        }
    }
}
