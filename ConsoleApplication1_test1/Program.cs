using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConsoleApplication1_test1
{
    class Program
    {
        static void Main(string[] args)
        {
            string test = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">1004</suaddr><info-data><satellites-num>3</satellites-num><info-time>20030630073000</info-time><server-time>20030630073000</server-time><shape><circle-2d><lat>12.345345</lat><long>24.668866</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
            XDocument xml_data = XDocument.Parse(test);
            
            string shape_type = (string)(from e in xml_data.Descendants("shape") select e.Elements().First().Name.LocalName).First();
            Console.WriteLine("shape_type :[{0}]", shape_type);
            switch (shape_type)
            {
                case "point-2d":
                    {
                        string lat_value = XmlGetTagValue(xml_data, "lat");
                        string long_value = XmlGetTagValue(xml_data, "long");
                    }
                    break;
                case "point-3d":
                    {
                        string lat_value = XmlGetTagValue(xml_data, "lat");
                        string long_value = XmlGetTagValue(xml_data, "long"); ;
                        string altitude_value = XmlGetTagValue(xml_data, "altitude"); 
                    }
                    break;
                case "circle-2d":
                    {
                        string lat_value = XmlGetTagValue(xml_data, "lat");
                        string long_value = XmlGetTagValue(xml_data, "long"); ;
                        string radius_value = XmlGetTagValue(xml_data, "radius");
                        Console.WriteLine("lat:[{0}] , long:[{1}] , radius:[{2}]", lat_value, long_value, radius_value);
                    }
                    break;
                case "circle-3d":
                    {
                        string lat_value = XmlGetTagValue(xml_data, "lat");
                        string long_value = XmlGetTagValue(xml_data, "long"); ;
                        string altitude_value = XmlGetTagValue(xml_data, "altitude");
                        string radius_value = XmlGetTagValue(xml_data, "radius");
                    }
                    break;
            }
            IEnumerable<XElement> de = from el in xml_data.Descendants("sensor") select el;
            List<string> sensor_name = (from e in de.Descendants("sensor-name") select (string)e).Cast<string>().ToList();
            List<string> sensor_value = (from e in de.Descendants("sensor-value") select (string)e).Cast<string>().ToList();
            List<string> sensor_type = (from e in de.Descendants("sensor-type") select (string)e).Cast<string>().ToList();
            int de_count = de.Count();

            IEnumerable<XName> elements = (from e1 in xml_data.DescendantNodes().OfType<XElement>() select e1).Select(x => x.Name).Distinct();
            if (elements.Contains(new XElement("info-data").Name))
                Console.WriteLine("info-data exist");
            else
                Console.WriteLine("info-data no exit");
            Console.WriteLine("#####################################");
            foreach (XName el in elements)
                Console.WriteLine(el);
            Console.WriteLine("#####################################");
            
            foreach (XElement el in de)
                Console.WriteLine(el);
            foreach (string s in sensor_name)
                Console.WriteLine(s);
            Console.WriteLine(de_count);
             
            Console.ReadLine();
        }
        static string XmlGetTagValue(XDocument xml_data, string tag_name)
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
        static IEnumerable<XName> XmlGetAllElementsXname(XDocument xml_data)
        {
            return (from e1 in xml_data.DescendantNodes().OfType<XElement>() select e1).Select(x => x.Name).Distinct();
        }
    }
}
