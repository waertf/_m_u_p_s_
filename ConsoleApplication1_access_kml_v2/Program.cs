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
    }
}
