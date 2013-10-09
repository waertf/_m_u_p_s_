using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Configuration;
using System.Text.RegularExpressions;


namespace ConsoleApplication1_access_kml_files
{
    class Program
    {
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
                result = "error";
            }

            return result;

        }
        static void Main(string[] args)
        {
            XDocument[] xml_load = new XDocument[5];
            string[]  device= new string[5] { "90001", "90002", "90003", "90004", "90005" };
            for (int i = 0; i < xml_load.Length; i++)
            {
                xml_load[i] = XDocument.Load("test0" + (i + 1) + ".kml");
            }
            /*
            XDocument xml_load1 = XDocument.Load(@"test01.kml");
            XDocument xml_load2 = XDocument.Load(@"test02.kml");
            XDocument xml_load3 = XDocument.Load(@"test03.kml");
            XDocument xml_load4 = XDocument.Load(@"test04.kml");
            XDocument xml_load5 = XDocument.Load(@"test05.kml");
            */

            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
            sql_client.connect();
            sql_client.modify("DELETE FROM public.epq_test_loc");

            for (int j = 0; j < xml_load.Length; j++)
            {
                string receive = XmlGetTagValue(xml_load[j], "coordinates");
                string[] parts = receive.Split(new char[] { '\r', '\n', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i = i + 3)
                {
                    if ((i + 1) % 3 != 0)
                    {
                        Console.WriteLine(i + ":" + parts[i]);
                        Console.WriteLine(i + 1 + ":" + parts[i + 1]);
                        sql_client.modify("INSERT INTO public.epq_test_loc (longitude,latitude,device) VALUES (" + "\'" + parts[i] + "\'" + "," + "\'" + parts[i + 1] + "\'" +","+ "\'" + device[j]+"\'" + ")");
                    }
                }
            }
            sql_client.disconnect();
            //Console.WriteLine("Press entry to continue...");
            //Console.ReadLine();
        }
    }
}
