using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_kml_generator
{
    class Program
    {
        static float first = 125.000000F;
        static float second = 30.000000F;
        static float thrid = 0.000000F;
         const float increate = -0.000100F;
        static int loop = 60;
        static void Main(string[] args)
        {
            for (int i = 0; i < loop; i++)
            {
                Console.WriteLine(first.ToString("0.000000") + "," + second.ToString("0.000000") + "," + thrid.ToString("0.000000"));
                first += increate;
                second += increate;
                
            }
        }
    }
}
