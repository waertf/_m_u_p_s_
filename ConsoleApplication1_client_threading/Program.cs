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
using log4net;
using log4net.Config;
using keeplive;

namespace ConsoleApplication1_client_threading
{
    class Program
    {
        //static TcpClient tcpClient = null;
        //static NetworkStream netStream = null;
        const int LENGTH_TO_CUT = 4;
        private static bool isValid = true;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static AutoResetEvent autoEvent = new AutoResetEvent(false);
        private static byte[] myReadBuffer = null;
        private static byte[] fBuffer = null;
        private static int fBytesRead = 0;
        private static TcpClient tcpClient;
        private static SqlClient sql_client;
        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public  struct AVLS_UNIT_Report_Packet
        {
            public string ID;
            public string GPS_Valid;//only L or A (L->valid , A->not valid)
            /*
             * The format is: 
            Year month day hour minute second 
            For example, the string 040315131415 means: 
            The date is “Year 2004, March, Day 15 “ 
            and the time is “13:14:15 ” 
             * */
            public string Date_Time;
            /*
             * For example, the string N2446.5321E12120.4231 means
“North 24 degrees 46.5321 minutes 
“East 121 degrees 20.4231 minutes” 
 Or S2446.5281W01234.5678 means 
“South 24 degrees 46. 5281 minutes” = “South 24.7755 degrees” = “South 24 
degrees 46 minutes 31.69 seconds” 
“West 12 degrees 34.5678 minutes” = “West 12.57613 degrees” = “West 12 
degrees 34 minutes 34.07 seconds”
             * */
            public string Loc;
            /*
             * from 0 to 999
             * km/hr
             */
            public string Speed;
            /*
             the GPS direction in degrees. And this value is between 0 and 359 
degree. (no decimal)*/
            public string Dir;
            /*
             * The string provides the temperature in the format UNIT-sign-degrees. (Non-fixed 
length 0 to 999 and no decimal. 
For example, 
the string F103 means “103 degree Fahrenheit” 
the string C-12 means “-12 degree Celsius” 
If the UNIT does not include a Temperature sensor, it will report ’NA’.
             */
            public string Temp;
            /*
             We use eight ASCII characters to represent 32 bit binary number. Each bit represents 
a flag in the UNITs status register. The status string will be represented in HEX for 
each set of the byte. To display a four-byte string, there will be 8 digits string */
            public string Status;//17
            public string Event;//150
            public string Message;
        }
       
