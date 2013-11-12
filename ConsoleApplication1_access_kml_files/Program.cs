using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Configuration;
using System.Text.RegularExpressions;
using System.IO;


namespace ConsoleApplication1_access_kml_files
{
    class Program
    {
        private static string sectionName = "appSettings";

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
        
        static void Main(string[] args)
        {
            // Force a reload of the changed section. This 
            // makes the new values available for reading.
            ConfigurationManager.RefreshSection(sectionName);

            int LENGTH  = Directory.GetFiles(Environment.CurrentDirectory, "*.kml", SearchOption.TopDirectoryOnly).Length;
            string[] file_list = Directory.GetFiles(Environment.CurrentDirectory, "*.kml", SearchOption.TopDirectoryOnly);
            //int device_initial = 900001;
            List<string> device = new List<string>();
            Console.WriteLine(LENGTH);
            XDocument[] xml_load = new XDocument[LENGTH];
            //for (int i = 0; i < LENGTH; i++)
            foreach(string a in file_list)
            {
                //device.Add((device_initial++).ToString());
                device.Add((a).ToString());
            }
            //string[]  device= new string[5] { "900001", "900002", "900003", "900004", "900005" };
            for (int i = 0; i < xml_load.Length; i++)
            {
                //xml_load[i] = XDocument.Load("test" + (i + 1) + ".kml");
                xml_load[i] = XDocument.Load(device[i]);
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
            sql_client.modify("TRUNCATE TABLE public.epq_test_loc");
            sql_client.disconnect();
            sql_client.connect();
            sql_client.modify("TRUNCATE TABLE public._gps_log");
            sql_client.disconnect();
            sql_client.connect();
            sql_client.modify("DROP SEQUENCE  manual_count");
            sql_client.disconnect();
            sql_client.connect();
            sql_client.modify(@"DROP FUNCTION create_seq(text, text,INTEGER,INTEGER)");
            sql_client.disconnect();
            for (int j = 0; j < xml_load.Length; j++)
            {
                string receive = string.Empty;
                //XDocument xdoc = XDocument.Load("test2.kml");
                XNamespace KmlNamespace = "http://earth.google.com/kml/2.2";
                var result = (from el in xml_load[j].Descendants(KmlNamespace + "coordinates") select el);
                string device_name = XmlGetTagValue(xml_load[j], (KmlNamespace + "name").ToString());
                string device_count = "device_" + device_name + "_count";
                sql_client.connect();
                sql_client.modify("DROP SEQUENCE " + device_count);
                
                sql_client.disconnect();
                foreach (string cc in result)
                {
                    receive += cc + Environment.NewLine;
                }
                Console.WriteLine(receive);

                //string receive = XmlGetTagValue(xml_load[j], "coordinates");
                string[] parts = receive.Split(new char[] { '\r', '\n', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i = i + 3)
                {
                    if ((i + 1) % 3 != 0)
                    {
                        Console.WriteLine(i + ":" + parts[i]);
                        Console.WriteLine(i + 1 + ":" + parts[i + 1]);
                        sql_client.connect();
                        sql_client.modify("INSERT INTO public.epq_test_loc (longitude,latitude,device) VALUES (" + "\'" + parts[i] + "\'" + "," + "\'" + parts[i + 1] + "\'" + "," + "\'" + device_name + "\'" + ")");
                        sql_client.disconnect();
                    }
                }
            }
           
            //Console.WriteLine("Press entry to continue...");
            //Console.ReadLine();
        }
    }
}
