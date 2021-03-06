﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Threading;
using System.IO;
using System.Xml;        // for XmlTextReader and XmlValidatingReader
using System.Xml.Schema; // for XmlSchemaCollection (which is used later)
using System.Configuration;
using System.Data;
using System.Diagnostics;
using log4net;
using log4net.Config;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;


namespace server_syn_console
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        

        // Incoming data from the client.
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static string data = null;
        const int LENGTH_TO_CUT = 4;
        private static bool isValid = true;
        public static Socket handler;
        // If a validation error occurs,
        // set this flag to false in the
        // validation event handler.
        private static Random random = new Random();
        Object thisLock = new Object();
        private static string sectionName = "appSettings";
        static bool? manual_send_value = null;
        static bool in_first_selection = true;

        private static IPAddress GetLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.Equals(new IPAddress(0x00000000)))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        //Console.WriteLine(ni.Name);
                        //Console.WriteLine(addr.Address);
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                //Console.WriteLine(ip.Address.ToString());
                                return ip.Address;
                            }

                        }
                    }

                }
            }
            return null;
        }

        public static void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes ;
            byte[] bytes_length;
            IPAddress ipAddress = null;
            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            /*
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            for (int i = 0; i < ipHostInfo.AddressList.Length; i++)
                if (ipHostInfo.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ipHostInfo.AddressList[i];
                    break;
                }
            */
            ipAddress = IPAddress.Parse(ConfigurationManager.AppSettings["MUPS_SERVER_IP"]);
            Console.WriteLine(ipAddress);
            log.Info("ip address =" + ipAddress.ToString());
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, int.Parse(ConfigurationManager.AppSettings["MUPS_SERVER_PORT"]));

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                Console.WriteLine("Waiting for a connection...");
                listener.Bind(localEndPoint);
                listener.Listen(10);
                handler = listener.Accept();
                Console.WriteLine("Waiting for data...");
                // Start listening for connections.
                while (true)
                {
                    
                    //Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.
                    //Socket handler = listener.Accept();
                    data = null;
                    isValid = true;
                    // An incoming connection needs to be processed.
                    //while (true)
                    //{
                        data = null;
                        bytes_length = new byte[2];
                        int numBytesRead = handler.Receive(bytes_length);
                        int data_length = GetLittleEndianIntegerFromByteArray(bytes_length, 0);
                        bytes = new byte[data_length];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        Console.WriteLine("Text received : {0}", data);
                        XDocument xml_data = XDocument.Parse(data);
                        
                        string xml_root_tag = xml_data.Root.Name.ToString();
                        xml_parse(handler, xml_root_tag, xml_data);



                    /*
                    // Show the data on the console.
                    Console.WriteLine("Text received : {0}", data);

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes(data);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                     * */
                        Console.WriteLine(
                            @"
Select 0-4 then press enter to send package
0.Triggered-Location-Report Message
1.Unsolicited-Location-Report Event Message
2.Unsolicited-Location-Report Emergency Message
3.Unsolicited-Location-Report Presence Event Message
4.Triggered-Location-Report with Invalid GPS Location Message
5.Unsolicited-Location-Report Absent Event Message
");
                        Console.Write("Select[0-4]:");
                        Thread.Sleep(500);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }
        private static bool xml_validation_with_dtd(string xml, string xml_root_tag)
        {
            string doctype_append = "<?xml version='1.0' encoding='utf-16'?><!DOCTYPE " + xml_root_tag + " SYSTEM \"" + xml_root_tag + ".dtd\">";
            /*
            XmlTextReader r = new XmlTextReader(new System.IO.StringReader(doctype_append + xml));
            XmlValidatingReader v = new XmlValidatingReader(r);
            v.ValidationType = ValidationType.DTD;
            v.ValidationEventHandler += new ValidationEventHandler(v_ValidationEventHandler);
            while (v.Read())
            {
                // Can add code here to process the content.
            }
            v.Close();
             * */
            // Set the validation settings.
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            settings.ValidationType = ValidationType.DTD;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

            // Create the XmlReader object.
            XmlReader reader = XmlReader.Create(new System.IO.StringReader(doctype_append + xml), settings);


            // Parse the file.  
            while (reader.Read()) ;
            
            reader.Close();
            // Check whether the document is valid or invalid.
            if (isValid)
            {
                Console.WriteLine("Document is valid");
                return true;
            }
            else
            {
                Console.WriteLine("Document is invalid");
                return false;
            }
        }
        /*
        static void v_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            isValid = false;
            Console.WriteLine("Validation event\n" + e.Message);

        }
         * */
        // Display any validation errors. 
        private static void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            isValid = false;
            Console.WriteLine("Validation Error: {0}", e.Message);
        }
        static IEnumerable<XName> XmlGetAllElementsXname(XDocument xml_data)
        {
            return (from e1 in xml_data.DescendantNodes().OfType<XElement>() select e1).Select(x => x.Name).Distinct();
        }
        private static void xml_parse(Socket handler, string xml_root_tag, XDocument xml_data)
        {
            string log = xml_data.ToString();
            string now =  DateTime.Now.ToString("yyyyMMddHHmmss");
            
            {
                Log("receive:\r\n" , log);
                // Close the writer and underlying file.
               
            }

            string device = string.Empty;
            IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
            if (elements.Contains(new XElement("suaddr").Name))
                device = XmlGetTagValue(xml_data, "suaddr");
            else
            {
                while (true)
                {
                    SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                    sql_client.connect();
                    string sql_command = @"SELECT 
public.epq_test_loc.device
FROM
  public.epq_test_loc
ORDER BY 
  public.epq_test_loc.id
  Limit 1";
                    DataTable dt = sql_client.get_DataTable(sql_command);
                    sql_client.disconnect();
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        Console.WriteLine("+if");
                        Console.WriteLine("dt:{0}", dt);
                        Console.WriteLine("dt.Rows.Count:{0}", dt.Rows.Count);
                        foreach (DataRow row in dt.Rows)
                        {

                            device = row[0].ToString();


                        }
                        break;
                    }
                    else
                    {
                        Console.WriteLine("+else");
                        sql_client.disconnect();
                        //reload_table();
                        continue;
                    }

                }
            }
            

            switch (xml_root_tag)
            {
                case "Location-Registration-Request":
                    {
                        /*
                        if (!xml_validation_with_dtd(log, xml_root_tag))
                        {
                            string error = "<" + "Unsolicited-Location-Report" + ">" + "<operation-error><result result-code=\"A\">SYNTAX ERROR</result></operation-error>" + "</" + "Unsolicited-Location-Report" + ">";
                            byte[] msg2 = (data_append_dataLength(error));

                            handler.Send(msg2);
                            using (StreamWriter w = File.AppendText("log.txt"))
                            {
                                Log("send:\r\n" ,error, w);
                                // Close the writer and underlying file.
                                w.Close();
                            }
                            break;
                        }
                        */
                        string application_id = XmlGetTagValue(xml_data, "application");
                        Console.WriteLine("application_id : {0}", application_id);
                        

                        string msg_send_back_LRA = "<Location-Registration-Answer><application application-id=\"" + application_id + "\"></application><result result-code=\"0\"></result></Location-Registration-Answer>";
                        string msg_send_back_ULRFP = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><event-info>Unit Present</event-info></Unsolicited-Location-Report>";

                        byte[] msg1 = (data_append_dataLength(msg_send_back_LRA));
                        byte[] msg3 = (data_append_dataLength(msg_send_back_ULRFP));

                        handler.Send(msg1);
                        
                            Log("send:\r\n" , msg_send_back_LRA);
                            // Close the writer and underlying file.
                          
                        /*
                        handler.Send(msg3);
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n" , msg_send_back_ULRFP, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        */
                        //if (!bool.Parse(ConfigurationManager.AppSettings["auto_send"]))
                        if(manual_send_value.Value)
                        {
                            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                            string min_count=string.Empty, max_count=string.Empty;
                            sql_client.connect();
                            string sql_command = @"
SELECT 
  
  public.epq_test_loc.id
FROM
  public.epq_test_loc

ORDER BY 
  public.epq_test_loc.id ASC 
  Limit 1";
                           DataTable dt = sql_client.get_DataTable(sql_command);
                            foreach (DataRow row in dt.Rows)
                            {
                                min_count = row[0].ToString();
                            }
                            sql_client.disconnect();

                            sql_client.connect();
                            sql_command = @"
SELECT 
  
  public.epq_test_loc.id
FROM
  public.epq_test_loc
ORDER BY 
  public.epq_test_loc.id DESC 
  Limit 1";
                            dt = sql_client.get_DataTable(sql_command);
                            foreach (DataRow row in dt.Rows)
                            {
                                max_count = row[0].ToString();
                            }
                            sql_client.disconnect();

                            Thread send_test_thread = new Thread(() => manual_send(handler,min_count,max_count));
                            send_test_thread.Start();
                        }
                        else
                        {
                            
                            /*
                             * SELECT count(DISTINCT
                                  public.epq_test_loc.device)
                                FROM
                                  public.epq_test_loc
                             */
                            int device_count=0;
                            //int device_initial = 900001;
                            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                            sql_client.connect();
                            string sql_command = @"SELECT count(DISTINCT
                                  public.epq_test_loc.device)
                                FROM
                                  public.epq_test_loc";
                            DataTable dt = sql_client.get_DataTable(sql_command);
                            sql_client.disconnect();
                            foreach (DataRow row in dt.Rows)
                            {
                                device_count = Convert.ToInt16(row[0]);
                            }
                            sql_client.connect();
                            sql_command = @"SELECT DISTINCT
                                  public.epq_test_loc.device
                                FROM
                                  public.epq_test_loc
                                ORDER BY 
                                  public.epq_test_loc.device";
                            dt = sql_client.get_DataTable(sql_command);
                            sql_client.disconnect();
                            List<string> device_list = new List<string>();
                            foreach (DataRow row in dt.Rows)
                            {
                                device_list.Add(Convert.ToString(row[0]));
                            }
                            
                            for (int x = 0; x < device_count; x++)
                            {
                                int i=x;
                                //ThreadPool.QueueUserWorkItem(state => autosent_test((device_initial++).ToString(), handler));
                                string min_count=string.Empty, max_count=string.Empty;

                                sql_client.connect();
                                sql_command = @"
SELECT 
  
  public.epq_test_loc.id
FROM
  public.epq_test_loc
WHERE
    public.epq_test_loc.device = '" + device_list[i] + @"' 
ORDER BY 
  public.epq_test_loc.id ASC 
  Limit 1";
                                 dt = sql_client.get_DataTable(sql_command);
                                 foreach (DataRow row in dt.Rows)
                                 {
                                     min_count = row[0].ToString();
                                 }
                                sql_client.disconnect();

                                sql_client.connect();
                                sql_command = @"
SELECT 
  
  public.epq_test_loc.id
FROM
  public.epq_test_loc
WHERE
    public.epq_test_loc.device = '" + device_list[i] + @"' 
ORDER BY 
  public.epq_test_loc.id DESC 
  Limit 1";
                                dt = sql_client.get_DataTable(sql_command);
                                foreach (DataRow row in dt.Rows)
                                {
                                    max_count = row[0].ToString();
                                }
                                sql_client.disconnect();
                                //try
                                //{
                                    ThreadPool.QueueUserWorkItem(state => autosent_test((device_list[i]).ToString(), handler, min_count, max_count, true));
                                //}
                                //catch (Exception ex)
                                //{
                                //    Console.WriteLine(ex.Message);
                                //    Thread.Sleep(5000);
                                //}
                                   
                                
                                
                            }

                        }
                        //sendtest(handler);
                    }
                    break;
                case "Immediate-Location-Request":
                    {
                        string Immediate_Location_Answer = "<Immediate-Location-Answer><request-id></request-id><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><result result-code=\"0\"></result></Immediate-Location-Answer>";
                        string Immediate_Location_Report = "<Immediate-Location-Report><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><info-data><info-time>" + now + "</info-time><server-time>" + now + "</server-time><shape><circle-2d><lat>12.345345</lat><long>24.668866</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Immediate-Location-Report>";
                        byte[] msg1 = (data_append_dataLength(Immediate_Location_Answer));
                        byte[] msg3 = (data_append_dataLength(Immediate_Location_Report));

                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" , Immediate_Location_Answer);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        handler.Send(msg3);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" ,Immediate_Location_Report);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                    }
                    break;
                case "Triggered-Location-Request":
                    {
                        string Triggered_Location_Answer = "<Triggered-Location-Answer><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APC0\">" + device + "</suaddr><result result-code=\"0\"></result></Triggered-Location-Answer>";
                        string Triggered_Location_Report = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><info-data><info-time>" + now + "</info-time><server-time>" + now + "</server-time><shape><circle-2d><lat>12.345345</lat><long>24.668866</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
                        byte[] msg1 = (data_append_dataLength(Triggered_Location_Answer));
                        byte[] msg3 = (data_append_dataLength(Triggered_Location_Report));

                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" , Triggered_Location_Answer);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        handler.Send(msg3);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" ,Triggered_Location_Report);
                            // Close the writer and underlying file.
                           // w.Close();
                        //}
                    }
                    break;
                case "Digital-Output-Change-Request":
                    {
                        string Digital_Output_Answer = "<Digital-Output-Answer><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><result result-code=\"0\"></result></Digital-Output-Answer>";
                        byte[] msg1 = (data_append_dataLength(Digital_Output_Answer));

                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" , Digital_Output_Answer);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                    }
                    break;
                case "Location-Protocol-Request":
                    {
                        string Location_Protocol_Report = "<Location-Protocol-Report><request-id>5621A</request-id><protocol-version>2</protocol-version></Location-Protocol-Report>";
                        byte[] msg1 = (data_append_dataLength(Location_Protocol_Report));

                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" , Location_Protocol_Report);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                    }
                    break;
                case "Triggered-Location-Stop-Request":
                    {
                        string Triggered_Location_Stop_Answer = "<Triggered-Location-Stop-Answer><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><result result-code=\"0\"></result></Triggered-Location-Stop-Answer>";
                        byte[] msg1 = (data_append_dataLength(Triggered_Location_Stop_Answer));

                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" , Triggered_Location_Stop_Answer);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                    }
                    break;
                default:
                    {
                        //error occur
                        string root = xml_root_tag;
                        string error = "<" + root + ">" + "<operation-error><result result-code=\"A\">SYNTAX ERROR</result></operation-error>" + "</" + root + ">";
                        byte[] msg1 = (data_append_dataLength(error));

                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n" , error);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                    }
                    break;
            }
        }

        private static void autosent_test(string device, Socket handler,string min_count,string max_count,bool initial)
        {
            string device_count = "device_" + device + "_count";
            string current_count = string.Empty;
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            DataTable dt = new DataTable(); 
            //throw new NotImplementedException();
            //if (ConfigurationManager.AppSettings.AllKeys.Contains(device_count))
            {
                // the config file contains the specific key 
                //ConfigurationManager.RefreshSection("appSettings");
                //current_count = ConfigurationManager.AppSettings[device_count].ToString();
            }
            //else
            /*
            if (initial)
            {
                sql_client.connect();
                sql_client.modify("DROP SEQUENCE " + device_count );
                sql_client.disconnect();
                initial = false;
            }
             * */
               
                //System.Configuration.Configuration config =ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                //config.AppSettings.Settings.Add(device_count, min_count);
                //config.Save();
                //ConfigurationManager.RefreshSection("appSettings");
                //current_count = ConfigurationManager.AppSettings[device_count].ToString();

                //CREATE SEQUENCE serial MINVALUE 34114 MAXVALUE 34981 START 34979 CYCLE 
                sql_client.connect();
                //sql_client.modify("CREATE SEQUENCE " + device_count + " MINVALUE " + min_count + " MAXVALUE " + max_count + " START " + min_count + " CYCLE ");
                sql_client.modify(@"SELECT create_seq('"+device_count+@"'"+@",'public'," + min_count + "," + max_count + ")");
                sql_client.disconnect();
                Console.WriteLine("CREATE SEQUENCE " + device_count + " MINVALUE " + min_count + " MAXVALUE " + max_count + " START " + min_count + " CYCLE ");
                //Thread.Sleep(1000);
            
            
            while(true)
            {
                try
                {
                    
                ///TODO:modify current_count to auto increase
                    ///SELECT nextval('device + "_counter"');

                    sql_client.connect();
                    dt =sql_client.get_DataTable(@"SELECT nextval('"+device_count+@"');");
                    sql_client.disconnect();
                    foreach (DataRow row in dt.Rows)
                    {
                        current_count = row[0].ToString();
                        //ConfigurationManager.AppSettings[device_count] = current_count;
                        //System.Configuration.Configuration config =
                  //ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        //config.Save();
                        //ConfigurationManager.RefreshSection("appSettings");
                    }

                    sql_client.connect();
                    string lat = string.Empty, lon = string.Empty, id = string.Empty, sql_command = @"SELECT 
  public.epq_test_loc.longitude,
  public.epq_test_loc.latitude,
  public.epq_test_loc.device,
  public.epq_test_loc.id
FROM
  public.epq_test_loc
WHERE
    public.epq_test_loc.id = '" + current_count + @"' 
";
                    dt = sql_client.get_DataTable(sql_command);
                    sql_client.disconnect();
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        Console.WriteLine("+if");
                        Console.WriteLine("dt:{0}", dt);
                        Console.WriteLine("dt.Rows.Count:{0}", dt.Rows.Count);
                        foreach (DataRow row in dt.Rows)
                        {
                            lon = row[0].ToString();
                            lat = row[1].ToString();
                            device = row[2].ToString();
                            id = row[3].ToString();
                        }
                    }
                    else
                        break;
                    //sql_client.modify("DELETE FROM public.epq_test_loc WHERE public.epq_test_loc.id = \'" + id + "\'");
                    

                    string today = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string Triggered_loc = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><info-data><info-time>" + today + "</info-time><server-time>" + today + "</server-time><shape><circle-2d><lat>" + lat + "</lat><long>" + lon + "</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
                    byte[] msg4 = (data_append_dataLength(Triggered_loc));
                    handler.Send(msg4);
                    //using (StreamWriter w = File.AppendText("log.txt"))
                    //{
                        Log("send:\r\n", Triggered_loc);
                        // Close the writer and underlying file.
                        //w.Close();
                    //}
                    Thread.Sleep(int.Parse(ConfigurationManager.AppSettings["auto_send_sec_interval"]) * 1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void manual_send(Socket handler,string min_count,string max_count)
        {

            

            while (true)
            {
                DataTable dt = new DataTable();
                string current_count = string.Empty;
                Console.WriteLine(
                    @"
Select 0-4 then press enter to send package
0.Triggered-Location-Report Message
1.Unsolicited-Location-Report Event Message
2.Unsolicited-Location-Report Emergency Message
3.Unsolicited-Location-Report Presence Event Message
4.Triggered-Location-Report with Invalid GPS Location Message
5.Unsolicited-Location-Report Absent Event Message
");
                ///auto send from fixed interval time
                ///900001->10sec interval
                ///900005->50sec interval
                Console.Write("Select[0-4]:");
                string select_num = string.Empty;
                select_num = Console.ReadLine();

                SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);

                sql_client.connect();
                //sql_client.modify("CREATE SEQUENCE " + "manual_count" + " MINVALUE " + min_count + " MAXVALUE " + max_count + " START " + min_count + " CYCLE ");
                sql_client.modify(@"SELECT create_seq('manual_count','public'," + min_count + "," + max_count + ")");
                sql_client.disconnect();

                sql_client.connect();
                dt = sql_client.get_DataTable(@"SELECT nextval('" + "manual_count" + @"');");
                sql_client.disconnect();
                foreach (DataRow row in dt.Rows)
                {
                    current_count = row[0].ToString();
                    //ConfigurationManager.AppSettings[device_count] = current_count;
                    //System.Configuration.Configuration config =
                    //ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    //config.Save();
                    //ConfigurationManager.RefreshSection("appSettings");
                }
                
                sql_client.connect();
                string lat = string.Empty,lon = string.Empty,id = string.Empty,device=string.Empty,sql_command = @"SELECT 
  public.epq_test_loc.longitude,
  public.epq_test_loc.latitude,
  public.epq_test_loc.device,
  public.epq_test_loc.id
FROM
  public.epq_test_loc
WHERE
  public.epq_test_loc.id=" + @"'" + current_count + @"'";
                 dt = sql_client.get_DataTable(sql_command);
                if (dt != null && dt.Rows.Count!=0)
                {
                    Console.WriteLine("+if");
                    Console.WriteLine("dt:{0}",dt);
                    Console.WriteLine("dt.Rows.Count:{0}", dt.Rows.Count);
                    foreach (DataRow row in dt.Rows)
                    {
                        lon = row[0].ToString();
                        lat = row[1].ToString();
                        device = row[2].ToString();
                        id = row[3].ToString();
                    }
                    sql_client.disconnect();
                }
                else
                {
                    Console.WriteLine("+else");
                    sql_client.disconnect();
                    
                    continue;
                }
                //sql_client.disconnect();

                //double lat = Convert.ToDouble("18." + random.Next(516400146, 630304598)); //18.51640014679267 - 18.630304598192915
                //double lon = Convert.ToDouble("-72." + random.Next(224464416, 341194152)); //-72.34119415283203 - -72.2244644165039

                string today = DateTime.Now.ToString("yyyyMMddHHmmss");
                Console.WriteLine("+manual_send");
                string Unsolicited_event = "<Unsolicited-Location-Report><event-info>Ignition Off</event-info><suaddr suaddr-type=\"APCO\">"+device+"</suaddr><info-data><info-time>" + today + "</info-time><server-time>" + today + "</server-time><shape><circle-2d><lat>" + lat + "</lat><long>" + lon + "</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Unsolicited-Location-Report>";
                //string Unsolicited_emer = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">"+device+"</suaddr><event-info>Emergency On</event-info><info-data><info-time>20081012185257</info-time><server-time>20081012165257</server-time><shape><point-3d><lat>40.697595</lat><long>-73.984557</long><altitude>0</altitude></point-3d></shape><speed-hor>0</speed-hor><direction-hor>184</direction-hor></info-data></Unsolicited-Location-Report>";
                string Unsolicited_emer = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">"+device+"</suaddr><event-info>Emergency On</event-info><info-data><info-time>" + today + "</info-time><server-time>" + today + "</server-time><shape><circle-2d><lat>" + lat + "</lat><long>" + lon + "</long><radius>0</radius></circle-2d></shape><speed-hor>0</speed-hor><direction-hor>184</direction-hor></info-data></Unsolicited-Location-Report>";

                string Unsolicited_pres = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">"+device+"</suaddr><event-info>Unit Present</event-info></Unsolicited-Location-Report>";
                string Triggered_loc = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">"+device+"</suaddr><info-data><info-time>" + today + "</info-time><server-time>" + today + "</server-time><shape><circle-2d><lat>" + lat + "</lat><long>" + lon + "</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
                string Triggered_loc_invalid_gps = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">"+device+"</suaddr><operation-error><result result-code=\"1006\">GPS INVALID</result></operation-error></Triggered-Location-Report>";
                string Unsolicited_Absent = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">" + device + "</suaddr><event-info>Unit Absent</event-info></Unsolicited-Location-Report>";
                byte[] msg1 = (data_append_dataLength(Unsolicited_event));
                byte[] msg2 = (data_append_dataLength(Unsolicited_emer));
                byte[] msg3 = (data_append_dataLength(Unsolicited_pres));
                byte[] msg4 = (data_append_dataLength(Triggered_loc));
                byte[] msg5 = (data_append_dataLength(Triggered_loc_invalid_gps));
                byte[] msg6 = (data_append_dataLength(Unsolicited_Absent));
                /*
                Console.WriteLine(
                    @"
Select 0-4 then press enter to send package
0.Triggered-Location-Report Message
1.Unsolicited-Location-Report Event Message
2.Unsolicited-Location-Report Emergency Message
3.Unsolicited-Location-Report Presence Event Message
4.Triggered-Location-Report with Invalid GPS Location Message
");
                ///auto send from fixed interval time
                ///900001->10sec interval
                ///900005->50sec interval
                Console.Write("Select[0-4]:");
                string select_num=string.Empty;
                select_num = Console.ReadLine();
                */
                
                /*
                if (select_num == "3" || select_num == "4")
                {
                    //sql_client.modify("DELETE FROM public.epq_test_loc WHERE public.epq_test_loc.id = \'" + id + "\'");
                    sql_client.disconnect();
                }
                else
                {
                    //sql_client.modify("DELETE FROM public.epq_test_loc WHERE public.epq_test_loc.id = \'" + id + "\'");
                    sql_client.disconnect();
                }
                */
                 
                switch (select_num)
                {
                    case "0":
                        handler.Send(msg4);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n", Triggered_loc);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        break;
                    case "1":
                        handler.Send(msg1);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n", Unsolicited_event);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        break;
                    case "2":
                        handler.Send(msg2);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n", Unsolicited_emer);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        break;
                    case "3":
                        handler.Send(msg3);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n", Unsolicited_pres);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        break;
                    case "4":
                        handler.Send(msg5);
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        //{
                            Log("send:\r\n", Triggered_loc_invalid_gps);
                            // Close the writer and underlying file.
                            //w.Close();
                        //}
                        break;
                    case "5":
                        handler.Send(msg6);
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Triggered_loc_invalid_gps);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        break;
                }
                /*
                handler.Send(msg1);
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log("send:\r\n", Unsolicited_event, w);
                    // Close the writer and underlying file.
                    w.Close();
                }
                handler.Send(msg2);
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log("send:\r\n", Unsolicited_emer, w);
                    // Close the writer and underlying file.
                    w.Close();
                }
                handler.Send(msg3);
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log("send:\r\n", Unsolicited_pres, w);
                    // Close the writer and underlying file.
                    w.Close();
                }
                handler.Send(msg4);
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log("send:\r\n", Triggered_loc, w);
                    // Close the writer and underlying file.
                    w.Close();
                }
                handler.Send(msg5);
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log("send:\r\n", Triggered_loc_invalid_gps, w);
                    // Close the writer and underlying file.
                    w.Close();
                }
                */
                
                Thread.Sleep(100);
            }
        }

        /*
        private static void sendtest(Socket handler)
        {
            string Unsolicited_event = "<Unsolicited-Location-Report><event-info>Ignition Off</event-info><suaddr suaddr-type=\"APCO\">1004</suaddr><info-data><info-time>20130630073000</info-time><server-time>20130630073000</server-time><shape><circle-2d><lat>12.345345</lat><long>24.668866</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Unsolicited-Location-Report>";
            //string Unsolicited_emer = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">1004</suaddr><event-info>Emergency On</event-info><info-data><info-time>20081012185257</info-time><server-time>20081012165257</server-time><shape><point-3d><lat>40.697595</lat><long>-73.984557</long><altitude>0</altitude></point-3d></shape><speed-hor>0</speed-hor><direction-hor>184</direction-hor></info-data></Unsolicited-Location-Report>";
            string Unsolicited_emer = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">1004</suaddr><event-info>Emergency On</event-info><info-data><info-time>20081012185257</info-time><server-time>20081012165257</server-time><shape><circle-2d><lat>40.697595</lat><long>-73.984557</long><radius>0</radius></circle-2d></shape><speed-hor>0</speed-hor><direction-hor>184</direction-hor></info-data></Unsolicited-Location-Report>";
            
            string Unsolicited_pres = "<Unsolicited-Location-Report><suaddr suaddr-type=\"APCO\">1004</suaddr><event-info>Unit Present</event-info></Unsolicited-Location-Report>";
            string Triggered_loc = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">1004</suaddr><info-data><info-time>20130630073000</info-time><server-time>20130630073000</server-time><shape><circle-2d><lat>12.345345</lat><long>24.668866</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
            
            byte[] msg1 = (data_append_dataLength(Unsolicited_event));
            byte[] msg2 = (data_append_dataLength(Unsolicited_emer));
            byte[] msg3 = (data_append_dataLength(Unsolicited_pres));
            byte[] msg4 = (data_append_dataLength(Triggered_loc));


            handler.Send(msg1);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Unsolicited_event, w);
                // Close the writer and underlying file.
                w.Close();
            }
            handler.Send(msg2);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Unsolicited_emer, w);
                // Close the writer and underlying file.
                w.Close();
            }
            handler.Send(msg3);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Unsolicited_pres, w);
                // Close the writer and underlying file.
                w.Close();
            }
            handler.Send(msg4);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_loc, w);
                // Close the writer and underlying file.
                w.Close();
            }
        }
        */
        static string XmlGetTagAttributeValue(XDocument xml_data, string tag_name, string tag_attribute_name)
        {
            string result = string.Empty;
            try
            {
                result = (string)(from e1 in xml_data.Descendants(tag_name) select e1.Attribute(tag_attribute_name).Value).First();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                result = "error";
            }

            return result;

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
        static int GetLittleEndianIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex])
                 | (data[startIndex + 1] << 8);
            //| (data[startIndex + 2] << 8)
            //| data[startIndex + 3];
        }
        static byte[] data_append_dataLength(string data)
        {
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(data);
            byte[] data_length = int_to_hex_little_endian(data.Length);

            byte[] rv = new byte[data_length.Length + byteArray.Length];
            System.Buffer.BlockCopy(data_length, 0, rv, 0, data_length.Length);
            System.Buffer.BlockCopy(byteArray, 0, rv, data_length.Length, byteArray.Length);
            return rv;
        }
        static byte[] int_to_hex_little_endian(int length)
        {
            var reversedBytes = System.Net.IPAddress.NetworkToHostOrder(length);
            string hex = reversedBytes.ToString("x");
            string trimmed = hex.Substring(0, LENGTH_TO_CUT);
            //System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            //Byte[] bytes = encoding.GetBytes(trimmed);
            byte[] bytes = StringToByteArray(trimmed);
            //string str = System.Text.Encoding.ASCII.GetString(bytes);
            return bytes;
            //return HexAsciiConvert(trimmed);
        }
        /*
        static string HexAsciiConvert(string hex)
        {

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i <= hex.Length - 2; i += 2)
            {

                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hex.Substring(i, 2),

                System.Globalization.NumberStyles.HexNumber))));

            }

            return sb.ToString();

        }
         * */
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public static void Log(string send_receive, String logMessage)
        {
            try
            {
                XDocument xml = XDocument.Parse(logMessage);
                logMessage = send_receive + xml.ToString();
                /*
                w.Write("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToString("H:mm:ss.fffffff"),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("  :");
                w.WriteLine("  :{0}", logMessage);
                w.WriteLine("-------------------------------");
                // Update the underlying file.
                w.Flush();
                 * */
                log.Info(logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                log.Info(logMessage);
            }
        }
        /*
        public static void demo_t(Object stateInfo)
        {
            try
            {
                Console.WriteLine("in demo_t");
                SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
                Console.WriteLine(System.DateTime.Now);
                sql_client.connect();
                DataTable data_table = sql_client.get_DataTable("SELECT  * FROM  public.epq_test_loc ORDER BY id LIMIT 1");
                Console.WriteLine("[device]:" + data_table.Rows[0]["device"]);
                Console.WriteLine("[longitude]:" + data_table.Rows[0]["longitude"]);
                Console.WriteLine("[latitude]:" + data_table.Rows[0]["latitude"]);
                string Triggered_Location_Report = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">" + data_table.Rows[0]["device"].ToString() + "</suaddr><info-data><info-time>" + DateTime.Now.ToString("yyyyMMddHHmmss") + "</info-time><server-time>" + DateTime.Now.ToString("yyyyMMddHHmmss") + "</server-time><shape><circle-2d><lat>" + data_table.Rows[0]["latitude"].ToString() + "</lat><long>" + data_table.Rows[0]["longitude"].ToString() + "</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
                byte[] msg3 = (data_append_dataLength(Triggered_Location_Report));
                if (handler != null)
                {
                    handler.Send(msg3);
                    sql_client.modify("DELETE FROM public.epq_test_loc WHERE id IN (SELECT id FROM public.epq_test_loc ORDER BY id LIMIT 1)");
                    using (StreamWriter w = File.AppendText("log.txt"))
                    {
                        Log("send:\r\n" + Triggered_Location_Report, w);
                        // Close the writer and underlying file.
                        w.Close();
                    }
                }
                sql_client.disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "_" + DateTime.Now.ToString("h:mm:ss.fff"));
            }

        }
         * */
        /*
        public static void demo_ttt()
        {
            while (true)
            {
                SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
                    
                try
                {
                    Console.WriteLine("in demo_ttt");
                    Console.WriteLine(System.DateTime.Now);
                    sql_client.connect();
                    DataTable data_table = sql_client.get_DataTable("SELECT  longitude,latitude,device,id FROM  public.epq_test_loc order by random() limit 1");
                    if ((data_table == null) || (data_table.Rows.Count==0))
                    {
                        Console.WriteLine("data_table == null");
                    }
                    else
                        Console.WriteLine("data_table != null");
                    Console.WriteLine("[device]:" + data_table.Rows[0]["device"]);
                    Console.WriteLine("[longitude]:" + data_table.Rows[0]["longitude"]);
                    Console.WriteLine("[latitude]:" + data_table.Rows[0]["latitude"]);
                    string Triggered_Location_Report = "<Triggered-Location-Report><suaddr suaddr-type=\"APCO\">" + data_table.Rows[0]["device"].ToString() + "</suaddr><info-data><info-time>" + DateTime.Now.ToString("yyyyMMddHHmmss") + "</info-time><server-time>" + DateTime.Now.ToString("yyyyMMddHHmmss") + "</server-time><shape><circle-2d><lat>" + data_table.Rows[0]["latitude"].ToString() + "</lat><long>" + data_table.Rows[0]["longitude"].ToString() + "</long><radius>100</radius></circle-2d></shape><speed-hor>50</speed-hor><direction-hor>32</direction-hor></info-data><sensor-info><sensor><sensor-name>Ignition</sensor-name><sensor-value>off</sensor-value><sensor-type>Input</sensor-type></sensor><sensor><sensor-name>door</sensor-name><sensor-value>open</sensor-value><sensor-type>Input</sensor-type></sensor></sensor-info><vehicle-info><odometer>10,000</odometer></vehicle-info></Triggered-Location-Report>";
                    byte[] msg3 = (data_append_dataLength(Triggered_Location_Report));
                    if (handler != null)
                    {
                        handler.Send(msg3);
                        sql_client.modify("DELETE FROM public.epq_test_loc WHERE id =" + data_table.Rows[0]["id"] + ")");
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n" , Triggered_Location_Report, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "_" + DateTime.Now.ToString("h:mm:ss.fff"));
                }
                finally
                {
                    sql_client.disconnect();
                }
                Thread.Sleep(int.Parse(ConfigurationManager.AppSettings["Timer_period"]) * 1000);
            }
           
            
        }
        */
        static void reload_table()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            Console.WriteLine("Refill the table with kml data...");
            string kml_application = "ConsoleApplication1_access_kml_files.exe";

            Process SomeProgram = new Process();
            SomeProgram.StartInfo.FileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\" + kml_application;
            /*
            SomeProgram.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            SomeProgram.StartInfo.UseShellExecute = false;
            SomeProgram.StartInfo.RedirectStandardOutput = true;
            SomeProgram.StartInfo.CreateNoWindow = true;
            */
            SomeProgram.Start();
            SomeProgram.WaitForExit();
            //string SomeProgramOutput = SomeProgram.StandardOutput.ReadToEnd();
            Console.WriteLine("Refill the table with kml data done...");
            ShowWindow(handle, SW_SHOW);
            SetForegroundWindow(handle);
        }
        static void Main(string[] args)
        {
            while (in_first_selection)
            {
                Console.WriteLine(@"
1.Auto send test
2.Manual send test
3.Reload All KML files
4.Next");
                Console.Write("Select[1-4]:");
                string select_num = string.Empty;
                select_num = Console.ReadLine();
                switch (select_num)
                {
                    case "1":
                        manual_send_value = false;
                        break;
                    case "2":
                        manual_send_value = true;
                        break;
                    case "3":
                        reload_table();
                        break;
                    case "4":
                        if (manual_send_value.HasValue)
                            in_first_selection = false;
                        else
                            Console.WriteLine("Necessary to select 1 or 2!!!");
                        break;
                }
            }

            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            sql_client.connect();
            sql_client.modify(@"
CREATE OR REPLACE FUNCTION create_seq(_seq text, _schema text = 'public',_min INTEGER =0, _max INTEGER =100)
  RETURNS void LANGUAGE plpgsql STRICT AS
$func$
DECLARE
   _fullname text := _schema || '.' || _seq;

BEGIN
-- If an object of the name exists, the first RAISE EXCEPTION goes through
-- with a meaningful message immediately.
-- Else, the cast `::regclass` raises its own EXCEPTION ""undefined_table""
-- which prevents RAISE from executing.
-- This particular error triggers sequence creation further down.

RAISE EXCEPTION 'Object >>%<< of type ""%"" already exists.'
   ,_fullname
   ,c.relkind FROM  pg_class c
              WHERE c.oid = (_schema || '.' || _seq)::regclass;

EXCEPTION
   WHEN undefined_table THEN -- SQLSTATE '42P01'
   EXECUTE 'CREATE SEQUENCE ' || quote_ident(_schema) || '.'
                              || quote_ident(_seq) ||
                              ' MINVALUE ' || _min ||
                              ' MAXVALUE ' || _max ||
                              ' START ' || _min ||
                              ' CYCLE ';
   RAISE NOTICE 'New sequence >>%<< created.', _fullname;
END
$func$;

            ");
            sql_client.modify(@"

COMMENT ON FUNCTION create_seq(text, text,INTEGER,INTEGER) IS 'Create new seq if name is free.
$1 _seq  .. name of sequence
$2 _name .. name of schema (optional; default is ""public"")
$3 _min  .. name of MINVALUE (optional; default is ""0"")
$4 _max .. name of MAXVALUE (optional; default is ""100"")';
");
            sql_client.disconnect();
            
            /*
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
            sql_client.connect();
            string sql_command = @"SELECT DISTINCT
                                  public.epq_test_loc.device
                                FROM
                                  public.epq_test_loc";
            DataTable dt = sql_client.get_DataTable(sql_command);
            List<string> device_list = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                device_list.Add(Convert.ToString(row[0]));
            }
            sql_client.disconnect();
            */

            // Force a reload of the changed section. This 
            // makes the new values available for reading.
            ConfigurationManager.RefreshSection(sectionName);
            /*
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
            sql_client.connect();
            string sql_command = @"SELECT 
public.epq_test_loc.device
FROM
  public.epq_test_loc
ORDER BY 
  public.epq_test_loc.id
  Limit 1";
            DataTable dt = sql_client.get_DataTable(sql_command);
            sql_client.disconnect();
            if (dt != null && dt.Rows.Count != 0)
            {
                
            }
            else
            //if (bool.Parse(ConfigurationManager.AppSettings["auto_send"]))
            {
                Console.WriteLine("Refill the table with kml data...");
                string kml_application = "ConsoleApplication1_access_kml_files.exe";

                Process SomeProgram = new Process();
                SomeProgram.StartInfo.FileName = kml_application;
                
                //SomeProgram.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                //SomeProgram.StartInfo.UseShellExecute = false;
                //SomeProgram.StartInfo.RedirectStandardOutput = true;
                //SomeProgram.StartInfo.CreateNoWindow = true;
                
                SomeProgram.Start();
                SomeProgram.WaitForExit();
                //string SomeProgramOutput = SomeProgram.StandardOutput.ReadToEnd();
                Console.WriteLine("Refill the table with kml data done...");
            }
            */
            //Console.WriteLine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            

            Thread read_thread = new Thread(new ThreadStart(StartListening));
            read_thread.Start();
            //Thread demo_tt = new Thread(new ThreadStart(demo_ttt));
            //if (bool.Parse(ConfigurationManager.AppSettings["SQL_ACCESS"]))
            //demo_tt.Start();
            /*
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            // Create the delegate that invokes methods for the timer.
            TimerCallback timerDelegate =
                new TimerCallback(demo_t);
            Timer stateTimer =
                new Timer(timerDelegate, autoEvent, 0, int.Parse(ConfigurationManager.AppSettings["Timer_period"]) * 1000);
            autoEvent.WaitOne(-1);
             * */
             

            




        }
    }
}