        static void Main(string[] args)
        {
            //string ipAddress = "127.0.0.1";
            string ipAddress = ConfigurationManager.AppSettings["MUPS_SERVER_IP"];
            //int port = 23;
            int port = int.Parse(ConfigurationManager.AppSettings["MUPS_SERVER_PORT"]);
            bool mups_connected = false;
            tcpClient = new TcpClient();
            while (!mups_connected)
            {
                try
                {
                    tcpClient.Connect(ipAddress, port);
                    mups_connected = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    log.Error(ex.Message);
                }
            }

            tcpClient.NoDelay = false;

            Keeplive.keep(tcpClient.Client);
            NetworkStream netStream = tcpClient.GetStream();

            sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);

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
        public static void avls_myWriteCallBack(IAsyncResult ar)
        {

            NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
            myNetworkStream.EndWrite(ar);
            sendDone.Set();

        }
        private static void ReadLine(TcpClient tcpClient, NetworkStream netStream,int prefix_length)
        {
            try
            {
                if (netStream.CanRead)
                {
                    //byte[] bytes = new byte[tcpClient.ReceiveBufferSize];

                    myReadBuffer = new byte[prefix_length];
                    netStream.BeginRead(myReadBuffer, 0, myReadBuffer.Length,
                                                                 new AsyncCallback(myReadSizeCallBack),
                                                                 netStream);

                    autoEvent.WaitOne();
                    /*
                    int numBytesRead = netStream.Read(bytes, 0,
                        (int)tcpClient.ReceiveBufferSize);

                    byte[] bytesRead = new byte[numBytesRead];
                    Array.Copy(bytes, bytesRead, numBytesRead);

                    string returndata = Encoding.ASCII.GetString(bytesRead);

                    output = String.Format("Read: Length: {0}, Data: \r\n{1}",
                        returndata.Length, returndata);
                     * */
                }

                //Console.WriteLine("-------------------------");
                //Console.WriteLine("Read: " + output);
                //Console.WriteLine("-------------------------");

                //return output.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void myReadSizeCallBack(IAsyncResult ar)
        {          
            try
            {
                NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
                // Read precisely four bytes for the length of the following message
                int numberOfBytesRead = myNetworkStream.EndRead(ar);
                if (myReadBuffer.Length != numberOfBytesRead)
                    throw new Exception();
                int data_length = GetLittleEndianIntegerFromByteArray(myReadBuffer, 0);
                //Array.Reverse(myReadBuffer);
                //int size = BitConverter.ToInt16(myReadBuffer, 0);

                // Create a buffer to hold the message and start reading it.
                fBytesRead = 0;
                fBuffer = new byte[data_length];
                myNetworkStream.BeginRead(fBuffer, 0, fBuffer.Length, FinishRead, myNetworkStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static void FinishRead(IAsyncResult result)
        {
            try
            {
                // Finish reading from our stream. 0 bytes read means stream was closed
                NetworkStream fStream = (NetworkStream)result.AsyncState;
                int read = fStream.EndRead(result);
                if (0 == read)
                    throw new Exception();

                // Increment the number of bytes we've read. If there's still more to get, get them
                fBytesRead += read;
                if (fBytesRead < fBuffer.Length)
                {
                    fStream.BeginRead(fBuffer, fBytesRead, fBuffer.Length - fBytesRead, FinishRead, null);
                    return;
                }

                // Should be exactly the right number read now.
                if (fBytesRead != fBuffer.Length)
                    throw new Exception();

                // Handle the message and go get the next one.
                string returndata = Encoding.ASCII.GetString(fBuffer);
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
                Console.WriteLine("Read:\r\n" + ouput2);
                //Console.WriteLine("First node:[" + xml_root_tag + "]");
                Console.WriteLine("E############################################################################");
                xml_parse(tcpClient, fStream, xml_root_tag, xml_data, sql_client);
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
                //OnMessageRead(fBuffer);
                fStream.BeginRead(myReadBuffer, 0, myReadBuffer.Length,
                                                                 new AsyncCallback(myReadSizeCallBack),
                                                                 fStream);
               // fStream.BeginRead(fSizeBuffer, 0, fSizeBuffer.Length, FinishReadSize, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static void read_thread_method(TcpClient tcpClient, NetworkStream netStream , SqlClient sql_client)
        {
            Console.WriteLine("in read thread");
            //asyn read
            ReadLine(tcpClient, netStream, 2);
            //syn read
            //while (true)
            {
                //Thread.Sleep(300);
                //if (netStream.CanRead)// && netStream.DataAvailable)
                {
                    //string xml_test = "<test></test>";
                    //int receive_total_length = tcpClient.ReceiveBufferSize;
                    //byte[] length = new byte[2];
                    //int numBytesRead = netStream.Read(length, 0, 2);
                    //int data_length = GetLittleEndianIntegerFromByteArray(length, 0);
                    //byte[] data = new byte[data_length];
                    //netStream.Read(data, 0, data_length);
                    //string returndata = Encoding.ASCII.GetString(data);
                    
                    //Console.WriteLine("out ReadLine");
                    /*
                    string returndata = Encoding.ASCII.GetString(fBuffer);
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
                    */
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
                }
            }
            Console.WriteLine("out read thread");
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
                            Console.WriteLine("event_info:{0}", htable["event_info"]);
                        }
                        if (elements.Contains(new XElement("operation-error").Name))
                        {
                             htable.Add("result_code",XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                             //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                             htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]);
                             Console.WriteLine("result_code:{0}", htable["result_code"]);
                             Console.WriteLine("result_msg:{0}", htable["result_msg"]);
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
                        if (bool.Parse(ConfigurationManager.AppSettings["AVLS_ACCESS"]))
                            access_avls_server(sql_client,xml_root_tag, htable, sensor_name, sensor_type, sensor_value, XmlGetAllElementsXname(xml_data), log);
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
            
            Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                foreach (DictionaryEntry ht in htable)
                {
                    Console.WriteLine("Key = {0}, Value = {1}" + Environment.NewLine, ht.Key, ht.Value);
                    Log("receive:\r\n", ht.Key+"="+ht.Value, w);
                    // Close the writer and underlying file.
                   

                }
                w.Close();
            }
            
            Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
        }

        private static void access_avls_server(SqlClient sql_client,string xml_root_tag, Hashtable htable, List<string> sensor_name, List<string> sensor_type, List<string> sensor_value, IEnumerable<XName> iEnumerable, string log)
        {
            TcpClient avls_tcpClient;
            string send_string = string.Empty;
            AVLS_UNIT_Report_Packet avls_package = new AVLS_UNIT_Report_Packet();
            //string ipAddress = "127.0.0.1";
            string ipAddress = ConfigurationManager.AppSettings["AVLS_SERVER_IP"];
            //int port = 23;
            int port = int.Parse(ConfigurationManager.AppSettings["AVLS_SERVER_PORT"]);
            
            avls_tcpClient = new TcpClient();

            avls_tcpClient.Connect(ipAddress, port);

            avls_tcpClient.NoDelay = false;

            //Keeplive.keep(avls_tcpClient.Client);
            NetworkStream netStream = avls_tcpClient.GetStream();
            if (iEnumerable.Contains(new XElement("operation-error").Name))
            {
                avls_tcpClient.Close();
                return;
            }
            else
            {
                
                if (htable.ContainsKey("suaddr"))
                {
                    avls_package.ID = htable["suaddr"].ToString() + ",";
                }
                if (htable.ContainsKey("result_code"))
                {
                    if (htable["result_code"].ToString().Equals("1006"))
                    {
                        avls_package.GPS_Valid = "L,";
                    }
                    else
                        avls_package.GPS_Valid = "A,";
                }
                else
                    avls_package.GPS_Valid = "A,";
                if (htable.ContainsKey("info_time"))
                {
                    avls_package.Date_Time = htable["info_time"].ToString().Substring(2) + ",";
                }
                if (htable.ContainsKey("lat_value") && htable.ContainsKey("long_value"))
                {
                    avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                }
                else
                    return;
                if (htable.ContainsKey("speed-hor"))
                {
                    avls_package.Speed = Convert.ToInt32((double.Parse(htable["speed-hor"].ToString()) * 1.609344)).ToString() + ",";
                }
                if (htable.ContainsKey("direction-hor"))
                {
                    avls_package.Dir = htable["direction-hor"].ToString() + ",";
                }
                avls_package.Temp = "NA,";
                if (htable.ContainsKey("event_info"))
                {


                    switch (htable["event_info"].ToString())
                    {
                        case "Emergency On":
                            avls_package.Event = "150,";
                            break;
                        case "Emergency Off":
                            avls_package.Event = "110,";
                            break;
                        case "Unit Present":
                        case "Unit Absent":
                            break;
                        case "Ignition Off":
                            avls_package.Status = "00000000,";
                            break;
                        case "Ignition On":
                            avls_package.Status = "00020000,";
                            break;

                    }
                }
                else
                {
                    avls_package.Event = "110,";
                    avls_package.Status = "00000000,";
                }
                avls_package.Message = "test";
            }
                
                
            send_string = "%%"+avls_package.ID+avls_package.GPS_Valid+avls_package.Date_Time+avls_package.Loc+avls_package.Speed+avls_package.Dir+avls_package.Temp+avls_package.Status+avls_package.Event+avls_package.Message+"\r\n";
            //netStream.Write(System.Text.Encoding.Default.GetBytes(send_string), 0, send_string.Length);
            avls_WriteLine(netStream, System.Text.Encoding.Default.GetBytes(send_string), send_string, sql_client);
            sendDone.WaitOne();

            //ReadLine(avls_tcpClient, netStream, send_string.Length);
            avls_tcpClient.Close();
        }

        private static void avls_WriteLine(NetworkStream netStream, byte[] writeData, string write, SqlClient sql_client)
        {
            if (netStream.CanWrite)
            {
                //byte[] writeData = Encoding.ASCII.GetBytes(write);
                try
                {
                    
                    Console.WriteLine("S----------------------------------------------------------------------------");
                    Console.WriteLine("Write:\r\n" + write);
                    Console.WriteLine("E----------------------------------------------------------------------------");

                    //send method1
                    //netStream.Write(writeData, 0, writeData.Length);
                    // 需等待資料真的已寫入 NetworkStream
                    //Thread.Sleep(3000);

                    //send method2
                    IAsyncResult result = netStream.BeginWrite(writeData, 0, writeData.Length, new AsyncCallback(avls_myWriteCallBack), netStream);
                    result.AsyncWaitHandle.WaitOne();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WriteError:\r\n" + ex.Message);
                }


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
            public string _altitude;
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

            if (htable.ContainsKey("altitude_value"))
            {
                gps_log._altitude = htable["altitude_value"].ToString();
            }
            else
                gps_log._altitude = "0";
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

            //insert into custom.cga_event_log
            /*
            sql_client.connect();
            double id_count = Convert.ToDouble(sql_client.get_DataTable("SELECT COUNT(uid)   FROM custom.cga_event_log").Rows[0].ItemArray[0]);
            sql_client.disconnect();

            if(sql_client.connect())
            {
                try
                {
                    if (xml_root_tag == "Unsolicited-Location-Report" && htable.ContainsKey("event_info"))
                    {
                        string sn = "\'" + gps_log._uid + now + id_count.ToString("D12") + "\'";
                        string table_columns = "serial_no ,uid ,status ,lat ,lon,altitude ,speed ,course ,radius ,info_time ,server_time ";
                        string table_column_value = sn + "," + gps_log._uid + "," + gps_log._option3 + "," + gps_log._lat + "," + gps_log._lon + "," +
                            gps_log._altitude + "," + gps_log._speed + "," + gps_log._course + "," + gps_log.j_5 + "," + gps_log._option0+","+gps_log._option1;
                        string cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                        sql_client.modify(cmd);
                    }


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
            */
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
                result = "";
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
                result = "";
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
                result = "";
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
                //Console.WriteLine(ex.Message);
                log.Info(logMessage);
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
