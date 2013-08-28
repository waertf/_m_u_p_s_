using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Collections.Specialized;
using System.Xml.Linq;
using System.IO;
using Devart.Data.PostgreSql;
using System.Collections;
using System.Xml;        // for XmlTextReader and XmlValidatingReader
using System.Xml.Schema; // for XmlSchemaCollection (which is used later)


namespace ConsoleApplication1_client_threading
{
    class Program
    {
        //static TcpClient tcpClient = null;
        //static NetworkStream netStream = null;
        const int LENGTH_TO_CUT = 4;
        private static bool isValid = true; 
        static void Main(string[] args)
        {
            //string ipAddress = "127.0.0.1";
            string ipAddress = ConfigurationManager.AppSettings["MUPS_SERVER_IP"];
            //int port = 23;
            int port = int.Parse(ConfigurationManager.AppSettings["MUPS_SERVER_PORT"]);
            
            TcpClient tcpClient = new TcpClient();

            tcpClient.Connect(ipAddress, port);

            tcpClient.NoDelay = false;
            
            NetworkStream netStream = tcpClient.GetStream();

            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);

            string registration_msg_error_test = "<Location-Registration-Request><application>" + ConfigurationManager.AppSettings["application_ID"] + "</application></Location-Registration-Request>";
            WriteLine(netStream, data_append_dataLength(registration_msg_error_test), registration_msg_error_test, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , registration_msg_error_test, w);
                // Close the writer and underlying file.
                w.Close();
            }
            
            //sendtest(netStream);
            
            //alonso
            Thread read_thread = new Thread(() => read_thread_method(tcpClient, netStream, sql_client));
            read_thread.Start();
            Thread send_test_thread = new Thread(() => sendtest2_t(netStream, sql_client));
            send_test_thread.Start();

