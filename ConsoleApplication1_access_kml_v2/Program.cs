using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConsoleApplication1_access_kml_v2
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string get = string.Empty;
                XDocument xdoc = XDocument.Load("test2.kml");
                XNamespace KmlNamespace = "http://earth.google.com/kml/2.2";
                var result = (from el in xdoc.Descendants(KmlNamespace+"coordinates") select el);
                foreach (string cc in result)
                {
                    get += cc + Environment.NewLine;
                }
                Console.WriteLine(get);
                Console.WriteLine(XmlGetTagValue(xdoc,(KmlNamespace+"name").ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
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
                Console.WriteLine("XmlGetTagValue:" + tag_name + ":" + e.Message);
                //log.Error("XmlGetTagValue:" + tag_name + ":" + e.Message);
                result = "";
            }

            return result;

        }
    }
}
