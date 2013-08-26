using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConsoleApplication1_kml_test
{
    class Program
    {
        static void Main(string[] args)
        {
            var xDoc = XDocument.Load("test01.kml");
            XNamespace ns = "http://earth.google.com/kml/2.2";
            Console.WriteLine(XmlGetTagValue(xDoc,ns+"coordinates"));
            Console.ReadLine();
            
        }
        static string XmlGetTagValue(XDocument xml_data, XName tag_name)
        {
            string result = string.Empty;
            try
            {
                result = (string)(from el in xml_data.Descendants(tag_name) select el).First();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                result = "error";
            }

            return result;

        }
    }
}
