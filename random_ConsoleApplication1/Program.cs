using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace random_ConsoleApplication1
{
    class Program
    {
        private static Random random = new Random();
        static void Main(string[] args)
        {
            while (true)
            {
                Console.ReadLine();
                Console.WriteLine(random.Next(516400146, 630304598));
                
            }
        }
    }
}
