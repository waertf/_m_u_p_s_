using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConsoleApplication1_test3
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            string Triggered_Location_Request_Distance = "<Triggered-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr><periodic-trigger><trg-distance>100</trg-distance></periodic-trigger></Triggered-Location-Request>";
            XDocument xml = XDocument.Parse(Triggered_Location_Request_Distance);
            string result = xml.ToString();
            Console.WriteLine(result);
            Console.ReadLine();
             * */
            Console.WriteLine(
@"
test
    test
        hahaha
");
            Console.Write("chose:");
            byte[] array = new byte[4];
            array[0] = 0x00; // Lowest
            array[1] = 0x01;
            array[2] = 0;
            array[3] = 0; // Sign bit
            //
            // Use BitConverter to convert the bytes to an int and a uint.
            // ... The int and uint can have different values if the sign bit differs.
            //
            int result1 = BitConverter.ToInt32(array, 0); // Start at first index
            uint result2 = BitConverter.ToUInt32(array, 0); // First index
            Console.WriteLine(result1);
            Console.WriteLine(result2);
            Console.ReadLine();
           
        }

    }
}