            //Thread send_test_thread = new Thread(() => sendtest(netStream, sql_client));
            //send_test_thread.Start();
            //output = ReadLine(tcpClient, netStream, output);
            //WriteLine(netStream, String.Join("\n", commands) + "\n");

            
            //tcpClient.Close();
        }

        private static void sendtest(NetworkStream netStream , SqlClient sql_client)
        {
            string Immediate_Location_Request = "<Immediate-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr></Immediate-Location-Request>";
            WriteLine(netStream, data_append_dataLength(Immediate_Location_Request), Immediate_Location_Request, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Immediate_Location_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Location_Protocol_Request = "<Location-Protocol-Request><request-id>4356A</request-id><request-protocol-version>2</request-protocol-version></Location-Protocol-Request>";
            WriteLine(netStream, data_append_dataLength(Location_Protocol_Request), Location_Protocol_Request, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Location_Protocol_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Triggered_Location_Stop_Request = "<Triggered-Location-Stop-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr></Triggered-Location-Stop-Request>";
            WriteLine(netStream, data_append_dataLength(Triggered_Location_Stop_Request), Triggered_Location_Stop_Request, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_Location_Stop_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Triggered_Location_Request_Cadence = "<Triggered-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr><periodic-trigger><interval>60</interval></periodic-trigger></Triggered-Location-Request>";
            WriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Cadence), Triggered_Location_Request_Cadence, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_Location_Request_Cadence, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Triggered_Location_Request_Distance = "<Triggered-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr><periodic-trigger><trg-distance>100</trg-distance></periodic-trigger></Triggered-Location-Request>";
            WriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Distance), Triggered_Location_Request_Distance, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_Location_Request_Distance, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Digital_Output_Change_Request = "<Digital-Output-Change-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1234568</suaddr><output-info><output-name>Alarm</output-name><output-value>1</output-value></output-info></Digital-Output-Change-Request>";
            WriteLine(netStream, data_append_dataLength(Digital_Output_Change_Request), Digital_Output_Change_Request, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" ,Digital_Output_Change_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }
            string error = "<error></error>";
            WriteLine(netStream, data_append_dataLength(error), error, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , error, w);
                // Close the writer and underlying file.
                w.Close();
            }
        }
        private static void sendtest2_t(NetworkStream netStream, SqlClient sql_client)
        {
            while (true)
            {
                /*
                Console.WriteLine(
                        @"
Select 1-6 then press enter to send package
1.Immediate-Location-Request Message
2.Triggered-Location-Request for Change Cadence Message
3.Triggered-Location-Request for Change Distance Message
4.Digital-Output-Change-Request Message
5.Location-Protocol-Request Message
6.Triggered-Location-Stop-Request Message
");
                Console.Write("Select[1-6]:");
                 * */
                string select_num = Console.ReadLine();

                switch (select_num)//ConfigurationManager.AppSettings["output-value"]
                {
                    case "1":
                        string Immediate_Location_Request = "<Immediate-Location-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr></Immediate-Location-Request>"; 
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Immediate_Location_Request, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        WriteLine(netStream, data_append_dataLength(Immediate_Location_Request), Immediate_Location_Request, sql_client);
                        
                        break;
                    case "5":
                        string Location_Protocol_Request = "<Location-Protocol-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><request-protocol-version>2</request-protocol-version></Location-Protocol-Request>";
                        
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Location_Protocol_Request, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        WriteLine(netStream, data_append_dataLength(Location_Protocol_Request), Location_Protocol_Request, sql_client);
                        break;
                    case "6":
                        string Triggered_Location_Stop_Request = "<Triggered-Location-Stop-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr></Triggered-Location-Stop-Request>";
                        
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Triggered_Location_Stop_Request, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        WriteLine(netStream, data_append_dataLength(Triggered_Location_Stop_Request), Triggered_Location_Stop_Request, sql_client);
                        break;
                    case "2":
                        string Triggered_Location_Request_Cadence = "<Triggered-Location-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><periodic-trigger><interval>" + ConfigurationManager.AppSettings["interval"] + "</interval></periodic-trigger></Triggered-Location-Request>";
                        
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Triggered_Location_Request_Cadence, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        WriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Cadence), Triggered_Location_Request_Cadence, sql_client);
                        break;
                    case "3":
                        string Triggered_Location_Request_Distance = "<Triggered-Location-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><periodic-trigger><trg-distance>" + ConfigurationManager.AppSettings["trg-distance"] + "</trg-distance></periodic-trigger></Triggered-Location-Request>";
                        
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Triggered_Location_Request_Distance, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        WriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Distance), Triggered_Location_Request_Distance, sql_client);
                        break;
                    case "4":
                        string Digital_Output_Change_Request = "<Digital-Output-Change-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><output-info><output-name>" + ConfigurationManager.AppSettings["output-name"] + "</output-name><output-value>" + ConfigurationManager.AppSettings["output-value"] + "</output-value></output-info></Digital-Output-Change-Request>";
                        
                        using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            Log("send:\r\n", Digital_Output_Change_Request, w);
                            // Close the writer and underlying file.
                            w.Close();
                        }
                        WriteLine(netStream, data_append_dataLength(Digital_Output_Change_Request), Digital_Output_Change_Request, sql_client);
                        break;
                }
                Thread.Sleep(100);
            }
            /*                                
            string error = "<error></error>";
            WriteLine(netStream, data_append_dataLength(error), error, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n", error, w);
                // Close the writer and underlying file.
                w.Close();
            }
            */
        }

        private static void WriteLine(NetworkStream netStream, byte[] writeData,string write , SqlClient sql_client)
        {
            if (netStream.CanWrite)
            {
                //byte[] writeData = Encoding.ASCII.GetBytes(write);
                try
                {
                    XDocument xml = XDocument.Parse(write);
                    write = xml.ToString();
                    Console.WriteLine("S----------------------------------------------------------------------------");
                    Console.WriteLine("Write:\r\n" + write);
                    Console.WriteLine("E----------------------------------------------------------------------------");

                    //send method1
                    //netStream.Write(writeData, 0, writeData.Length);
                    // 需等待資料真的已寫入 NetworkStream
                    //Thread.Sleep(3000);

                    //send method2
                    IAsyncResult result = netStream.BeginWrite(writeData, 0, writeData.Length, new AsyncCallback(myWriteCallBack), netStream);
                    result.AsyncWaitHandle.WaitOne();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WriteError:\r\n" + ex.Message);
                }

                
            }
        }
        public static void myWriteCallBack(IAsyncResult ar)
        {

            NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
            myNetworkStream.EndWrite(ar);
        }

        private static string ReadLine(TcpClient tcpClient, NetworkStream netStream,
            string output)
        {
            if (netStream.CanRead)
            {
                byte[] bytes = new byte[tcpClient.ReceiveBufferSize];

                int numBytesRead = netStream.Read(bytes, 0,
                    (int)tcpClient.ReceiveBufferSize);

                byte[] bytesRead = new byte[numBytesRead];
                Array.Copy(bytes, bytesRead, numBytesRead);

                string returndata = Encoding.ASCII.GetString(bytesRead);

                output = String.Format("Read: Length: {0}, Data: \r\n{1}",
                    returndata.Length, returndata);
            }

            Console.WriteLine("-------------------------");
            Console.WriteLine("Read: " + output);
            Console.WriteLine("-------------------------");

            return output.Trim();
        }
        static void read_thread_method(TcpClient tcpClient, NetworkStream netStream , SqlClient sql_client)
        {
            Console.WriteLine("in read thread");
            while (true)
            {
                Thread.Sleep(300);
                if (netStream.CanRead && netStream.DataAvailable)
                {
                    //string xml_test = "<test></test>";
                    int receive_total_length = tcpClient.ReceiveBufferSize;
                    byte[] length = new byte[2];
                    int numBytesRead = netStream.Read(length, 0, 2);
                    int data_length = GetLittleEndianIntegerFromByteArray(length, 0);
                    byte[] data = new byte[data_length];
                    netStream.Read(data, 0, data_length);
                    string returndata = Encoding.ASCII.GetString(data);
                    string output = String.Format("Read: Length: {0}, Data: {1}", returndata.Length, returndata);
                    XDocument xml_data = XDocument.Parse(returndata);
                    string xml_root_tag = xml_data.Root.Name.ToString();
                    Console.WriteLine();
                    string ouput2 = string.Empty;
                    try
                    {
                        ouput2 = xml_data.ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ReadError:\r\n" + ex.Message);
                    }
                    Console.WriteLine("S############################################################################");
                    Console.WriteLine( "Read:\r\n"+ouput2 );
                    //Console.WriteLine("First node:[" + xml_root_tag + "]");
                    Console.WriteLine("E############################################################################");
                    xml_parse(tcpClient, netStream, xml_root_tag, xml_data, sql_client);
                    //Console.ReadLine();
                    
                    //byte[] bytes = new byte[tcpClient.ReceiveBufferSize];
                    
                    //int numBytesRead = netStream.Read(bytes, 0,
                        //(int)tcpClient.ReceiveBufferSize);
                    
                    //byte[] bytesRead = new byte[numBytesRead];
                    //Array.Copy(bytes, bytesRead, numBytesRead);
                    /*
                    Array.Copy(bytes, 0, bytesRead, 0, numBytesRead);
                    string returndata = Encoding.ASCII.GetString(bytesRead);
                    string output = String.Format("Read: Length: {0}, Data: {1}", returndata.Length, returndata);
                    Console.WriteLine("============================================================================");
                    Console.WriteLine(output);
                    Console.WriteLine("############################################################################");
                     * */

                    Console.WriteLine(
                        @"
Select 1-6 then press enter to send package
1.Immediate-Location-Request Message
2.Triggered-Location-Request for Change Cadence Message
3.Triggered-Location-Request for Change Distance Message
4.Digital-Output-Change-Request Message
5.Location-Protocol-Request Message
6.Triggered-Location-Stop-Request Message

");
                    Console.Write("Select[1-6]:");
                }
            }
        }

        private static void xml_parse(TcpClient tcpClient, NetworkStream netStream, string xml_root_tag, XDocument xml_data, SqlClient sql_client)
        {
            string log = xml_data.ToString();
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("receive:\r\n" , log, w);
                // Close the writer and underlying file.
                w.Close();
            }
            Hashtable htable = new Hashtable();
            IEnumerable<XElement> de;
            List<string> sensor_name = new List<string>();
            List<string> sensor_value = new List<string>();
            List<string> sensor_type = new List<string>();
            
            switch (xml_root_tag)
            {
                case "Triggered-Location-Report":
                case "Immediate-Location-Report":
                case "Unsolicited-Location-Report":
                    {
                        IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("suaddr").Name))
                        {
                            htable.Add("suaddr", XmlGetTagValue(xml_data, "suaddr"));
                            Console.WriteLine("suaddr:{0}", htable["suaddr"]);
                        }
                        if (elements.Contains(new XElement("event-info").Name) && xml_root_tag == "Unsolicited-Location-Report")
                        {
                            htable.Add("event_info", XmlGetTagValue(xml_data, "event-info"));
                            //Console.WriteLine("event_info:{0}", event_info);
                        }
                        if (elements.Contains(new XElement("operation-error").Name))
                        {
                             htable.Add("result_code",XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                             //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                             htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]); 
                            //Console.WriteLine("result_code:{0}", result_code);
                            //Console.WriteLine("err_msg:{0}", err_msg);
                        }
                        if (elements.Contains(new XElement("info-data").Name))
                        {
                            
                            //string shape_type = (string)(from e in xml_data.Descendants("shape") select e.Elements().First().Name.LocalName).First();
                            htable.Add("info_time",  XmlGetTagValue(xml_data, "info-time"));//info-data scope
                            htable.Add("server_time" , XmlGetTagValue(xml_data, "server-time"));//info-data scope
                            //Console.WriteLine("info_time:{0}", info_time);
                            //Console.WriteLine("server_time:{0}", server_time);
                            if (elements.Contains(new XElement("satellites-num").Name))
                            {
                                htable.Add("satellites_num" , XmlGetTagValue(xml_data, "satellites-num"));//info-data scope
                                //Console.WriteLine("satellites_num:{0}", satellites_num);
                            }
                            if (elements.Contains(new XElement("speed-hor").Name))
                                htable.Add("speed-hor" , XmlGetTagValue(xml_data, "speed-hor"));//info-data scope
                            if (elements.Contains(new XElement("direction-hor").Name))
                                htable.Add("direction-hor" , XmlGetTagValue(xml_data, "direction-hor"));//info-data scope
                            htable.Add("shape-type" , XmlGetFirstChildTagName(xml_data, "shape"));//info-data scope
                            //Console.WriteLine("shape_type :[{0}]", shape_type);
                            //Console.WriteLine("speed_hor:{0}", speed_hor);
                            //Console.WriteLine("Direction_hor:{0}", Direction_hor);
                            switch (Convert.ToString(htable["shape-type"]))//info-data scope
                            {
                                case "point-2d":
                                    {
                                        htable.Add("lat_value" , XmlGetTagValue(xml_data, "lat"));
                                        htable.Add("long_value" , XmlGetTagValue(xml_data, "long"));
                                        //Console.WriteLine("lat_value:{0}", lat_value);
                                        //Console.WriteLine("long_value:{0}", long_value);
                                    }
                                    break;
                                case "point-3d":
                                    {
                                        htable.Add("lat_value" ,XmlGetTagValue(xml_data, "lat"));
                                        htable.Add("long_value" , XmlGetTagValue(xml_data, "long")); 
                                        htable.Add("altitude_value" , XmlGetTagValue(xml_data, "altitude"));
                                        //Console.WriteLine("lat_value:{0}", lat_value);
                                        //Console.WriteLine("long_value:{0}", long_value);
                                        //Console.WriteLine("altitude_value:{0}", altitude_value);
                                    }
                                    break;
                                case "circle-2d":
                                    {
                                        htable.Add("lat_value" , XmlGetTagValue(xml_data, "lat"));
                                        htable.Add("long_value" , XmlGetTagValue(xml_data, "long")); 
                                        htable.Add("radius_value" , XmlGetTagValue(xml_data, "radius"));
                                        //Console.WriteLine("lat:[{0}] , long:[{1}] , radius:[{2}]", lat_value, long_value, radius_value);
                                    }
                                    break;
                                case "circle-3d":
                                    {
                                        htable.Add("lat_value" , XmlGetTagValue(xml_data, "lat"));
                                        htable.Add("long_value" , XmlGetTagValue(xml_data, "long")); 
                                        htable.Add("altitude_value" , XmlGetTagValue(xml_data, "altitude"));
                                        htable.Add("radius_value", XmlGetTagValue(xml_data, "radius"));
                                        //Console.WriteLine("lat_value:{0}", lat_value);
                                        //Console.WriteLine("long_value:{0}", long_value);
                                        //Console.WriteLine("altitude_value:{0}", altitude_value);
                                        //Console.WriteLine("radius_value:{0}", radius_value);
                                    }
                                    break;
                            }
                            if (elements.Contains(new XElement("sensor-info").Name))//Sensor Info
                            {
                                de = from el in xml_data.Descendants("sensor") select el;
                                sensor_name = (from e in de.Descendants("sensor-name") select (string)e).Cast<string>().ToList();
                                sensor_value = (from e in de.Descendants("sensor-value") select (string)e).Cast<string>().ToList();
                                sensor_type = (from e in de.Descendants("sensor-type") select (string)e).Cast<string>().ToList();
                                int i = 0;
                                foreach (string e in sensor_name)
                                {
                                    Console.WriteLine("sensor_name"+ i++ +":{0}", e);
                                }
                                i = 0;
                                foreach (string e in sensor_value)
                                {
                                    Console.WriteLine("sensor_value" + i++ + ":{0}", e);
                                }
                                i = 0;
                                foreach (string e in sensor_type)
                                {
                                    Console.WriteLine("sensor_type" + i++ + ":{0}", e);
                                }
                            }
                            if (elements.Contains(new XElement("vehicle-info").Name))//Vehicle Info
                            {
                                htable.Add("Odometer" , XmlGetTagValue(xml_data, "odometer"));
                                //Console.WriteLine("Odometer:{0}", Odometer);
                            }
                        }
                        if (bool.Parse(ConfigurationManager.AppSettings["SQL_ACCESS"]))
                            access_sql_server(sql_client, xml_root_tag, htable, sensor_name, sensor_type, sensor_value, XmlGetAllElementsXname(xml_data),log);   
                    }
                     
                    break;

                
                case "Location-Protocol-Report":
                    {
                        IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("request-id").Name))
                        {
                            htable.Add("request_id" , XmlGetTagValue(xml_data, "request-id"));
                            //Console.WriteLine("request_id:{0}", request_id);
                        }
                        if (elements.Contains(new XElement("protocol-version").Name))
                        {
                            htable.Add("protocol_version" , XmlGetTagValue(xml_data, "protocol-version"));
                            //Console.WriteLine("protocol_version:{0}", protocol_version);
                        }
                        if (elements.Contains(new XElement("result").Name))
                        {
                            htable.Add("result_code", XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                            //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                            htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]); 
                            /*
                            string err_msg = string.Empty;
                            string result_code = XmlGetTagAttributeValue(xml_data, "result", "result-code");
                            Console.WriteLine("result_code:{0}", result_code);
                            if (!result_code.Equals("0"))
                            {
                                //err_msg = (string)(from el in xml_data.Descendants("result") select el).First();
                                err_msg = XmlGetTagValue(xml_data, "result");
                                string err_msg1 = ConfigurationManager.AppSettings["RESULT_CODE_" + result_code]; 
                                Console.WriteLine("err_msg:{0}", err_msg);
                            }
                             * */
                        }

                    }
                    break;
                case "Location-Registration-Answer":
                    {
                        IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        
                        //string app_id = (string)(from e1 in xml_data.Descendants("application") select e1.Attribute("application-id").Value).First();
                        //string result_code = (string)(from e1 in xml_data.Descendants("result") select e1.Attribute("result-code").Value).First();
                        if (elements.Contains(new XElement("application").Name))
                        {
                            htable.Add("app_id" , XmlGetTagAttributeValue(xml_data, "application", "application-id"));
                            //Console.WriteLine("app_id:{0}", app_id);
                        }
                        if (elements.Contains(new XElement("result").Name))
                        {
                            htable.Add("result_code", XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                            //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                            htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]);
                            /*
                            string err_msg = string.Empty;
                            string result_code = XmlGetTagAttributeValue(xml_data, "result", "result-code");
                            Console.WriteLine("result_code:{0}", result_code);
                            if (!result_code.Equals("0"))
                            {
                                //err_msg = (string)(from el in xml_data.Descendants("result") select el).First();
                                err_msg = XmlGetTagValue(xml_data, "result");
                                string err_msg1 = ConfigurationManager.AppSettings["RESULT_CODE_" + result_code]; 
                                Console.WriteLine("err_msg:{0}", err_msg);
                            }
                             * */
                        }
                    }
                    break;
                case "Immediate-Location-Answer":
                case "Triggered-Location-Stop-Answer":
                case "Digital-Output-Answer":
                case "Triggered-Location-Answer":
                    {
                        IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("suaddr").Name))
                        {
                            htable.Add( "suaddr" , XmlGetTagValue(xml_data, "suaddr"));
                            //Console.WriteLine("suaddr:{0}", suaddr);
                        }
                        if (elements.Contains(new XElement("request-id").Name))
                        {
                            htable.Add( "request_id" , XmlGetTagValue(xml_data, "request-id"));
                            //Console.WriteLine("request_id:{0}", request_id);
                        }
                        if (elements.Contains(new XElement("result").Name))
                        {
                            htable.Add("result_code", XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                            //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                            htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]);
                            /*
                            string err_msg = string.Empty;
                            string result_code = XmlGetTagAttributeValue(xml_data, "result", "result-code");
                            Console.WriteLine("result_code:{0}", result_code);
                            if (!result_code.Equals("0"))
                            {
                                //err_msg = (string)(from el in xml_data.Descendants("result") select el).First();
                                err_msg = XmlGetTagValue(xml_data, "result");
                                string err_msg1 = ConfigurationManager.AppSettings["RESULT_CODE_" + result_code]; 
                                Console.WriteLine("err_msg:{0}", err_msg);
                            }
                             * */
                        }
                    }
                    break;
                case "Triggered-Location-Device-Type-Report ":
                    {
                        IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("suaddr").Name))
                        {
                            htable.Add("suaddr", XmlGetTagValue(xml_data, "suaddr"));
                            //Console.WriteLine("suaddr:{0}", suaddr);
                        }
                        if (elements.Contains(new XElement("operation-error").Name))
                        {
                            htable.Add("result_code", XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                            //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                            htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]);
                            //Console.WriteLine("result_code:{0}", result_code);
                            //Console.WriteLine("err_msg:{0}", err_msg);
                        }
                    }
                    break;
                default:
                    //error occur
                    Console.WriteLine("ERROR:" + log);
                    break;
            }
        }
        public struct SQL_DATA
        {
            
            public string _id;
            public string _uid;
            public string _status;//max 2 length
            public string _time;
            public string _validity;
            public string _lat;
            public string _lon;
            public string _speed;
            public string _course;
            public string _distance;
            //
            public string _or_lon;
            public string _or_lat;
            public string _satellites;
            public string _temperature;
            public string _voltage;
            //
            public string j_5;//radius
            public string j_6;//emergency on/off
            public string j_7;//present/absent
            public string j_8;//Ignition on/Off
            public string _option0;//info-time
            public string _option1;//server-time
            public string _option2;//result-code
            public string _option3;//result_msg , event-info

        }
        enum device_status
        {
            MV,TK,EM,PE,UL
        }
        private static void access_sql_server(SqlClient sql_client, string xml_root_tag, Hashtable htable, List<string> sensor_name, List<string> sensor_type, List<string> sensor_value, IEnumerable<XName> elements,string log)
        {
            DateTime dt = DateTime.Now;
            SQL_DATA gps_log = new SQL_DATA();
            gps_log._or_lat = gps_log._or_lon = gps_log._satellites = gps_log._temperature = gps_log._voltage = "0";
            string now = string.Format("{0:yyyyMMdd}", dt);
            gps_log._time = "\'"+string.Format("{0:yyyyMMdd hh:mm:ss.fff}", dt)+"+08"+"\'";

            sql_client.connect();
            string id_serial_command = sql_client.get_DataTable("SELECT COUNT(_uid)   FROM public._gps_log").Rows[0].ItemArray[0].ToString();
            sql_client.disconnect();

            if (htable.ContainsKey("app_id"))
            {
 
            }
            if (htable.ContainsKey("suaddr"))
            {
                gps_log._uid = "\'" + htable["suaddr"].ToString() + "\'";
                gps_log._id = "\'" + htable["suaddr"].ToString() + "_" + now + "_" + id_serial_command + "\'";
            }
            else
            {
                gps_log._uid = "\'" + "null" + "\'";
                gps_log._id = "\'" + Convert.ToBase64String(System.Guid.NewGuid().ToByteArray())  + "_" + id_serial_command + "\'";
            }
            if (htable.ContainsKey("result_code"))
            {
                gps_log._option2 = "\'"+htable["result_code"].ToString()+"\'";
                gps_log._option3 = "\'" + ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"].ToString()] + "\'";
            }
            //if (htable.ContainsKey("result_msg"))
            //{
            //    gps_log._option3 = "\'"+htable["result_msg"].ToString()+"\'";
            //}
            if (htable.ContainsKey("event_info"))
            {
                gps_log._option3 = "\'"+htable["event_info"].ToString()+"\'";

                switch (htable["event_info"].ToString())
                {
                    case "Emergency On":
                    case "Emergency Off":
                        gps_log.j_6 = "\'" + htable["event_info"].ToString() + "\'";
                        gps_log.j_7 = "\'" + "null" + "\'";
                        gps_log.j_8 = "\'" + "null" + "\'";
                        break;
                    case "Unit Present":
                    case "Unit Absent":
                        gps_log.j_7 = "\'" + htable["event_info"].ToString() + "\'";
                        gps_log.j_6 = "\'" + "null" + "\'";
                        gps_log.j_8 = "\'" + "null" + "\'";
                        break;
                    case "Ignition Off":
                    case "Ignition On":
                        gps_log.j_8 = "\'" + htable["event_info"].ToString() + "\'";
                        gps_log.j_6 = "\'" + "null" + "\'";
                        gps_log.j_7 = "\'" + "null" + "\'";
                        break;

                }
            }
            if (htable.ContainsKey("lat_value"))
            {
                gps_log._lat = htable["lat_value"].ToString();
            }
            else
                gps_log._lat = "0";
            if (htable.ContainsKey("long_value"))
            {
                gps_log._lon = htable["long_value"].ToString();
            }
            else
                gps_log._lon = "0";
            if (htable.ContainsKey("radius_value"))
            {
                gps_log.j_5 = htable["radius_value"].ToString();
            }
            else
                gps_log.j_5 = "0";
            if (htable.ContainsKey("speed-hor"))
            {
                gps_log._speed = htable["speed-hor"].ToString();
            }
            else
                gps_log._speed = "0";
            if (htable.ContainsKey("direction-hor"))
            {
                gps_log._course = htable["direction-hor"].ToString();
            }
            else
                gps_log._course = "0";
            if (htable.ContainsKey("Odometer"))
            {
                gps_log._distance = htable["Odometer"].ToString().Replace(",",".");
            }
            if (htable.ContainsKey("info_time"))
            {
                gps_log._option0 = "\'"+htable["info_time"].ToString()+"\'";
            }
            else
                gps_log._option0 = "\'" + "null" + "\'";
            if (htable.ContainsKey("server_time"))
            {
                gps_log._option1 = "\'"+"0"+"\'";
            }
            else
                gps_log._option1 = "\'" + "0" + "\'";
            if (sql_client.connect())
            {
                
                try
                {
                    string cmd = string.Empty;
                    string table_columns = string.Empty;
                    string table_column_value = string.Empty;
                    switch (xml_root_tag)
                    {
                        case "Triggered-Location-Report":
                            if (elements.Contains(new XElement("operation-error").Name))
                            {
                                table_columns = "_id,_uid,_option2,_option3,_or_lon,_or_lat,_satellites,_temperature,_voltage";
                                table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._option2 + "," + gps_log._option3+","+
                                    gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                               gps_log._temperature + "," + gps_log._voltage;
                                cmd = "INSERT INTO public._gps_log ("+table_columns+") VALUES (" + table_column_value  + ")";
                            }
                            else
                            {
                                if (elements.Contains(new XElement("vehicle-info").Name))
                                {
                                    gps_log._status = ((int)device_status.MV).ToString();
                                    gps_log._validity = "\'Y\'";
                                    table_columns = "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,_distance,j_5,_option0,_option1," +
                                                    "_or_lon,_or_lat,_satellites,_temperature,_voltage";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," + gps_log._time +
                                               "," + gps_log._validity + "," + gps_log._lat + "," + gps_log._lon + "," + gps_log._speed +
                                               "," + gps_log._course + "," + gps_log._distance + "," + gps_log.j_5 + "," + gps_log._option0 +
                                               "," + gps_log._option1 + "," +
                                               gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                               gps_log._temperature + "," + gps_log._voltage;
                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                                }
                                else
                                {
                                    gps_log._status = ((int)device_status.MV).ToString();
                                    gps_log._validity = "\'Y\'";
                                    table_columns = "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,j_5,_option0,_option1," +
                                                    "_or_lon,_or_lat,_satellites,_temperature,_voltage";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," + gps_log._time +
                                               "," + gps_log._validity + "," + gps_log._lat + "," + gps_log._lon + "," + gps_log._speed +
                                               "," + gps_log._course  + "," + gps_log.j_5 + "," + gps_log._option0 +
                                               "," + gps_log._option1 + "," +
                                               gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                               gps_log._temperature + "," + gps_log._voltage;
                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                                }
                                       
                                /*
                                if (elements.Contains(new XElement("sensor-info").Name) && elements.Contains(new XElement("vehicle-info").Name))
                                {

                                }
                                else
                                {
                                    if (!elements.Contains(new XElement("sensor-info").Name) && elements.Contains(new XElement("vehicle-info").Name))
                                    {

                                    }
                                    else
                                    {
                                        if (elements.Contains(new XElement("sensor-info").Name) && !elements.Contains(new XElement("vehicle-info").Name))
                                        {

                                        }
                                        else
                                        {
                                            if (!elements.Contains(new XElement("sensor-info").Name) && !elements.Contains(new XElement("vehicle-info").Name))
                                            {

                                            }
                                        }
                                    }
                                }
                                */
                            }
                            break;
                        case "Unsolicited-Location-Report":
                            {
                                gps_log._status = ((int)device_status.UL).ToString();
                                if (xml_validation_with_dtd(log, xml_root_tag))
                                {
                                    gps_log._validity = "\'Y\'";
                                    table_columns = "_id,_uid,_status,_validity,_or_lon,_or_lat,_satellites,_temperature,,_voltage,_option3,j_6,j_7";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," +
                                                         gps_log._validity + "," +
                                               gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                               gps_log._temperature + "," + gps_log._voltage + "," + gps_log._option3+","+gps_log.j_6+","+gps_log.j_7;
                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                                }
                                else
                                {
                                    if (elements.Contains(new XElement("operation-error").Name))
                                    {
                                        table_columns = "_id,_uid,_option2,_option3,_or_lon,_or_lat,_satellites,_temperature,_voltage";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._option2 + "," + gps_log._option3+","+
                                        gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                                   gps_log._temperature + "," + gps_log._voltage;
                                    cmd = "INSERT INTO public._gps_log ("+table_columns+") VALUES (" + table_column_value  + ")";
                                   
                                    }
                                    else
                                    {
                                        if (elements.Contains(new XElement("vehicle-info").Name))
                                        {
                                            gps_log._status = ((int)device_status.MV).ToString();
                                            gps_log._validity = "\'Y\'";
                                            table_columns = "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,_distance,j_5,_option0,_option1," +
                                                            "_or_lon,_or_lat,_satellites,_temperature,_voltage,_option3,j_6,j_7";
                                            table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," + gps_log._time +
                                                       "," + gps_log._validity + "," + gps_log._lat + "," + gps_log._lon + "," + gps_log._speed +
                                                       "," + gps_log._course + "," + gps_log._distance + "," + gps_log.j_5 + "," + gps_log._option0 +
                                                       "," + gps_log._option1 + "," +
                                                       gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                                       gps_log._temperature + "," + gps_log._voltage + "," + gps_log._option3 + "," + gps_log.j_6 + "," + gps_log.j_7;
                                            //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                            cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                                        }
                                        else
                                        {
                                            gps_log._status = ((int)device_status.MV).ToString();
                                            gps_log._validity = "\'Y\'";
                                            table_columns = "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,j_5,_option0,_option1," +
                                                            "_or_lon,_or_lat,_satellites,_temperature,_voltage,_option3,j_6,j_7";
                                            table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," + gps_log._time +
                                                       "," + gps_log._validity + "," + gps_log._lat + "," + gps_log._lon + "," + gps_log._speed +
                                                       "," + gps_log._course + "," + gps_log.j_5 + "," + gps_log._option0 +
                                                       "," + gps_log._option1 + "," +
                                                       gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites + "," +
                                                       gps_log._temperature + "," + gps_log._voltage + "," + gps_log._option3 + "," + gps_log.j_6 + "," + gps_log.j_7;
                                            //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                            cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                                        }
                                    }
                                }
                               
                        }
                        break;
                        case "Location-Registration-Answer":
                        {
 
                        }
                        break;
                    }
                    sql_client.modify(cmd);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    sql_client.disconnect();
                }
            }
        }
        /*
             * <result result-code="A">SYNTAX ERROR</result>
         * *****************************************************
             * XmlGetTagAttributeValue(xml_data, "result", "result-code");
             * return : A
         * *****************************************************
         *     XmlGetTagValue(xml_data, "result")
         *     return : SYNTAX ERROR
             * */
        static string XmlGetTagAttributeValue(XDocument xml_data, string tag_name, string tag_attribute_name)
        {
            string result = string.Empty;
            try
            {
                result = (string)(from e1 in xml_data.Descendants(tag_name) select e1.Attribute(tag_attribute_name).Value).First();
            }
            catch (Exception e)
            {
                Console.WriteLine("XmlGetTagAttributeValue:"+tag_name + ":" + tag_attribute_name+":"+e.Message);
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
                Console.WriteLine("XmlGetTagValue:"+tag_name+":"+e.Message);
                result = "error";
            }

                return result;

        }
        /*
         * <a><b></b><c></c></a>
         * *****************************************************
         * XmlGetFirstChildTagName(xml_data,"a")
         * return : b
         */
        static string XmlGetFirstChildTagName(XDocument xml_data, string parent_tag_name)
        {
            string result=string.Empty;
            try
            {
                result = (string)(from e in xml_data.Descendants(parent_tag_name) select e.Elements().First().Name.LocalName).First();
            }
            catch (Exception e)
            {
                Console.WriteLine("XmlGetFirstChildTagName:"+parent_tag_name+":"+e.Message);
                result = "error";
            }

                return result;

        }
        static IEnumerable<XName> XmlGetAllElementsXname(XDocument xml_data)
        {
            return (from e1 in xml_data.DescendantNodes().OfType<XElement>() select e1).Select(x => x.Name).Distinct();
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
        static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public static void Log(string send_receive, String logMessage, TextWriter w)
        {
            try
            {
                XDocument xml = XDocument.Parse(logMessage);
                logMessage = send_receive + xml.ToString();
                w.Write("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToString("H:mm:ss.fffffff"),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("  :");
                w.WriteLine("  :{0}", logMessage);
                w.WriteLine("-------------------------------");
                // Update the underlying file.
                w.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
        // Display any validation errors. 
        private static void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            isValid = false;
            Console.WriteLine("Validation Error: {0}", e.Message);
        }

    }

}
