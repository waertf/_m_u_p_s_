using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Collections.Specialized;
using System.Timers;
using System.Xml.Linq;
using System.IO;
using Devart.Data.PostgreSql;
using System.Collections;
using System.Xml;        // for XmlTextReader and XmlValidatingReader
using System.Xml.Schema; // for XmlSchemaCollection (which is used later)
using log4net;
using log4net.Config;
using keeplive;
using System.Net;
using Gurock.SmartInspect;
using Configuration = System.Configuration.Configuration;

namespace ConsoleApplication1_client_threading
{
    public static class ExceptionHelper
    {
        public static int LineNumber(this Exception e)
        {

            int linenum = 0;
            try
            {
                linenum = Convert.ToInt32(e.StackTrace.Substring(e.StackTrace.LastIndexOf(":line") + 5));
            }
            catch
            {
                //Stack trace is not available!
            }
            return linenum;
        }
    }
    public static class GeoCodeCalc
    {
        public const double EarthRadiusInMiles = 3956.0;
        public const double EarthRadiusInKilometers = 6367.0;
        public static double ToRadian(double val) { return val * (Math.PI / 180); }
        public static double DiffRadian(double val1, double val2) { return ToRadian(val2) - ToRadian(val1); }
        /// <summary> 
        /// Calculate the distance between two geocodes. Defaults to using Miles. 
        /// </summary> 
        public static double CalcDistance(double lat1, double lng1, double lat2, double lng2)
        {
            return CalcDistance(lat1, lng1, lat2, lng2, GeoCodeCalcMeasurement.Miles);
        }
        /// <summary> 
        /// Calculate the distance between two geocodes. 
        /// </summary> 
        public static double CalcDistance(double lat1, double lng1, double lat2, double lng2, GeoCodeCalcMeasurement m)
        {
            double radius = GeoCodeCalc.EarthRadiusInMiles;
            if (m == GeoCodeCalcMeasurement.Kilometers) { radius = GeoCodeCalc.EarthRadiusInKilometers; }
            return radius * 2 * Math.Asin(Math.Min(1, Math.Sqrt((Math.Pow(Math.Sin((DiffRadian(lat1, lat2)) / 2.0), 2.0) + Math.Cos(ToRadian(lat1)) * Math.Cos(ToRadian(lat2)) * Math.Pow(Math.Sin((DiffRadian(lng1, lng2)) / 2.0), 2.0)))));
        }
    }
    public enum GeoCodeCalcMeasurement : int
    {
        Miles = 0,
        Kilometers = 1
    }
    class Program
    {
        //static TcpClient unsTcpClient = null;
        //static NetworkStream netStream = null;
        const int LENGTH_TO_CUT = 4;
        private static bool isValid = true;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //static AutoResetEvent autoEvent = new AutoResetEvent(false);
        private static byte[] myReadBuffer = new byte[2];
        private static byte[] fBuffer = null;
        private static int fBytesRead = 0;
        private volatile static TcpClient unsTcpClient, avlsTcpClient;
        private volatile static NetworkStream avlsNetworkStream, unsNetworkStream;

        [ThreadStatic]
        private static string avlsSendPackage;

        private volatile static byte[] unsSendPackage = null;
        private volatile static string unsUnsTcpWriteLineWriteParame = string.Empty;
        //private static SqlClient sql_client;
        // ManualResetEvent instances signal completion.
        private static ManualResetEvent unsConnectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent unsSendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent unsReceiveDone =
            new ManualResetEvent(false);
        private static ManualResetEvent avlsConnectDone =
           new ManualResetEvent(false);
        private static ManualResetEvent avlsSendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent avlsReceiveDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sqlAccessEvent =
            new ManualResetEvent(false);

        private static object readRecoveryLock = new object();
        private static object getGidAndFullnameLock = new object();
        private static object gpsLogAccessSqlLock = new object();
        private static object cgaEventAccessSqlLock = new object();
        private static object access_uns_deivce_power_status_Lock = new object();

        static string  last_avls_lon = string.Empty,last_avls_lat =string.Empty;

        private static string sectionName = "appSettings";

        //string ipAddress = "127.0.0.1";
        static readonly string ipAddress = ConfigurationManager.AppSettings["MUPS_SERVER_IP"];
        //int port = 23;
        static readonly int port = int.Parse(ConfigurationManager.AppSettings["MUPS_SERVER_PORT"]);
        //bool mups_connected = false;

        //string ipAddress = "127.0.0.1";
        static readonly string avls_ipaddress = ConfigurationManager.AppSettings["AVLS_SERVER_IP"];
        //int port = 23;
        static readonly int avls_port = int.Parse(ConfigurationManager.AppSettings["AVLS_SERVER_PORT"]);

        public class Device_power_status
        {
            public string ID { get; set; }
            public string SN { get; set; }
            public string power_on_time { get; set; }
            public string power_off_time { get; set; }

        }

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

        static void ConnectCallback(IAsyncResult ar)
        {
            TcpClient t = (TcpClient)ar.AsyncState;
            try
            {
                
                if (t != null && t.Client != null)
                {
                    t.EndConnect(ar);
                    unsConnectDone.Set();
                }
                
                
            }
            catch (Exception ex)
            {
                //unsTcpClient.GetStream().Close();
                //unsTcpClient.Close();
                Console.WriteLine(ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name +"_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error(ex);
                if(unsNetworkStream!=null)
                    unsNetworkStream.Close();
                if (unsTcpClient != null)
                {
                    unsTcpClient.Close();
                        
                }
                unsTcpClient = new TcpClient();
                
                unsConnectDone.Reset();
                unsTcpClient.BeginConnect(ipAddress, port, new AsyncCallback(ConnectCallback), unsTcpClient);
                unsConnectDone.WaitOne();
                Keeplive.keep(unsTcpClient.Client);
            }
            
        }
        static void AvlsConnectCallback(IAsyncResult ar)
        {
            try
            {

                TcpClient t = (TcpClient)ar.AsyncState;
                if (t != null && t.Client != null)
                {
                    t.EndConnect(ar);
                    avlsConnectDone.Set();
                }
                
            }
            catch (Exception ex)
            {
                //unsTcpClient.GetStream().Close();
                //unsTcpClient.Close();
                Console.WriteLine(ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error(ex);
                if(avlsNetworkStream!=null)
                    avlsNetworkStream.Close();
                if(avlsTcpClient!=null)
                    avlsTcpClient.Close();
                avlsTcpClient = new TcpClient();
                avlsConnectDone.Reset();
                avlsTcpClient.BeginConnect(avls_ipaddress, avls_port, new AsyncCallback(AvlsConnectCallback), avlsTcpClient);
                avlsConnectDone.WaitOne();
                Keeplive.keep(avlsTcpClient.Client);
                avlsNetworkStream = avlsTcpClient.GetStream();
            }

        }
        private static bool CheckIfUidExist(string uid)
        {
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            string sqlCmd = @"SELECT 
  sd.equipment.uid
FROM
  sd.equipment
WHERE
  sd.equipment.uid = '" + uid + @"'
LIMIT 1";
            while (!sql_client.connect())
            {
                Thread.Sleep(30);
            }


            DataTable dt = sql_client.get_DataTable(sqlCmd);
            sql_client.disconnect();
            sql_client.Dispose();
			sql_client=null;
            if (dt != null && dt.Rows.Count != 0)
            {
                dt.Dispose();
                dt = null;
                return true;
            }
            else
            {
                dt.Dispose();
                dt = null;
                return false;
            }
        }
        private static void ConvertLocToAvlsLoc(ref string lat, ref string lon)
        {
            double tmpLat = double.Parse(lat);
            double tmpLon = double.Parse(lon);
            double latInt = Math.Truncate(tmpLat);
            double lonInt = Math.Truncate(tmpLon);
            double latNumberAfterPoint = tmpLat - latInt;
            double lonNumberAfterPoint = tmpLon - lonInt;
            lat = ((latNumberAfterPoint * 60 / 100 + latInt) * 100).ToString();
            lon = ((lonNumberAfterPoint * 60 / 100 + lonInt) * 100).ToString();
        }

        private static int avlsAccessCount = 0;
        private static bool avlsFlag = false;
        private static object IsFirstExecuteLock = new object();
        private static uint deviceCount = 0;
        static Queue xmlQueue = new Queue();
        static Queue avlsQueue = new Queue();
        static Queue sqlQueue = new Queue();

        private static void Main(string[] args)
        {

            //Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory + "Client.exe");
            //int work, complete;
            /*
            ThreadPool.GetMinThreads(out work,out complete);
            Console.WriteLine("min workerThreads={0}:min completionPortThreads={1}", work, complete);

            ThreadPool.GetMaxThreads(out work, out complete);
            Console.WriteLine("max workerThreads={0}:max completionPortThreads={1}", work, complete);

            Thread.Sleep(5000);
            ThreadPool.SetMinThreads(int.Parse(ConfigurationManager.AppSettings["MinWorkerThreads"]), int.Parse(ConfigurationManager.AppSettings["MinCompletionPortThreads"]));
            ThreadPool.SetMaxThreads(int.Parse(ConfigurationManager.AppSettings["MaxWorkerThreads"]), int.Parse(ConfigurationManager.AppSettings["MaxCompletionPortThreads"]));
            */
            Thread.Sleep(5000);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            string StartupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string datalogicFilePath = Path.Combine(StartupPath, "StayCheck.sdf");
            string connString = string.Format("Data Source={0}", datalogicFilePath);
            //Console.WriteLine(connString);

            /*
            //string connString = "Data Source = 'StayCheck.sdf'";
            StayCheck sqlCEdb = new StayCheck(connString);
            string searchID = string.Empty;
            try
            {
                searchID = (from p in sqlCEdb.CheckIfOverTime where p.Uid == "id" select p.Uid).First();
            }
            catch (Exception)
            {

                if (string.IsNullOrEmpty(searchID))
                {
                    //not found id in sql->add new row with id
                    CheckIfOverTime newRow = new CheckIfOverTime();
                    newRow.Uid = "id";
                    newRow.CreateTime = DateTime.Now;
                    sqlCEdb.CheckIfOverTime.InsertOnSubmit(newRow);
                    sqlCEdb.SubmitChanges();
                }
            }
            CheckIfOverTime getRow = sqlCEdb.CheckIfOverTime.FirstOrDefault(p => p.CreateTime == null && p.Uid == "id");
            if (getRow != null)
            {
            }
            else
            {
                
            }
            */
            // Force a reload of the changed section. This 
            // makes the new values available for reading.
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            ConfigurationManager.RefreshSection(sectionName);

            SiAuto.Si.Enabled = true;
            SiAuto.Si.Level = Level.Debug;
            SiAuto.Si.Connections = @"file(filename=""" +
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                    "\\log.sil\",rotate=weekly,append=true,maxparts=5,maxsize=500MB)";
            string logMsg = string.Empty;
            logMsg = "Start time:" + DateTime.Now.ToString("G");
            SiAuto.Main.LogError(logMsg);
            log.Fatal(logMsg);
            //SiAuto.Main.LogMessage("This is my first SmartInspect message!");
            //SiAuto.Main.LogText(Level.Debug,"test","hahaha");


            //Console.WriteLine(GetLocalIPAddress());//current ip address
            //Console.WriteLine(System.Environment.UserName);//current username
            //Console.WriteLine(string.Format("{0:yyMMddHHmmss}", DateTime.Now));
            //Console.WriteLine(DateTime.Now.ToString("yyyyMMdd HHmmss+8"));
            //Console.WriteLine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            //test();
            unsTcpClient = new TcpClient();

            avlsTcpClient = new TcpClient();

            /*
            while (!mups_connected)
            {
                try
                {
                    unsTcpClient.Connect(ipAddress, port);
                    mups_connected = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    log.Error("Connect to MUPS Server Error:"+Environment.NewLine+ex);
                }
            }
            */
            //unsTcpClient.NoDelay = false;

            unsConnectDone.Reset();
            unsTcpClient.BeginConnect(ipAddress, port, new AsyncCallback(ConnectCallback), unsTcpClient);
            unsConnectDone.WaitOne();
            Keeplive.keep(unsTcpClient.Client);
            NetworkStream netStream = unsTcpClient.GetStream();
            unsNetworkStream = netStream;

            //avls_tcpClient.Connect(ipAddress, port);
            avlsConnectDone.Reset();
            avlsTcpClient.BeginConnect(avls_ipaddress, avls_port, new AsyncCallback(AvlsConnectCallback), avlsTcpClient);
            avlsConnectDone.WaitOne();
            Keeplive.keep(avlsTcpClient.Client);
            avlsNetworkStream = avlsTcpClient.GetStream();

            var sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"],
                ConfigurationManager.AppSettings["SQL_SERVER_PORT"],
                ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"],
                ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"],
                ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"],
                ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"],
                ConfigurationManager.AppSettings["ConnectionLifetime"]);

            while (!sql_client.connect())
            {
                Thread.Sleep(30);
            }
            string totalDevice =
                sql_client.get_DataTable(@"SELECT reltuples FROM pg_class WHERE oid = 'sd.equipment'::regclass").Rows[0]
                    .ItemArray[0].ToString();
            //sql_client.disconnect();

            deviceCount = uint.Parse(totalDevice);
            //empty power column in table custom.uns_deivce_power_status
            string emptyPowerStatusTable = @"DELETE FROM custom.uns_deivce_power_status";
            //while (!sql_client.connect())
            //{
            //Thread.Sleep(30);
            //}
            sql_client.modify(emptyPowerStatusTable);
            //sql_client.disconnect();

            #region

            string regSqlCmd = string.Empty;
            regSqlCmd = @"SELECT
  sd.equipment.uid
  FROM
  sd.equipment
  ";
            //while (!sql_client.connect())
            {
                //Thread.Sleep(30);
            }
            DataTable dt = sql_client.get_DataTable(regSqlCmd);
            //sql_client.disconnect();

            if (dt != null && dt.Rows.Count != 0)
            {
                string uid = string.Empty;
                foreach (DataRow row in dt.Rows)
                {
                    uid = row[0].ToString();
                    regSqlCmd = "INSERT INTO custom.uns_deivce_power_status (uid) VALUES (" + "\'" + uid + "\'" + ")";
                    //while (!sql_client.connect())
                    {
                        //Thread.Sleep(30);
                    }
                    sql_client.modify(regSqlCmd);
                    //sql_client.disconnect();
                    //Console.WriteLine("find:{0}", uid);
                }

            }
            dt.Dispose();
            dt = null;

            #endregion

            string registration_msg = "<Location-Registration-Request><application>" +
                                      ConfigurationManager.AppSettings["application_ID"] +
                                      "</application></Location-Registration-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(registration_msg), registration_msg);
            //using (StreamWriter w = File.AppendText("log.txt"))
            {
                log.Info("send:\r\n" + registration_msg);
                // Close the writer and underlying file.
                //w.Close();
            }
            {
//access sql
                string now = string.Format("{0:yyyyMMdd}", DateTime.Now);
                //while (!sql_client.connect())
                {
                    //Thread.Sleep(30);
                }
                string manual_id_serial_command =
                    sql_client.get_DataTable("SELECT COUNT(_id)   FROM public.operation_log").Rows[0].ItemArray[0]
                        .ToString();
                //sql_client.disconnect();
                MANUAL_SQL_DATA operation_log = new MANUAL_SQL_DATA();
                operation_log._id = "\'" + "operation" + "_" + now + "_" + manual_id_serial_command + "\'";
                operation_log.event_id = @"'null'";
                operation_log.application_id = "\'" + ConfigurationManager.AppSettings["application_ID"] + "\'";
                operation_log.create_user = @"'System'";
                string table_columns, table_column_value, cmd;
                table_column_value = operation_log._id + "," + operation_log.event_id + "," +
                                     operation_log.application_id + "," + operation_log.create_user;
                table_columns = "_id,event_id,application_id,create_user";
                cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES  (" + table_column_value + ")";
                //while (!sql_client.connect())
                {
                    //Thread.Sleep(30);
                }
                sql_client.modify(cmd);
                //sql_client.disconnect();
            }
            sql_client.Dispose();
            sql_client = null;
            //sendtest(netStream);

            //alonso
            Thread read_thread = new Thread(read_thread_method);
            read_thread.Start(unsTcpClient);
            //ThreadPool.QueueUserWorkItem(new WaitCallback(read_thread_method), unsTcpClient);
            if (bool.Parse(ConfigurationManager.AppSettings["ManualSend"]))
            {
                Thread send_test_thread = new Thread(() => ManualSend(netStream));
                send_test_thread.Start();
            }
            else
            {
                //Thread autoSendFromSqlTableThread = new Thread(()=>AutoSend(netStream));
                //autoSendFromSqlTableThread.Start();
                var autoSendFromSqlTableTimer =
                    new System.Timers.Timer((int) uint.Parse(ConfigurationManager.AppSettings["autosend_interval"])*1000);
                autoSendFromSqlTableTimer.Elapsed += (sender, e) => { AutoSend(netStream); };
                autoSendFromSqlTableTimer.Enabled = true;

            }

            var accessUnsDeivcePowerStatusSqlTable =
                new System.Timers.Timer(
                    int.Parse(ConfigurationManager.AppSettings["uns_deivce_power_status_Timer_interval_sec"])*1000);
            accessUnsDeivcePowerStatusSqlTable.Elapsed +=
                (sender, e) => { SendToAvlsEventColumnSetNegativeOneIfPowerOff(avlsTcpClient, avlsNetworkStream); };
            accessUnsDeivcePowerStatusSqlTable.Enabled = true;

            if (bool.Parse(ConfigurationManager.AppSettings["IsEvery30SecondSendUidEqlSixZeroToAvls"]))
            {
                var every30SecondSendUidEqlSixZeroToAvls = new System.Timers.Timer(30*1000);
                every30SecondSendUidEqlSixZeroToAvls.Elapsed +=
                    (sender, e) => { every30SecondSendUidEqlSixZeroToAvls_Elapsed(avlsTcpClient, avlsNetworkStream); };
                //every30SecondSendUidEqlSixZeroToAvls.Enabled = true;
            }

            var memoryUsageTimer = new System.Timers.Timer(3600000);
            memoryUsageTimer.Elapsed += new ElapsedEventHandler(memoryUsageTimer_Elapsed);
            memoryUsageTimer.Enabled = true;
            //GC
            /*
            while (true)
            {
                Thread.Sleep(5000);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
             * */
            /*
            while (true)
            {
                if (!xmlQueue.Count.Equals(0))
                {
                    Thread xmlParseThread = new Thread(xml_parse);
                    xmlParseThread.Start(xmlQueue.Dequeue());
                    //xmlParseThread.Join(int.Parse(ConfigurationManager.AppSettings["xmlParseJoinTimeout"]));
                }
                Thread.Sleep(30);
            }
            */
            /*
            var xmlParseTimer = new System.Timers.Timer(30);
            xmlParseTimer.Elapsed += (sender, e) => { 
                if (!xmlQueue.Count.Equals(0))
                xml_parse(xmlQueue.Dequeue()); };
            xmlParseTimer.Enabled = true;
            */
            {
                var avlsTimer = new System.Timers.Timer(15);
                avlsTimer.Elapsed += (sender, e) =>
                {
                    if (avlsQueue.Count>0)
                        access_avls_server(avlsQueue.Dequeue());
                };
                avlsTimer.Enabled = true;
            }
            {
                var sqlTimer = new System.Timers.Timer(30);
                sqlTimer.Elapsed += (sender, e) =>
                {
                    if (sqlQueue.Count>0)
                        access_sql_server(sqlQueue.Dequeue());
                };
                sqlTimer.Enabled = true;
            }   

        Console.ReadLine();
            /*
            var GC =
                    new System.Timers.Timer(60 * 1000);
            GC.Elapsed += (sender, e) => { GCFunction(); };
            GC.Enabled = true;
            Console.ReadLine();
            */
            //Thread send_test_thread = new Thread(() => sendtest(netStream, sql_client));
            //send_test_thread.Start();
            //output = ReadLine(unsTcpClient, netStream, output);
            //UnsTcpWriteLine(netStream, String.Join("\n", commands) + "\n");


            //unsTcpClient.Close();
        }

        static void memoryUsageTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string logMsg = string.Empty;
            logMsg = DateTime.Now.ToString("G") + Environment.NewLine +
                  "Memory usage:" +
                  Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            SiAuto.Main.LogError(logMsg);
            log.Fatal(logMsg);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            string logMsg = string.Empty;
            logMsg = "Close time:" + DateTime.Now.ToString("G") + Environment.NewLine +
                  "Memory usage:" +
                  Process.GetCurrentProcess().WorkingSet64/1024.0/1024.0;
            SiAuto.Main.LogError(logMsg);
            log.Fatal(logMsg);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                SiAuto.Main.LogError("Restart:" + exception.ToString());
                log.Fatal("Restart:" + exception.ToString());
            }
                
            Environment.Exit(1);
            //Restart();
        }

        private static void GCFunction()
        {
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        private static void every30SecondSendUidEqlSixZeroToAvls_Elapsed(TcpClient avlsTcpClient, NetworkStream avlsNetworkStream)
        {
            Thread Tevery30SecondSendUidEqlSixZeroToAvls = new Thread(() => SendPackageToAvlsOnlyByUidAndLocGetFromSql("000000", "0", avlsTcpClient, avlsNetworkStream));
            Tevery30SecondSendUidEqlSixZeroToAvls.Start();
        }

        
        /*
        private static void test()
        {
            return;
            var sqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            DataTable dt = new DataTable();
            var sqlCmd =@"SELECT 
  custom.uns_deivce_power_status.uid
FROM
  custom.uns_deivce_power_status
WHERE
  custom.uns_deivce_power_status.""updateTime"" <= current_timestamp- interval '" +
    ConfigurationManager.AppSettings["setNegativeOneToAvlsInterval"] + 
@"' AND 
  custom.uns_deivce_power_status.power = 'off'";
            sqlClient.connect();
            dt = sqlClient.get_DataTable(sqlCmd);
            sqlClient.disconnect();
            string uid = string.Empty;
            if (dt != null && dt.Rows.Count != 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    uid = row[0].ToString();
                    Console.WriteLine("find:{0}",uid);
                }
               
            }
            else
            {
                Console.WriteLine("not find");
            }
        }
        */
        private static void SendToAvlsEventColumnSetNegativeOneIfPowerOff(TcpClient avlsTcpClient,NetworkStream avlsNetworkStream)
        {
            //SiAuto.Main.EnterMethod(Level.Debug, "SendToAvlsEventColumnSetNegativeOneIfPowerOff");
            var sqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            var sqlCmd =@"SELECT 
  custom.uns_deivce_power_status.uid
FROM
  custom.uns_deivce_power_status
WHERE
  custom.uns_deivce_power_status.""updateTime"" <= current_timestamp- interval '" +
    ConfigurationManager.AppSettings["setNegativeOneToAvlsInterval"] + 
@"' AND 
  custom.uns_deivce_power_status.power = 'off'";
            while (!sqlClient.connect())
            {
                Thread.Sleep(30);
            }

            DataTable dt = sqlClient.get_DataTable(sqlCmd);
            //sqlClient.disconnect();
            string uid = string.Empty;
            if (dt != null && dt.Rows.Count != 0)
            {
                string unsUpdateTimeStamp = string.Empty;
                string unsSqlCmd = string.Empty;
                foreach (DataRow row in dt.Rows)
                {

                    uid = row[0].ToString();
                    string device_uid = uid;

                    //alonso
                    Thread TSendPackageToAvlsOnlyByUidAndLocGetFromSql =
                        new Thread(
                            () =>
                                SendPackageToAvlsOnlyByUidAndLocGetFromSql(device_uid, "-1", avlsTcpClient,
                                    avlsNetworkStream));
                    TSendPackageToAvlsOnlyByUidAndLocGetFromSql.Priority = ThreadPriority.BelowNormal;
                    TSendPackageToAvlsOnlyByUidAndLocGetFromSql.Start();

                    unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                    unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = NULL,
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + device_uid + @"'";
                    //while (!sqlClient.connect())
                    {
                        //Thread.Sleep(30);
                    }
                    //lock (access_uns_deivce_power_status_Lock)
                    sqlClient.modify(unsSqlCmd);
                    sqlClient.disconnect();
                    sqlClient.Dispose();
					sqlClient=null;
                    Thread.Sleep(30);
                    

                }
            }
            dt.Dispose();
            dt = null;
            /*
            else
            {
                //send power on only
                sqlCmd = @"SELECT 
  custom.uns_deivce_power_status.uid
FROM
  custom.uns_deivce_power_status
WHERE
  custom.uns_deivce_power_status.power = 'on'";
                while (!sqlClient.connect())
                {
                    Thread.Sleep(30);
                }

                dt = sqlClient.get_DataTable(sqlCmd);
                sqlClient.disconnect();
                if (dt != null && dt.Rows.Count != 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        uid = row[0].ToString();
                        string device_uid = uid;
                        //alonso
                        Thread TSendPackageToAvlsOnlyByUidAndLocGetFromSqlPowerOn =
                            new Thread(
                                () =>
                                    SendPackageToAvlsOnlyByUidAndLocGetFromSql(device_uid, "181", avlsTcpClient,
                                        avlsNetworkStream));
                        TSendPackageToAvlsOnlyByUidAndLocGetFromSqlPowerOn.Start();

                    }
                }
            }*/
            //SiAuto.Main.LeaveMethod(Level.Debug, "SendToAvlsEventColumnSetNegativeOneIfPowerOff");
        }
        private static void SendPackageToAvlsOnlyByUidAndLocGetFromSql(string uid, string eventStatus, TcpClient avlsTcpClient, NetworkStream avlsNetworkStream)
        {
            //SiAuto.Main.EnterMethod(Level.Debug, "SendPackageToAvlsOnlyByUidAndLocGetFromSql");
            //TcpClient avls_tcpClient;
            string send_string = string.Empty;
            AVLS_UNIT_Report_Packet avls_package = new AVLS_UNIT_Report_Packet();
            //string ipAddress = "127.0.0.1";
            string ipAddress = ConfigurationManager.AppSettings["AVLS_SERVER_IP"];
            //int port = 23;
            int port = int.Parse(ConfigurationManager.AppSettings["AVLS_SERVER_PORT"]);

            //avlsTcpClient = new TcpClient();
            //avlsConnectDone.Reset();
            //avlsTcpClient.BeginConnect(ipAddress, port, new AsyncCallback(AvlsConnectCallback), avlsTcpClient);
            //avlsConnectDone.WaitOne();

            //avls_tcpClient.NoDelay = false;

            //Keeplive.keep(avls_tcpClient.Client);
            //NetworkStream netStream = avls_tcpClient.GetStream();
            avls_package.Event = eventStatus + ",";
            avls_package.Date_Time = string.Format("{0:yyMMddHHmmss}", DateTime.Now.ToUniversalTime()) + ",";
            avls_package.ID = uid;
            avls_package.GPS_Valid = "A,";

            if (true)
            {
                var avlsSqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                string avlsSqlCmd = @"SELECT 
  public._gps_log._lat,
  public._gps_log._lon
FROM
  public._gps_log
WHERE
  public._gps_log._time < now() AND 
  public._gps_log._uid = '" + avls_package.ID + @"'
ORDER BY
  public._gps_log._time DESC
LIMIT 1";
                log.Info("avlsSqlCmd=" + Environment.NewLine + avlsSqlCmd);
                while (!avlsSqlClient.connect())
                {
                    Thread.Sleep(30);
                }
                
                DataTable dt = avlsSqlClient.get_DataTable(avlsSqlCmd);
                avlsSqlClient.disconnect();
                avlsSqlClient.Dispose();
				avlsSqlClient=null;
                if (dt != null && dt.Rows.Count != 0)
                {
                    string avlsLat = string.Empty, avlsLon = string.Empty;
                    foreach (DataRow row in dt.Rows)
                    {
                        avlsLat = row[0].ToString();
                        avlsLon = row[1].ToString();
                    }
                    string zero = "0";
                    if (avlsLat.Equals(zero) || avlsLon.Equals(zero))
                    {
                        GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                    }
                    //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                    //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                    //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                    //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                    //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                    string lat_str = avlsLat, long_str = avlsLon;
                    ConvertLocToAvlsLoc(ref lat_str, ref long_str);
                    avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                }
                else
                {
                    string avlsLat = string.Empty, avlsLon = string.Empty;
                    GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                    //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                    //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                    //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                    //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                    //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                    string lat_str = avlsLat, long_str = avlsLon;
                    ConvertLocToAvlsLoc(ref lat_str, ref long_str);
                    avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                    //avls_package.Loc = "N00000.0000E00000.0000,";
                }
                dt.Dispose();
                dt = null;
            }
            else
            {
                string avlsLat = string.Empty, avlsLon = string.Empty;
                GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                //avls_package.Loc = "N00000.0000E00000.0000,";
            }

            avls_package.Speed = "0,";
            avls_package.Dir = "0,";
            avls_package.Temp = "NA,";
            avls_package.Status = "00000000,";
            switch (eventStatus)
            {
                case "-1":
                    avls_package.Message = "power_off_over_1_hour";
                    break;
                case "181":
                    avls_package.Message = "power_on";
                    break;
                case "182":
                    avls_package.Message = "power_off";
                    break;
                default:
                    avls_package.Message = "null";
                    break;

            }


            //}
            avls_package.ID += ",";
            send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + avls_package.Date_Time + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + avls_package.Event + avls_package.Message + "\r\n";
            //SiAuto.Main.LogText(Level.Debug, "send_string", send_string);
            avlsSendPackage = send_string;
            //avlsSendDone.Reset();
            avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
            //avlsSendDone.WaitOne();

            //ReadLine(avls_tcpClient, netStream, send_string.Length);
            //netStream.Close();
            //avls_tcpClient.Close();
            //SiAuto.Main.LeaveMethod(Level.Debug, "SendPackageToAvlsOnlyByUidAndLocGetFromSql");

        }
        private static void AutoSend(NetworkStream netStream)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            //while (true)
            {
                {
                 
                    var AutosendsqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);

                    while (!AutosendsqlClient.connect())
                    {
                        Thread.Sleep(30);
                    }
                    
                    string sqlCmd = @"
SELECT 
  custom.equipment_request.serial_no,
  custom.equipment_request.func_type,
  custom.equipment_request.uid,
  custom.equipment_request.send_value,
  custom.equipment_request.time_interval,
  custom.equipment_request.create_time
FROM
  custom.equipment_request
WHERE
  custom.equipment_request.send_value = 0
ORDER BY
  custom.equipment_request.create_time
LIMIT
1";
                    DataTable dt = AutosendsqlClient.get_DataTable(sqlCmd);
                    //AutosendsqlClient.disconnect();
                    //AutosendsqlClient.Dispose();
                    //AutosendsqlClient = null;
                    Hashtable requeseHashtable = new  Hashtable();
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            requeseHashtable.Add("serial_no", row[0]);
                            requeseHashtable.Add("func_type", row[1]);
                            requeseHashtable.Add("uid", row[2]);
                            requeseHashtable.Add("send_value", row[3]);
                            requeseHashtable.Add("time_interval", row[4]);
                            requeseHashtable.Add("distance_interval", row[4]);
                            requeseHashtable.Add("create_time", row[5]);
                        }
                        if (CheckIfUidExist(requeseHashtable["uid"].ToString()))
                        {
                        }
                        else
                        {
                            //continue;
                            Thread.Sleep(30);
                            return ;
                        }
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
                         * 
                        func_type
                         * 0: 回報載具定位資訊, 
    1:開啟載具定位資訊回報
    2: 關閉載具定位資訊回報 
    3: 設定載具定位資訊回傳時間

    ");
                         */
                        {
                            string select_num = (string)requeseHashtable["func_type"];

                            switch (select_num)//ConfigurationManager.AppSettings["output-value"]
                            {
                                case "0":
                                    string Immediate_Location_Request = "<Immediate-Location-Request><request-id>" +
                                        ConfigurationManager.AppSettings["request-id"] +
                                        "</request-id><suaddr suaddr-type=\"" +
                                        ConfigurationManager.AppSettings["suaddr-type"] +
                                        "\">" +
                                        requeseHashtable["uid"] +
                                        "</suaddr></Immediate-Location-Request>";
                                    //using (StreamWriter w = File.AppendText("log.txt"))
                                    {
                                        log.Info("send:\r\n"+ Immediate_Location_Request);
                                        // Close the writer and underlying file.
                                        //w.Close();
                                    }
                                    SiAuto.Main.LogText(Level.Debug, "Immediate_Location_Request", Immediate_Location_Request);
                                    UnsTcpWriteLine(netStream, data_append_dataLength(Immediate_Location_Request), Immediate_Location_Request);

                                    break;
                                case "-5":
                                    string Location_Protocol_Request = "<Location-Protocol-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><request-protocol-version>2</request-protocol-version></Location-Protocol-Request>";

                                    //using (StreamWriter w = File.AppendText("log.txt"))
                                    {
                                        log.Info("send:\r\n"+Location_Protocol_Request);
                                        // Close the writer and underlying file.
                                        //w.Close();
                                    }
                                    UnsTcpWriteLine(netStream, data_append_dataLength(Location_Protocol_Request), Location_Protocol_Request);
                                    break;
                                case "2":
                                    string Triggered_Location_Stop_Request = "<Triggered-Location-Stop-Request><request-id>" +
                                        ConfigurationManager.AppSettings["request-id"] +
                                        "</request-id><suaddr suaddr-type=\"" +
                                        ConfigurationManager.AppSettings["suaddr-type"] +
                                        "\">" +
                                        requeseHashtable["uid"] +
                                        "</suaddr></Triggered-Location-Stop-Request>";
                                    SiAuto.Main.LogText(Level.Debug, "Triggered_Location_Stop_Request", Triggered_Location_Stop_Request);
                                    //using (StreamWriter w = File.AppendText("log.txt"))
                                    {
                                        log.Info("send:\r\n"+ Triggered_Location_Stop_Request);
                                        // Close the writer and underlying file.
                                        //w.Close();
                                    }
                                    UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Stop_Request), Triggered_Location_Stop_Request);
                                    break;
                                case "1":
                                case "3":
                                    string Triggered_Location_Request_Cadence = "<Triggered-Location-Request><request-id>" +
                                        ConfigurationManager.AppSettings["request-id"] +
                                        "</request-id><suaddr suaddr-type=\"" +
                                        ConfigurationManager.AppSettings["suaddr-type"] +
                                        "\">" +
                                        requeseHashtable["uid"] +
                                        "</suaddr><periodic-trigger><interval>" +
                                        requeseHashtable["time_interval"] +
                                        "</interval></periodic-trigger></Triggered-Location-Request>";
                                    SiAuto.Main.LogText(Level.Debug, "Triggered_Location_Request_Cadence", Triggered_Location_Request_Cadence);
                                    //using (StreamWriter w = File.AppendText("log.txt"))
                                    {
                                        log.Info("send:\r\n"+ Triggered_Location_Request_Cadence);
                                        // Close the writer and underlying file.
                                        //w.Close();
                                    }
                                    UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Cadence), Triggered_Location_Request_Cadence);
                                    break;
                                case "4":
                                    string Triggered_Location_Request_Distance = "<Triggered-Location-Request><request-id>" +
                                        ConfigurationManager.AppSettings["request-id"] + 
                                        "</request-id><suaddr suaddr-type=\"" + 
                                        ConfigurationManager.AppSettings["suaddr-type"] + "\">" +
                                        requeseHashtable["uid"] +
                                        "</suaddr><periodic-trigger><trg-distance>" +
                                        requeseHashtable["distance_interval"]+ // meter
                                        //(int)(double.Parse(requeseHashtable["distance_interval"].ToString()) * 1.0936133) +//meter to yard
                                        "</trg-distance></periodic-trigger></Triggered-Location-Request>";

                                    //using (StreamWriter w = File.AppendText("log.txt"))
                                    {
                                        log.Info("send:\r\n"+ Triggered_Location_Request_Distance);
                                        // Close the writer and underlying file.
                                        //w.Close();
                                    }
                                    UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Distance), Triggered_Location_Request_Distance);
                                    break;
                                case "-4":
                                    string Digital_Output_Change_Request = "<Digital-Output-Change-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><output-info><output-name>" + ConfigurationManager.AppSettings["output-name"] + "</output-name><output-value>" + ConfigurationManager.AppSettings["output-value"] + "</output-value></output-info></Digital-Output-Change-Request>";

                                    //using (StreamWriter w = File.AppendText("log.txt"))
                                    {
                                        log.Info("send:\r\n"+ Digital_Output_Change_Request);
                                        // Close the writer and underlying file.
                                        //w.Close();
                                    }
                                    UnsTcpWriteLine(netStream, data_append_dataLength(Digital_Output_Change_Request), Digital_Output_Change_Request);
                                    break;
                            }
                        }

                        if (int.Parse((string)requeseHashtable["send_value"]).Equals(0))
                        {

                            sqlCmd = @"UPDATE custom.equipment_request SET send_value = 1 WHERE custom.equipment_request.serial_no = '" +
                                requeseHashtable["serial_no"] + @"'";
                            //while (!AutosendsqlClient.connect())
                            {
                                //Thread.Sleep(30);
                            }
                            AutosendsqlClient.modify(sqlCmd);
                            AutosendsqlClient.disconnect();
                        }
                    }
                    dt.Dispose();
                    dt = null;
                    AutosendsqlClient.Dispose();
					AutosendsqlClient=null;
                    //Thread.Sleep((int)uint.Parse(ConfigurationManager.AppSettings["autosend_interval"]) * 1000);
                }
                
            }
            Thread.Sleep(30);
        }
        /*
        private static void sendtest(NetworkStream netStream , SqlClient sql_client)
        {
            string Immediate_Location_Request = "<Immediate-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr></Immediate-Location-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(Immediate_Location_Request), Immediate_Location_Request);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Immediate_Location_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Location_Protocol_Request = "<Location-Protocol-Request><request-id>4356A</request-id><request-protocol-version>2</request-protocol-version></Location-Protocol-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(Location_Protocol_Request), Location_Protocol_Request);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Location_Protocol_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Triggered_Location_Stop_Request = "<Triggered-Location-Stop-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr></Triggered-Location-Stop-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Stop_Request), Triggered_Location_Stop_Request);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_Location_Stop_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Triggered_Location_Request_Cadence = "<Triggered-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr><periodic-trigger><interval>60</interval></periodic-trigger></Triggered-Location-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Cadence), Triggered_Location_Request_Cadence);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_Location_Request_Cadence, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Triggered_Location_Request_Distance = "<Triggered-Location-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1004</suaddr><periodic-trigger><trg-distance>100</trg-distance></periodic-trigger></Triggered-Location-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Distance), Triggered_Location_Request_Distance);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , Triggered_Location_Request_Distance, w);
                // Close the writer and underlying file.
                w.Close();
            }

            string Digital_Output_Change_Request = "<Digital-Output-Change-Request><request-id>2468ACE0</request-id><suaddr suaddr-type=\"APCO\">1234568</suaddr><output-info><output-name>Alarm</output-name><output-value>1</output-value></output-info></Digital-Output-Change-Request>";
            UnsTcpWriteLine(netStream, data_append_dataLength(Digital_Output_Change_Request), Digital_Output_Change_Request);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" ,Digital_Output_Change_Request, w);
                // Close the writer and underlying file.
                w.Close();
            }
            string error = "<error></error>";
            UnsTcpWriteLine(netStream, data_append_dataLength(error), error);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n" , error, w);
                // Close the writer and underlying file.
                w.Close();
            }
        }
         * */
        private static void ManualSend(NetworkStream netStream)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
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
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            log.Info("send:\r\n"+ Immediate_Location_Request);
                            // Close the writer and underlying file.
                            //w.Close();
                        }
                        UnsTcpWriteLine(netStream, data_append_dataLength(Immediate_Location_Request), Immediate_Location_Request);
                        
                        break;
                    case "5":
                        string Location_Protocol_Request = "<Location-Protocol-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><request-protocol-version>2</request-protocol-version></Location-Protocol-Request>";
                        
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            log.Info("send:\r\n"+ Location_Protocol_Request);
                            // Close the writer and underlying file.
                            //w.Close();
                        }
                        UnsTcpWriteLine(netStream, data_append_dataLength(Location_Protocol_Request), Location_Protocol_Request);
                        break;
                    case "6":
                        string Triggered_Location_Stop_Request = "<Triggered-Location-Stop-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr></Triggered-Location-Stop-Request>";
                        
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            log.Info("send:\r\n"+ Triggered_Location_Stop_Request);
                            // Close the writer and underlying file.
                            //w.Close();
                        }
                        UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Stop_Request), Triggered_Location_Stop_Request);
                        break;
                    case "2":
                        string Triggered_Location_Request_Cadence = "<Triggered-Location-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><periodic-trigger><interval>" + ConfigurationManager.AppSettings["interval"] + "</interval></periodic-trigger></Triggered-Location-Request>";
                        
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            log.Info("send:\r\n"+ Triggered_Location_Request_Cadence);
                            // Close the writer and underlying file.
                            //w.Close();
                        }
                        UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Cadence), Triggered_Location_Request_Cadence);
                        break;
                    case "3":
                        string Triggered_Location_Request_Distance = "<Triggered-Location-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><periodic-trigger><trg-distance>" + ConfigurationManager.AppSettings["trg-distance"] + "</trg-distance></periodic-trigger></Triggered-Location-Request>";
                        
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            log.Info("send:\r\n"+ Triggered_Location_Request_Distance);
                            // Close the writer and underlying file.
                            //w.Close();
                        }
                        UnsTcpWriteLine(netStream, data_append_dataLength(Triggered_Location_Request_Distance), Triggered_Location_Request_Distance);
                        break;
                    case "4":
                        string Digital_Output_Change_Request = "<Digital-Output-Change-Request><request-id>" + ConfigurationManager.AppSettings["request-id"] + "</request-id><suaddr suaddr-type=\"" + ConfigurationManager.AppSettings["suaddr-type"] + "\">" + ConfigurationManager.AppSettings["suaddr"] + "</suaddr><output-info><output-name>" + ConfigurationManager.AppSettings["output-name"] + "</output-name><output-value>" + ConfigurationManager.AppSettings["output-value"] + "</output-value></output-info></Digital-Output-Change-Request>";
                        
                        //using (StreamWriter w = File.AppendText("log.txt"))
                        {
                            log.Info("send:\r\n"+ Digital_Output_Change_Request);
                            // Close the writer and underlying file.
                            //w.Close();
                        }
                        UnsTcpWriteLine(netStream, data_append_dataLength(Digital_Output_Change_Request), Digital_Output_Change_Request);
                        break;
                }
                Thread.Sleep(30);
            }
            /*                                
            string error = "<error></error>";
            UnsTcpWriteLine(netStream, data_append_dataLength(error), error, sql_client);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("send:\r\n", error, w);
                // Close the writer and underlying file.
                w.Close();
            }
            */
        }

        private static void UnsTcpWriteLine(NetworkStream netStream, byte[] writeData,string write )
        {
            unsSendPackage = writeData;
            unsUnsTcpWriteLineWriteParame = write;
            if (netStream.CanWrite)
            {
                //byte[] writeData = Encoding.ASCII.GetBytes(write);
                try
                {
                    XDocument xml = XDocument.Parse(write);
                    write = xml.ToString();
                    
                    

                    //send method1
                    //netStream.Write(writeData, 0, writeData.Length);
                    // 需等待資料真的已寫入 NetworkStream
                    //Thread.Sleep(3000);

                    //send method2
                    IAsyncResult result = netStream.BeginWrite(writeData, 0, writeData.Length, new AsyncCallback(UnsTcpWriteCallBack), netStream);
                    result.AsyncWaitHandle.WaitOne();
                    result.AsyncWaitHandle.Close();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("S****************************************************************************");
                    Console.WriteLine("uns write:\r\n" + write);
                    Console.WriteLine("E****************************************************************************");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WriteLineError:\r\n" + ex);
                    //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                    //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                    log.Error("WriteLineError:\r\n" + ex);

                    if (unsTcpClient != null)
                    {
                        if (netStream != null)
                            netStream.Close();
                        unsTcpClient.Close();

                    }
                    unsTcpClient = new TcpClient();

                    unsConnectDone.Reset();
                    unsTcpClient.BeginConnect(ipAddress, port, new AsyncCallback(ConnectCallback), unsTcpClient);
                    unsConnectDone.WaitOne();
                    Keeplive.keep(unsTcpClient.Client);
                    if (unsTcpClient != null && unsTcpClient.Client != null)
                    {
                        unsNetworkStream = unsTcpClient.GetStream();
                        UnsTcpWriteLine(unsNetworkStream, unsSendPackage, unsUnsTcpWriteLineWriteParame);
                    }
                   
                }

                
            }
        }
        public static void UnsTcpWriteCallBack(IAsyncResult ar)
        {
            NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
            try
            {
                
                myNetworkStream.EndWrite(ar);
            }
            catch (Exception ex)
            {

                Console.WriteLine("UnsTcpWriteCallBack:\r\n" + ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("UnsTcpWriteCallBack:\r\n" + ex);

                if (unsTcpClient != null)
                {
                    if (myNetworkStream != null)
                        myNetworkStream.Close();
                    unsTcpClient.Close();

                }
                unsTcpClient = new TcpClient();

                unsConnectDone.Reset();
                unsTcpClient.BeginConnect(ipAddress, port, new AsyncCallback(ConnectCallback), unsTcpClient);
                unsConnectDone.WaitOne();
                Keeplive.keep(unsTcpClient.Client);
                if (unsTcpClient != null && unsTcpClient.Client != null)
                {
                    unsNetworkStream = unsTcpClient.GetStream();
                    UnsTcpWriteLine(unsNetworkStream, unsSendPackage, unsUnsTcpWriteLineWriteParame);
                }
            }
            
        }
        public static void avls_myWriteCallBack(IAsyncResult ar)
        {
            try
            {
                NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
                myNetworkStream.EndWrite(ar);
                
               
                //avlsSendDone.Set();
            }
            catch (Exception ex)
            {

                Console.WriteLine("avls_myWriteCallBack:\r\n" + ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("avls_myWriteCallBack:\r\n" + ex);
                if (avlsNetworkStream != null)
                    avlsNetworkStream.Close();
                if (avlsTcpClient != null)
                    avlsTcpClient.Close();
                avlsTcpClient = new TcpClient();
                avlsConnectDone.Reset();
                avlsTcpClient.BeginConnect(avls_ipaddress, avls_port, new AsyncCallback(AvlsConnectCallback), avlsTcpClient);
                avlsConnectDone.WaitOne();
                Keeplive.keep(avlsTcpClient.Client);
                if (avlsTcpClient != null && avlsTcpClient.Client != null)
                {
                    avlsNetworkStream = avlsTcpClient.GetStream();
                    avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(avlsSendPackage), avlsSendPackage);
                    SiAuto.Main.LogMessage(avlsSendPackage);
                }
                
            }
            

        }
        private static void ReadLine(object TtcpClient)
        {
            var tcpClient = TtcpClient as TcpClient;
            NetworkStream netStream = tcpClient.GetStream();
            try
            {
                if (netStream.CanRead)
                {
                    //byte[] bytes = new byte[unsTcpClient.ReceiveBufferSize];
                    avlsConnectDone.WaitOne();
                    //myReadBuffer = new byte[prefix_length];
                    netStream.BeginRead(myReadBuffer, 0, myReadBuffer.Length,
                                                                 new AsyncCallback(myReadSizeCallBack),
                                                                 netStream);
                    
                    //autoEvent.WaitOne();
                    /*
                    int numBytesRead = netStream.Read(bytes, 0,
                        (int)unsTcpClient.ReceiveBufferSize);

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
                Console.WriteLine("ReadLineError:\r\n" + ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("ReadLineError:\r\n" + ex);
                try
                {
                    Monitor.Enter(readRecoveryLock);
                    ReadRecovery(netStream);
                }
                catch (Exception exx)
                {
                    SiAuto.Main.LogError("ReadRecovery",exx.ToString());
                    log.Error("ReadRecovery"+exx.Message);
                    Console.WriteLine("ReadRecovery" + exx.Message);
                }
                finally
                {
                    Monitor.Exit(readRecoveryLock);
                }
                
            }
        }
        public static void myReadSizeCallBack(IAsyncResult ar)
        {
            NetworkStream myNetworkStream = (NetworkStream)ar.AsyncState;
            try
            {
                
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
                Console.WriteLine("myReadSizeCallBackError:"+Environment.NewLine+ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("myReadSizeCallBackError:" + Environment.NewLine + ex);
                try
                {
                    Monitor.Enter(readRecoveryLock);
                    ReadRecovery(myNetworkStream);
                }
                catch (Exception exx)
                {

                    SiAuto.Main.LogError("ReadRecovery", exx.ToString());
                    log.Error("ReadRecovery" + exx.Message);
                    Console.WriteLine("ReadRecovery" + exx.Message);
                }
                finally
                {
                    Monitor.Exit(readRecoveryLock);
                }
                
                
                
            }
        }

        private static void ReadRecovery(NetworkStream myNetworkStream)
        {
                if (unsTcpClient != null)
                {
                    if (myNetworkStream != null)
                        myNetworkStream.Close();
                    unsTcpClient.Close();
                }

                unsTcpClient = new TcpClient();

                unsConnectDone.Reset();
                unsTcpClient.BeginConnect(ipAddress, port, new AsyncCallback(ConnectCallback), unsTcpClient);
                unsConnectDone.WaitOne();
                Keeplive.keep(unsTcpClient.Client);
                myNetworkStream = unsTcpClient.GetStream();

                var sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"],
                    ConfigurationManager.AppSettings["SQL_SERVER_PORT"],
                    ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"],
                    ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"],
                    ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"],
                    ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"],
                    ConfigurationManager.AppSettings["ConnectionLifetime"]);

                string registration_msg = "<Location-Registration-Request><application>" +
                                          ConfigurationManager.AppSettings["application_ID"] +
                                          "</application></Location-Registration-Request>";
                UnsTcpWriteLine(myNetworkStream, data_append_dataLength(registration_msg), registration_msg);
                //using (StreamWriter w = File.AppendText("log.txt"))
                {
                    log.Info("send:\r\n" + registration_msg);
                    // Close the writer and underlying file.
                    //w.Close();
                }
                {
//access sql
                    string now = string.Format("{0:yyyyMMdd}", DateTime.Now);
                    while (!sql_client.connect())
                    {
                        Thread.Sleep(30);
                    }
                    
                    string manual_id_serial_command =
                        sql_client.get_DataTable("SELECT COUNT(_id)   FROM public.operation_log").Rows[0].ItemArray[0]
                            .ToString();
                    //sql_client.disconnect();
                    MANUAL_SQL_DATA operation_log = new MANUAL_SQL_DATA();
                    operation_log._id = "\'" + "operation" + "_" + now + "_" + manual_id_serial_command + "\'";
                    operation_log.event_id = @"'null'";
                    operation_log.application_id = "\'" + ConfigurationManager.AppSettings["application_ID"] + "\'";
                    operation_log.create_user = @"'System'";
                    string table_columns, table_column_value, cmd;
                    table_column_value = operation_log._id + "," + operation_log.event_id + "," +
                                         operation_log.application_id + "," + operation_log.create_user;
                    table_columns = "_id,event_id,application_id,create_user";
                    cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES  (" + table_column_value +
                          ")";
                    //while (!sql_client.connect())
                    {
                        //Thread.Sleep(30);
                    }
                    sql_client.modify(cmd);
                    sql_client.disconnect();
                }
            sql_client.Dispose();
			sql_client=null;
            Thread readlineThread = new Thread(ReadLine);
            readlineThread.Start(unsTcpClient);
                //ReadLine(unsTcpClient, 2);          
        }

        private static void FinishRead(IAsyncResult result)
        {
            NetworkStream fStream = (NetworkStream)result.AsyncState;
            try
            {
                // Finish reading from our stream. 0 bytes read means stream was closed
                
                int read = fStream.EndRead(result);
                if (0 == read)
                    throw new Exception("0 == read");

                // Increment the number of bytes we've read. If there's still more to get, get them
                fBytesRead += read;
                if (fBytesRead < fBuffer.Length)
                {
                    fStream.BeginRead(fBuffer, fBytesRead, fBuffer.Length - fBytesRead, FinishRead, null);
                    return;
                }

                // Should be exactly the right number read now.
                if (fBytesRead != fBuffer.Length)
                    throw new Exception("fBytesRead != fBuffer.Length");

                // Handle the message and go get the next one.
                string returndata = Encoding.ASCII.GetString(fBuffer);
                Array.Resize<byte>(ref fBuffer, 0);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                string output = String.Format("Read: Length: {0}, Data: {1}", returndata.Length, returndata);
                //XDocument xml_data = XDocument.Parse(returndata);
                //string xml_root_tag = xml_data.Root.Name.ToString();
                //Console.WriteLine();
                string ouput2 = string.Empty;
                try
                {
                    ouput2 = XDocument.Parse(returndata).ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FinishReadError1:\r\n" + ex);
                    log.Error("FinishReadError1:\r\n" + ex);
                }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("S############################################################################");
                Console.WriteLine("Read:\r\n" + ouput2);
                //Console.WriteLine("First node:[" + xml_root_tag + "]");
                Console.WriteLine("E############################################################################");
                Console.ResetColor();
                
                //xmlQueue.Enqueue(returndata);
                xml_parse(returndata);
                Thread.Sleep(60);
				//Thread xmlParseThread = new Thread(xml_parse);
                //xmlParseThread.Start(returndata);
                //xmlParseThread.Join(int.Parse(ConfigurationManager.AppSettings["xmlParseJoinTimeout"]));
                //Thread.Sleep(1);
				//xml_parse(new XmlClass(unsTcpClient, fStream, returndata, avlsTcpClient));
                //ThreadPool.QueueUserWorkItem(new WaitCallback(xml_parse), new XmlClass(unsTcpClient, fStream, returndata, avlsTcpClient));

                //Console.ReadLine();

                //byte[] bytes = new byte[unsTcpClient.ReceiveBufferSize];test

                //int numBytesRead = netStream.Read(bytes, 0,
                //(int)unsTcpClient.ReceiveBufferSize);

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
                if (bool.Parse(ConfigurationManager.AppSettings["ManualSend"]))
                {
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
                avlsConnectDone.WaitOne();
                //OnMessageRead(fBuffer);
                fStream.BeginRead(myReadBuffer, 0, myReadBuffer.Length,
                                                                 new AsyncCallback(myReadSizeCallBack),
                                                                 fStream);
               // fStream.BeginRead(fSizeBuffer, 0, fSizeBuffer.Length, FinishReadSize, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FinishReadError:\r\n" + ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("FinishReadError:\r\n" + ex);
                if (ex.GetType() == typeof(OutOfMemoryException))
                {
                    log.Error("memory usage" + GC.GetTotalMemory(true));
                    Environment.Exit(1);
                    //Restart();
                }
                else
                {
                    try
                    {
                        Monitor.Enter(readRecoveryLock);
                        ReadRecovery(fStream);
                    }
                    catch (Exception exx)
                    {

                        SiAuto.Main.LogError("ReadRecovery", exx.ToString());
                        log.Error("ReadRecovery" + exx.Message);
                        Console.WriteLine("ReadRecovery" + exx.Message);
                    }
                    finally
                    {
                        Monitor.Exit(readRecoveryLock);
                    } 
                }
                
                
            }
        }
        static void read_thread_method(object tcpClientObject)
        {
            var tcpClient = tcpClientObject as TcpClient;
            Console.WriteLine("in read thread");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            //asyn read
            Thread readlineThread = new Thread(ReadLine);
            readlineThread.Start(unsTcpClient);
            //ReadLine(tcpClient, 2);
			
            //syn read
            //while (true)
            {
                //Thread.Sleep(30);
                //if (netStream.CanRead)// && netStream.DataAvailable)
                {
                    //string xml_test = "<test></test>";
                    //int receive_total_length = unsTcpClient.ReceiveBufferSize;
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
                        Console.WriteLine("ReadError:\r\n" + ex);
                    }
                    Console.WriteLine("S############################################################################");
                    Console.WriteLine( "Read:\r\n"+ouput2 );
                    //Console.WriteLine("First node:[" + xml_root_tag + "]");
                    Console.WriteLine("E############################################################################");
                    xml_parse(unsTcpClient, netStream, xml_root_tag, xml_data, sql_client);
                    */
                    //Console.ReadLine();
                    
                    //byte[] bytes = new byte[unsTcpClient.ReceiveBufferSize];
                    
                    //int numBytesRead = netStream.Read(bytes, 0,
                        //(int)unsTcpClient.ReceiveBufferSize);
                    
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
            //Restart();
        }

        //private static void xml_parse(TcpClient tcpClient, NetworkStream netStream, string returndata, TcpClient avlsTcpClient)
        private static void xml_parse(object o)
        {
            
            Stopwatch stopWatch = Stopwatch.StartNew();
            TcpClient tcpClient = unsTcpClient;
            string returndata = o as string;
            XDocument xml_data = XDocument.Parse(returndata);
            string xml_root_tag = xml_data.Root.Name.ToString();
            string logData = xml_data.ToString();
            //using (StreamWriter w = File.AppendText("log.txt"))
            {
                log.Info("receive:\r\n" + logData);
                // Close the writer and underlying file.
                //w.Close();
            }
            Hashtable htable = new Hashtable();
            List<string> sensor_name = new List<string>();
            List<string> sensor_value = new List<string>();
            List<string> sensor_type = new List<string>();
            SiAuto.Main.LogText(Level.Debug, xml_root_tag+"reveive time:"+DateTime.UtcNow.ToString("g"), xml_data.ToString());
            HashSet<XName> elements = XmlGetAllElementsXname(xml_data);
            switch (xml_root_tag)
            {
                case "Triggered-Location-Report":
                case "Immediate-Location-Report":
                case "Unsolicited-Location-Report":
                    {
                        //if (xml_root_tag.Equals("Immediate-Location-Report"))
                        //{
                            //SiAuto.Main.LogMessage(xml_data.ToString());
                        //}
                        //IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("suaddr").Name))
                        {
                            string id = string.Empty;
                            id = XmlGetTagValue(xml_data, "suaddr");
                            if(id.Equals(string.Empty))
                                return;
                            else
                            {
                                htable.Add("suaddr", id);
                                //Console.WriteLine("suaddr:{0}", htable["suaddr"]);
                            }
                            
                            
                            if(CheckIfUidExist(htable["suaddr"].ToString()))
                            {}
                            else
                            {
                                return;
                            }
                        }
                        if (elements.Contains(new XElement("event-info").Name) && xml_root_tag == "Unsolicited-Location-Report")
                        {
                            htable.Add("event_info", XmlGetTagValue(xml_data, "event-info"));
                            //Console.WriteLine("event_info:{0}", htable["event_info"]);
                        }
                        if (elements.Contains(new XElement("operation-error").Name))
                        {
                             htable.Add("result_code",XmlGetTagAttributeValue(xml_data, "result", "result-code"));
                             //htable.Add("err_msg" , XmlGetTagValue(xml_data, "result"));
                             htable.Add("result_msg", ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"]]);
                             if (htable["result_code"].Equals("3"))//UNAUTHORIZED APPLICATION
                             {
                                 log.Info("UNAUTHORIZED APPLICATION call restart");
                                 //Restart();
                                 Environment.Exit(1);
                             }
                             //Console.WriteLine("result_code:{0}", htable["result_code"]);
                             //Console.WriteLine("result_msg:{0}", htable["result_msg"]);
                        }
                        if (elements.Contains(new XElement("info-data").Name))
                        {
                            if(elements.Contains(new XElement("impl-spec-data").Name))
                            {}
                            
                            //string shape_type = (string)(from e in xml_data.Descendants("shape") select e.Elements().First().Name.LocalName).First();
                            if (elements.Contains(new XElement("info-time").Name))
                                htable.Add("info_time",  XmlGetTagValue(xml_data, "info-time"));//info-data scope
                            if (elements.Contains(new XElement("server-time").Name))
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
                            if (elements.Contains(new XElement("shape").Name))
                                htable.Add("shape-type" , XmlGetFirstChildTagName(xml_data, "shape"));//info-data scope
                            //Console.WriteLine("shape_type :[{0}]", shape_type);
                            //Console.WriteLine("speed_hor:{0}", speed_hor);
                            //Console.WriteLine("Direction_hor:{0}", Direction_hor);
                            if (elements.Contains(new XElement("shape").Name))
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
                                IEnumerable<XElement> de = from el in xml_data.Descendants("sensor") select el;
                                sensor_name = (from e in de.Descendants("sensor-name") select (string)e).Cast<string>().ToList();
                                sensor_value = (from e in de.Descendants("sensor-value") select (string)e).Cast<string>().ToList();
                                sensor_type = (from e in de.Descendants("sensor-type") select (string)e).Cast<string>().ToList();
                                int i = 0;
                                /*
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
                                */
                            }
                            if (elements.Contains(new XElement("vehicle-info").Name))//Vehicle Info
                            {
                                htable.Add("Odometer" , XmlGetTagValue(xml_data, "odometer"));
                                //Console.WriteLine("Odometer:{0}", Odometer);
                            }
                        }

                        //List<EAB> prohibitedList, locationList;
                    string prohibitedTableName = string.Empty, locationTableName = string.Empty;
                    //prohibitedTableName = "public.prohibited";
                    //locationTableName = "public.patrol_location";
                    //GetRidAndGeomFromSqlTable(prohibitedTableName, out prohibitedList);
                    //GetRidAndGeomFromSqlTable(locationTableName, out locationList);

                    prohibitedTableName = "public.p_prohibited";
                    locationTableName = "public.patrol_location";
                    string getMessage = string.Empty;
                        //lock (getGidAndFullnameLock)
                        {
                            if (htable.ContainsKey("lat_value") && htable.ContainsKey("long_value") && htable.ContainsKey("suaddr"))
                            getMessage = GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql(prohibitedTableName,
                                locationTableName,
                                htable["lat_value"] as string, htable["long_value"] as string, htable["suaddr"] as string, false, htable["suaddr"].ToString());
                        }
                        {
                            Thread access_sql = null, access_avls = null;
                            if (bool.Parse(ConfigurationManager.AppSettings["SQL_ACCESS"]))
                            {
                                /*
                                //sqlAccessEvent.Reset();
                                 access_sql = new Thread(access_sql_server);
                                access_sql.Start(new SqlClass(xml_root_tag, htable, sensor_name.ToList(),
                                    sensor_type.ToList(), sensor_value.ToList(), elements,
                                    logData, getMessage));
                                */
                                sqlQueue.Enqueue(new SqlClass(xml_root_tag, htable, sensor_name.ToList(),
                                    sensor_type.ToList(), sensor_value.ToList(), elements,
                                    logData, getMessage));
                                //ThreadPool.QueueUserWorkItem(new WaitCallback(access_sql_server), new SqlClass(xml_root_tag, htable, sensor_name.ToList(), sensor_type.ToList(), sensor_value.ToList(), XmlGetAllElementsXname(xml_data), logData, getMessage));
                                //access_sql.Join();
                                //Console.WriteLine("SQL Access Enable");
                                //sqlAccessEvent.WaitOne();
                            }

                            if (bool.Parse(ConfigurationManager.AppSettings["AVLS_ACCESS"]))
                            {
                                /*
                                //avlsSendDone.Reset();
                                 access_avls = new Thread(access_avls_server);
                                //access_avls.Priority = ThreadPriority.BelowNormal;
                                access_avls.Start(new AvlsClass(xml_root_tag, htable, sensor_name.ToList(),
                                    sensor_type.ToList(), sensor_value.ToList(), elements,
                                    logData, getMessage));
                                */
                                avlsQueue.Enqueue(new AvlsClass(xml_root_tag, htable, sensor_name.ToList(),
                                    sensor_type.ToList(), sensor_value.ToList(), elements,
                                    logData, getMessage));
                                //ThreadPool.QueueUserWorkItem(new WaitCallback(access_avls_server), new AvlsClass(xml_root_tag, htable, sensor_name.ToList(), sensor_type.ToList(), sensor_value.ToList(), XmlGetAllElementsXname(xml_data), logData, getMessage));
                                //access_avls.Join();
                                //Console.WriteLine("AVLS Access Enable");
                                //avlsSendDone.WaitOne();
                            }
                            //if (access_sql != null) access_sql.Join(int.Parse(ConfigurationManager.AppSettings["accessSqlJoinTimeout"]));
                            //access_avls.Join();
                            //Thread.Sleep(1);
                        }



                        /*
                        Console.Clear();
                        Console.WriteLine("unsAvslPowerOnDeviceCount:"+unsAvslPowerOnDeviceCount);
                        Console.WriteLine("unsSqlPowerOnDeviceCount:"+unsSqlPowerOnDeviceCount);
                        try
                        {
                            string sendToUdpServer = DateTime.Now.ToString("o") + Environment.NewLine + "unsAvslPowerOnDeviceCount:" + unsAvslPowerOnDeviceCount +
                                                Environment.NewLine + "unsSqlPowerOnDeviceCount:" +
                                                unsSqlPowerOnDeviceCount;
                            UdpClient udpClient = new UdpClient(int.Parse(ConfigurationManager.AppSettings["udpPort"]));
                            udpClient.Connect(ConfigurationManager.AppSettings["udpServerIP"], int.Parse(ConfigurationManager.AppSettings["udpPort"]));

                            // Sends a message to the host to which you have connected.
                            Byte[] sendBytes = Encoding.ASCII.GetBytes(sendToUdpServer);

                            udpClient.Send(sendBytes, sendBytes.Length);
                            udpClient.Close();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            log.Error(e.ToString());
                        }
                       */

                    }
                     
                    break;

                
                case "Location-Protocol-Report":
                    {
                        //IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
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
                    
                        //IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        
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
                    
                    if (bool.Parse(ConfigurationManager.AppSettings["SQL_ACCESS"]))
                    {
                        //Thread access_sql = new Thread(access_sql_server);
                        //access_sql.Start(new SqlClass(xml_root_tag, htable, sensor_name.ToList(), sensor_type.ToList(), sensor_value.ToList(), elements, logData, null));
                        sqlQueue.Enqueue(new SqlClass(xml_root_tag, htable, sensor_name.ToList(), sensor_type.ToList(), sensor_value.ToList(), elements, logData, null)); ;
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(access_sql_server), new SqlClass(xml_root_tag, htable, sensor_name.ToList(), sensor_type.ToList(), sensor_value.ToList(), XmlGetAllElementsXname(xml_data), logData, null));
                            
                        //access_sql.Join();
                    }
                    break;
                case "Immediate-Location-Answer":
                case "Triggered-Location-Stop-Answer":
                case "Digital-Output-Answer":
                case "Triggered-Location-Answer":
                    {

                        //SiAuto.Main.LogMessage(xml_data.ToString());
                        //IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("suaddr").Name))
                        {
                            string id = string.Empty;
                            id = XmlGetTagValue(xml_data, "suaddr");
                            if (id.Equals(string.Empty))
                                return;
                            else
                            {
                                htable.Add("suaddr", id);
                                //Console.WriteLine("suaddr:{0}", htable["suaddr"]);
                            }

                            
                            if (CheckIfUidExist(htable["suaddr"].ToString()))
                            { }
                            else
                            {
                                return;
                            }
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
                        //IEnumerable<XName> elements = XmlGetAllElementsXname(xml_data);
                        if (elements.Contains(new XElement("suaddr").Name))
                        {
                            string id = string.Empty;
                            id = XmlGetTagValue(xml_data, "suaddr");
                            if (id.Equals(string.Empty))
                                return;
                            else
                            {
                                htable.Add("suaddr", id);
                                //Console.WriteLine("suaddr:{0}", htable["suaddr"]);
                            }
                            if (CheckIfUidExist(htable["suaddr"].ToString()))
                            { }
                            else
                            {
                                return;
                            }
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
                    Console.WriteLine("ERROR:" + logData);
                    break;
            }
            
            //Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
            //using (StreamWriter w = File.AppendText("log.txt"))
            /*
            {
                foreach (DictionaryEntry ht in htable)
                {
                    Console.WriteLine("Key = {0}, Value = {1}" + Environment.NewLine, ht.Key, ht.Value);
                    log.Info("receive:\r\n"+ht.Key+"="+ht.Value);
                    // Close the writer and underlying file.
                   

                }
                //w.Close();
            }
            */
            htable.Clear();
            htable = null;
            sensor_name.Clear();
            sensor_name = null;
            sensor_value.Clear();
            sensor_value = null;
            sensor_type.Clear();
            sensor_type = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            
            Console.WriteLine();
            stopWatch.Stop();
            SiAuto.Main.LogMessage("xml_parse spend time(ms):"+stopWatch.ElapsedMilliseconds);
        }

        private static void Restart()
        {
            //Process.Start(AppDomain.CurrentDomain.BaseDirectory + "Client.exe");

            //some time to start the new instance.
            //Thread.Sleep(2000);

            //Environment.Exit(-1);//Force termination of the current process.
            
            System.Windows.Forms.Application.Restart();
            Process.GetCurrentProcess().Kill();
        }

        private static void GetInitialLocationFromSql(ref string lat, ref string lon, string id)
        {
            StackFrame frame = new StackFrame(1);
            var callerMethod = frame.GetMethod();
            var callerType = callerMethod.DeclaringType;
            var callerName = callerMethod.Name;
            var sqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            string sqlCmd = @"SELECT 
  sd.initial_location.lat,
  sd.initial_location.lon
FROM
  sd.initial_location
WHERE
  sd.initial_location.uid = '" + id + @"'";
            log.Info("GetInitialLocationFromSqllCmd=" + Environment.NewLine + sqlCmd);
            while (!sqlClient.connect())
            {
                Thread.Sleep(30);
            }
            
            DataTable dt = sqlClient.get_DataTable(sqlCmd);
            sqlClient.disconnect();
            sqlClient.Dispose();
			sqlClient=null;
            if (dt != null && dt.Rows.Count != 0)
            {

                foreach (DataRow row in dt.Rows)
                {
                    lat = row[0].ToString();
                    lon = row[1].ToString();
                }
            }
            else
            {
                lat = lon = "0";
                switch (id)
                {
                    case "000000":
                        break;
                    default:
                        log.Error("callerType:" + callerType);
                        log.Error("callerName:" + callerName);
                        log.Error("Cannot find lat lon of deviceID: " + id + " in sql table: sd.initial_location ");
                        break;
                }
                
            }
            dt.Dispose();
            dt = null;
        }
        /*
         * avls event list:
         * 175:Immediate-Location-Report
         * 182:msg(power_off):ABSENT SUBSCRIBER,Unit Absent
         * 181:msg(power_on):Unit Present
         * 1:INSUFFICIENT GPS SATELLITES,BAD GPS GEOMETRY,GPS INVALID
         * 0:Emergency Off,Ignition Off,Ignition On
         * -1:msg(power_off_over_1_hour):
         * 
         * avls message default:"null"
         * 
         * only for access sql table:custom.cga_event_log
         * event:2->msg:p_prohibited
         * event:3->msg:patrol_location
         * event:4->msg:p_prohibited,patrol_location
         * event:x.5->x stay over specific time
         * event:5->stay over specific time within 0.1 km
        */
        private static void access_avls_server(object o)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("+access_avls_server");
            Console.ResetColor();
            var oo = o as AvlsClass;

            string xml_root_tag = oo.XmlRootTag;
            Hashtable htable = oo.Htable;
            List<string> sensor_name = oo.SensorName;
            List<string> sensor_type = oo.SensorType;
            List<string> sensor_value = oo.SensorValue;
            HashSet<XName> iEnumerable = oo.Elements;
            string log = oo.Log;
            string getMessage = oo.GetMessage;
            //Console.WriteLine("+access_avls_server");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            string send_string = string.Empty;
            string initialLat = string.Empty, initialLon = string.Empty;
            AVLS_UNIT_Report_Packet avls_package = new AVLS_UNIT_Report_Packet();
            avls_package.Message = "null";
            
             

            

            //avls_tcpClient.NoDelay = false;

            //Keeplive.keep(avls_tcpClient.Client);
            //NetworkStream netStream = avlsNetworkStream;
            /*
            if (htable.ContainsKey("event_info"))
                if (htable["event_info"].ToString().Equals("Unit Absent"))
                {
                    netStream.Close();
                    avls_tcpClient.Close();
                    return;
                }
             */
            #region operation error
            if (iEnumerable.Contains(new XElement("operation-error").Name))
            {
                if (htable.ContainsKey("suaddr"))
                {
                    avls_package.ID = htable["suaddr"].ToString();
                    avls_package.Speed = "0,";
                    avls_package.Dir = "0,";
                    avls_package.Temp = "NA,";

                    avls_package.Event = "0,";
                    avls_package.Status = "00000000,";

                
                    if (xml_root_tag.Equals("Immediate-Location-Report"))
                    {
                        avls_package.Event = "175,";
                    }
                    if (htable.ContainsKey("result_msg"))
                    {
                        avls_package.Message = htable["result_msg"].ToString();
                        //-999 to -500 :motorola error
                        //-499 to -100 :our error
                        switch (htable["result_msg"].ToString())
                        {
                            case "ABSENT SUBSCRIBER":
                                avls_package.Event = "182,";
                                avls_package.Status = "00000000,";
                                avls_package.Message = "power_off";
                                break;
                                /*
                            case  "SYSTEM FAILURE":
                                avls_package.Event = "-500,";
                                break;
                            case "UNSPECIFIED ERROR":
                                avls_package.Event = "-501,";
                                break;
                            case "UNAUTHORIZED APPLICATION":
                                avls_package.Event = "-499,";
                                break;
                            case "CONGESTION IN MOBILE NETWORK":
                                avls_package.Event = "-502,";
                                break;
                            case "UNSUPPORTED VERSION":
                                avls_package.Event = "-498,";
                                break;
                            case "SYNTAX ERROR":
                                avls_package.Event = "-497,";
                                break;
                            case "SERVICE NOT SUPPORTED":
                                avls_package.Event = "-496,";
                                break;
                            case "QUERY INFO NOT CURRENTLY ATTAINABLE":
                                avls_package.Event = "-503,";
                                break;
                            case "REPORTING WILL STOP":
                                avls_package.Event = "-99,";
                                break;
                                */
                            case "INSUFFICIENT GPS SATELLITES":
                                avls_package.Event = "1,";
                                break;
                            case "BAD GPS GEOMETRY":
                                avls_package.Event = "1,";
                                break;
                            case "GPS INVALID":
                                avls_package.Event = "1,";
                                break;
                                /*
                            case "API DISCONNECTED":
                                avls_package.Event = "-495,";
                                break;
                            case "OPERA TION NOT PERMITTED":
                                avls_package.Event = "-494,";
                                break;
                            case "API NOT LICENSED":
                                avls_package.Event = "-493,";
                                break;
                                */
                        }
                    }
                    else
                    {
                        avls_package.Message = "null";
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

                    DateTime tempDatetime = DateTime.Now.ToUniversalTime();
                    avls_package.Date_Time = tempDatetime.ToString("yyMMddHHmmss") + ",";

                    if (bool.Parse(ConfigurationManager.AppSettings["avlsGetLastLocation"]))
                    {
                        var avlsSqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                        string avlsSqlCmd = @"SELECT 
  public._gps_log._lat,
  public._gps_log._lon
FROM
  public._gps_log
WHERE
  public._gps_log._time < now() AND 
  public._gps_log._uid = '" + avls_package.ID + @"'
ORDER BY
  public._gps_log._time DESC
LIMIT 1";
                        while (!avlsSqlClient.connect())
                        {
                            Thread.Sleep(30);
                        }
                        
                        DataTable dt = avlsSqlClient.get_DataTable(avlsSqlCmd);
                        avlsSqlClient.disconnect();
                        avlsSqlClient.Dispose();
						avlsSqlClient=null;
                        if (dt != null && dt.Rows.Count != 0)
                        {
                            string avlsLat = string.Empty, avlsLon = string.Empty;
                            foreach (DataRow row in dt.Rows)
                            {
                                initialLat=avlsLat = row[0].ToString();
                                initialLon=avlsLon = row[1].ToString();
                            }
                            string zero = "0";
                            if (avlsLat.Equals(zero) || avlsLon.Equals(zero))
                            {
                                GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                            }
                            //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                            //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                            //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                            //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                            //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                            string lat_str = avlsLat, long_str = avlsLon;
                            ConvertLocToAvlsLoc(ref lat_str, ref long_str); 
                            avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                        }
                        else
                        {
                            string avlsLat = string.Empty, avlsLon = string.Empty;
                            GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                            //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                            //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                            //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                            //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                            //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                            string lat_str = initialLat=avlsLat, long_str = initialLon=avlsLon;
                            ConvertLocToAvlsLoc(ref lat_str, ref long_str); 
                            avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                            //avls_package.Loc = "N00000.0000E00000.0000,";
                        }
                        dt.Dispose();
                        dt = null;
                        /*
                         * SELECT 
      public._gps_log._lat,
      public._gps_log._lon
    FROM
      public._gps_log
    WHERE
      public._gps_log._time < now() AND 
      public._gps_log._uid = 'avls_package.ID'
    ORDER BY
      public._gps_log._time DESC
    LIMIT 1
                         */
                    }
                    //check range of initialLat/initialLon in exclusion_area_boundary then send event by avls_package.Event
                    avls_package.ID += ",";
                    send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + avls_package.Date_Time + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + avls_package.Event + avls_package.Message + "\r\n";

                    //avlsSendDone.Reset();
                    //var sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                    avlsSendPackage = send_string;
                    avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                    SiAuto.Main.LogMessage(send_string);
                    //avlsSendDone.WaitOne();

                    //ReadLine(avls_tcpClient, netStream, send_string.Length);
                    //netStream.Close();
                    //avlsTcpClient.Close();
                    //Console.WriteLine("-access_avls_server");
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("-access_avls_server");
                    Console.ResetColor();
                    return;
                }
                else
                {
                    //netStream.Close();
                    //avlsTcpClient.Close();
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("-access_avls_server");
                    Console.ResetColor();
                    return;
                }

            }
            #endregion
            else
            {
                
                if (htable.ContainsKey("suaddr"))
                {
                    avls_package.ID = htable["suaddr"].ToString() ;
                }
                else
                {
                   // netStream.Close();
                    //avlsTcpClient.Close();
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("-access_avls_server");
                    Console.ResetColor();
                    return;
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
                    //Console.WriteLine(@"+if (htable.ContainsKey(""info_time""))");
                    //Console.WriteLine(htable["info_time"].ToString());
                    avls_package.Date_Time = htable["info_time"].ToString().Substring(2) + ",";
                    //Console.WriteLine(@"-if (htable.ContainsKey(""info_time""))");
                }
                else
                {
                    DateTime tempDatetime = DateTime.Now.ToUniversalTime();
                    avls_package.Date_Time = tempDatetime.ToString("yyMMddHHmmss") + ",";
                    //avls_package.Date_Time = string.Format("{0:yyMMddHHmmss}", DateTime.Now) + ",";
                }
                ///TODO:implement set last lat lon value    
                if (htable.ContainsKey("lat_value") && htable.ContainsKey("long_value"))
                {
                    //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(htable["lat_value"]));
                    //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(htable["long_value"]));
                    //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                    //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                    //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                    string lat_str = initialLat=(string) htable["lat_value"], long_str =initialLon= (string) htable["long_value"];
                    ConvertLocToAvlsLoc(ref lat_str, ref long_str); 
                    avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                    last_avls_lat = lat_str;
                    last_avls_lon = long_str;
                }
                else
                {
                    //avls_tcpClient.Close();
                    //return;
                    //avls_package.Loc = "N" + last_avls_lat + "E" + last_avls_lon + ",";
                    if (bool.Parse(ConfigurationManager.AppSettings["avlsGetLastLocation"]))
                    {
                        var avlsSqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
                        string avlsSqlCmd = @"SELECT 
  public._gps_log._lat,
  public._gps_log._lon
FROM
  public._gps_log
WHERE
  public._gps_log._time < now() AND 
  public._gps_log._uid = '" + avls_package.ID + @"'
ORDER BY
  public._gps_log._time DESC
LIMIT 1";
                        while (!avlsSqlClient.connect())
                        {
                            Thread.Sleep(30);
                        }
                        DataTable dt = avlsSqlClient.get_DataTable(avlsSqlCmd);
                        avlsSqlClient.disconnect();
                        avlsSqlClient.Dispose();
						avlsSqlClient=null;
                        if (dt != null && dt.Rows.Count != 0)
                        {
                            string avlsLat = string.Empty, avlsLon = string.Empty;
                            foreach (DataRow row in dt.Rows)
                            {
                                avlsLat = row[0].ToString();
                                avlsLon = row[1].ToString();
                            }
                            string zero = "0";
                            if (avlsLat.Equals(zero) || avlsLon.Equals(zero))
                            {
                                GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                            }
                            //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                            //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                            //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                            //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                            //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                            string lat_str = initialLat = avlsLat, long_str = initialLon = avlsLon;
                            ConvertLocToAvlsLoc(ref lat_str, ref long_str); 
                            avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                        }
                        else
                        {
                            string avlsLat = string.Empty, avlsLon = string.Empty;
                            GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                            //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                            //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                            //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                            //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                            //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                            string lat_str =initialLat= avlsLat, long_str =initialLon= avlsLon;
                            ConvertLocToAvlsLoc(ref lat_str, ref long_str); 
                            avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                            //avls_package.Loc = "N00000.0000E00000.0000,";
                        }
                        dt.Dispose();
                        dt = null;
                        /*
                         * SELECT 
      public._gps_log._lat,
      public._gps_log._lon
    FROM
      public._gps_log
    WHERE
      public._gps_log._time < now() AND 
      public._gps_log._uid = 'avls_package.ID'
    ORDER BY
      public._gps_log._time DESC
    LIMIT 1
                         */
                    }
                    else
                    {
                        string avlsLat = string.Empty, avlsLon = string.Empty;
                        GetInitialLocationFromSql(ref avlsLat, ref avlsLon, avls_package.ID);
                        //GeoAngle lat_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLat));
                        //GeoAngle long_value = GeoAngle.FromDouble(Convert.ToDecimal(avlsLon));
                        //string lat_str = lat_value.Degrees.ToString() + lat_value.Minutes.ToString("D2") + "." + lat_value.Seconds.ToString("D2") + lat_value.Milliseconds.ToString("D3");
                        //string long_str = long_value.Degrees.ToString() + long_value.Minutes.ToString("D2") + "." + long_value.Seconds.ToString("D2") + long_value.Milliseconds.ToString("D3");
                        //avls_package.Loc = "N" + (Convert.ToDouble(htable["lat_value"])*100).ToString() + "E" + (Convert.ToDouble(htable["long_value"])*100).ToString()+ ",";
                        string lat_str = initialLat=avlsLat, long_str =initialLon= avlsLon;
                        ConvertLocToAvlsLoc(ref lat_str, ref long_str); 
                        avls_package.Loc = "N" + lat_str + "E" + long_str + ",";
                        //avls_package.Loc = "N00000.0000E00000.0000,";
                    }
                        
                }
                if (htable.ContainsKey("speed-hor"))
                {
                    avls_package.Speed = Convert.ToInt32((double.Parse(htable["speed-hor"].ToString()) * 3.6)).ToString() + ",";
                }
                else
                {
                    avls_package.Speed = "0,";
                }
                if (htable.ContainsKey("direction-hor"))
                {
                    avls_package.Dir = htable["direction-hor"].ToString() + ",";
                }
                else
                {
                    avls_package.Dir = "0,";
                }
                avls_package.Temp = "NA,";
                if (htable.ContainsKey("event_info"))
                {


                    switch (htable["event_info"].ToString())
                    {
                        case "Emergency On":
                            avls_package.Event = "150,";
                            avls_package.Status = "00000000,";
                            avls_package.Message = htable["event_info"].ToString();
                            break;
                        case "Emergency Off":
                            avls_package.Event = "0,";
                            avls_package.Status = "00000000,";
                            avls_package.Message = htable["event_info"].ToString();
                            break;
                        case "Unit Present":
                            avls_package.Event = "181,";
                            avls_package.Status = "00000000,";
                            avls_package.Message = "power_on";
                            avlsAccessCount++;
                            //netStream.Close();
                            //avls_tcpClient.Close();
                            //return;
                            break;
                        case "Unit Absent":
                            avls_package.Event = "182,";
                            avls_package.Status = "00000000,";
                            avls_package.Message = "power_off";
                            avlsAccessCount++;
                            //netStream.Close();
                            //avls_tcpClient.Close();
                            //return;
                            break;
                        case "Ignition Off":
                            avls_package.Event = "0,";
                            avls_package.Status = "00000000,";
                            avls_package.Message = htable["event_info"].ToString();
                            break;
                        case "Ignition On":
                            avls_package.Event = "0,";
                            avls_package.Status = "00020000,";
                            avls_package.Message = htable["event_info"].ToString();
                            break;

                    }
                }
                else
                {
                    avls_package.Event = "0,";
                    avls_package.Status = "00000000,";
                }
                if (xml_root_tag.Equals("Immediate-Location-Report"))
                {
                    avls_package.Event = "175,";
                }

            }
            string deviceID = string.Empty;
            deviceID = avls_package.ID;
            avls_package.ID += ",";

            send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + avls_package.Date_Time + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + avls_package.Event + avls_package.Message + "\r\n";
            avlsSendPackage = send_string;
            avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
            SiAuto.Main.LogMessage(send_string);
            avlsFlag = true;

            var deviceChar = deviceID.ToCharArray();
            if (!deviceChar[3].Equals('0')  && avls_package.Event == "0,")
            {
                string now = string.Format("{0:yyMMddHHmmss}", DateTime.Now.ToUniversalTime()) + ",";
                #region send specific msg
                //check range of initialLat/initialLon in exclusion_area_boundary then send event by avls_package.Event

                //List<EAB> prohibitedList, locationList;
                string prohibitedTableName = string.Empty, locationTableName = string.Empty;
                //prohibitedTableName = "public.prohibited";
                //locationTableName = "public.patrol_location";
                //GetRidAndGeomFromSqlTable(prohibitedTableName, out prohibitedList);
                //GetRidAndGeomFromSqlTable(locationTableName, out locationList);

                prohibitedTableName = "public.p_prohibited";
                locationTableName = "public.patrol_location";
                //string getMessage = string.Empty;
                //lock (getGidAndFullnameLock)
                {
                    send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + now + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + "0,";
                    //getMessage = GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql(prohibitedTableName,
                    //locationTableName,
                    //initialLat, initialLon, deviceID, false);
                    //if (getMessage.Contains("p_prohibited"))
                        //SiAuto.Main.LogMessage(getMessage);

                    if (!string.IsNullOrEmpty(getMessage))
                    {
                        send_string += getMessage + "\r\n";
                        //if (getMessage.Contains("p_prohibited"))
                            //SiAuto.Main.LogMessage(send_string);
                        avlsSendPackage = send_string;
                        avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                        SiAuto.Main.LogMessage(send_string);
                    }

                    send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + now + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + "0,";
                    getMessage = CheckIfStayOverTime(initialLat, initialLon, deviceID);
                    if (!string.IsNullOrEmpty(getMessage))
                    {
                        switch (getMessage)
                        {
                            case "in": //stay over time
                                send_string += @";stay_over_specific_time" + "\r\n";
                                avlsSendPackage = send_string;
                                avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                                SiAuto.Main.LogMessage(send_string);
                                break;
                        }

                    }
                }
                #endregion  send specific msg
            }

            

            
            /*
            if (avlsAccessCount > deviceCount || avlsFlag)
            {
                lock (IsFirstExecuteLock)
                {
                    if (bool.Parse(ConfigurationManager.AppSettings["IsFirstExecute"]))
                    {
                        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                        //make changes
                        config.AppSettings.Settings["IsFirstExecute"].Value = "false";

                        //save to apply changes
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                    }
               
                }


                avls_WriteLine(netStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                avlsFlag = true;

                var deviceChar = deviceID.ToCharArray();
                if (!deviceChar[3].Equals('0'))
                {
                    #region send specific msg
                    //check range of initialLat/initialLon in exclusion_area_boundary then send event by avls_package.Event

                    //List<EAB> prohibitedList, locationList;
                    string prohibitedTableName = string.Empty, locationTableName = string.Empty;
                    //prohibitedTableName = "public.prohibited";
                    //locationTableName = "public.patrol_location";
                    //GetRidAndGeomFromSqlTable(prohibitedTableName, out prohibitedList);
                    //GetRidAndGeomFromSqlTable(locationTableName, out locationList);

                    prohibitedTableName = "public.p_prohibited";
                    locationTableName = "public.patrol_location";
                    //string getMessage = string.Empty;
                    lock (getGidAndFullnameLock)
                    {
                        send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + avls_package.Date_Time + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + avls_package.Event;
                        //getMessage = GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql(prohibitedTableName,
                            //locationTableName,
                            //initialLat, initialLon, deviceID, false);
                        if (getMessage.Contains("p_prohibited"))
                            SiAuto.Main.LogMessage(getMessage);

                        if (!string.IsNullOrEmpty(getMessage))
                        {
                            send_string += getMessage + "\r\n";
                            if (getMessage.Contains("p_prohibited"))
                                SiAuto.Main.LogMessage(send_string);
                            avls_WriteLine(netStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                        }

                        send_string = "%%" + avls_package.ID + avls_package.GPS_Valid + avls_package.Date_Time + avls_package.Loc + avls_package.Speed + avls_package.Dir + avls_package.Temp + avls_package.Status + avls_package.Event;
                        getMessage = CheckIfStayOverTime(initialLat, initialLon, deviceID);
                        if (!string.IsNullOrEmpty(getMessage))
                        {
                            switch (getMessage)
                            {
                                case "in": //stay over time
                                    send_string += @";stay_over_specific_time" + "\r\n";
                                    avls_WriteLine(netStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                                    break;
                            }

                        }
                    }
                    #endregion  send specific msg
                }
                
            }
            else
            {
                if (!xml_root_tag.Equals("Unsolicited-Location-Report"))
                {
                    avls_WriteLine(netStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                }
                if (bool.Parse(ConfigurationManager.AppSettings["IsFirstExecute"]))
                {
                    avls_WriteLine(netStream, System.Text.Encoding.UTF8.GetBytes(send_string), send_string);
                }
                
            }
            */
            //avlsSendDone.WaitOne();

            //ReadLine(avls_tcpClient, netStream, send_string.Length);
            //netStream.Close();
            //avlsTcpClient.Close();
            htable.Clear();
            htable = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //Console.WriteLine("-access_avls_server");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("-access_avls_server");
            Console.ResetColor();
        }

        private static string GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql(string prohibitedTableName, string locationTableName, string initialLat, string initialLon,string id,bool isStayTimeEnable,string deviceID)
        {
            object mylock = new object();
            string startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string datalogicFilePath = Path.Combine(startupPath, "StayCheck.sdf");
            string connString = string.Format("Data Source={0}", datalogicFilePath);
            string message = string.Empty;
            StayCheck sqlCEdb;
            try
            {
                sqlCEdb = new StayCheck(connString);
            }
            catch (Exception ex)
            {
                
                SiAuto.Main.LogError(ex.ToString());
                return string.Empty;
            }
            string searchID = string.Empty, searchID2 = string.Empty;
            double  stayTimeInMin =0;
            
            try
            {
                searchID = (from p in sqlCEdb.CheckIfOverTime where p.Uid == id select p.Uid).First();
                searchID2 = (from p in sqlCEdb.CheckIfOverTime2 where p.Uid == id select p.Uid).First();
            }
            catch (Exception)
            {
                try
                {
                    if (string.IsNullOrEmpty(searchID))
                    {
                        //not found id in sql->add new row with id
                        CheckIfOverTime newRow = new CheckIfOverTime();
                        newRow.Uid = id;
                        sqlCEdb.CheckIfOverTime.InsertOnSubmit(newRow);
                        sqlCEdb.SubmitChanges();
                    }
                    if (string.IsNullOrEmpty(searchID2))
                    {
                        //not found id in sql->add new row with id
                        CheckIfOverTime2 newRow2 = new CheckIfOverTime2();
                        newRow2.Uid = id;
                        sqlCEdb.CheckIfOverTime2.InsertOnSubmit(newRow2);
                        sqlCEdb.SubmitChanges();
                    }
                }
                catch (Exception ex)
                {

                    SiAuto.Main.LogError(ex.ToString());
                }
                
            }
            
            string DB = string.Empty;
            DB = "lmap100";
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], DB, ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            string regSqlCmdForProhibitedTable = string.Empty;
            string regSqlCmdForLocationTable = string.Empty;
            regSqlCmdForProhibitedTable = @"select gid, fullname
from p_prohibited
where st_intersects(the_geom, st_geomfromtext('POINT(" +initialLon+" "+initialLat+ @")', 4326)) AND
now() >= start_time::timestamp AND
now() <= end_time::timestamp ";
            regSqlCmdForLocationTable = @"select gid, fullname
from patrol_location
where st_intersects(st_buffer(the_geom, 0.0009009/100*raidus), st_geomfromtext('POINT(" + initialLon + " " + initialLat + @")', 4326))AND
now() >= start_time::timestamp AND
now() <= end_time::timestamp ";

            log.Info(deviceID+":p_prohibited:sql cmd:" + Environment.NewLine + regSqlCmdForProhibitedTable);
            log.Info(deviceID + ":patrol_location:sql cmd:" + Environment.NewLine + regSqlCmdForLocationTable);
            while (!sql_client.connect())
            {
                Thread.Sleep(30);
            }
            DataTable dt = sql_client.get_DataTable(regSqlCmdForProhibitedTable);
            //sql_client.disconnect();
            //List<EAB2> prohibitedEab2s= new List<EAB2>();
            if (dt != null && dt.Rows.Count != 0)
            {
                //SiAuto.Main.AddCheckpoint(Level.Debug,id+"-find data from sql", regSqlCmdForProhibitedTable);
                try
                {
                    CheckIfOverTime getRow = sqlCEdb.CheckIfOverTime.FirstOrDefault(p => p.CreateTime == null && p.Uid == id);
                    if (getRow != null)
                    {
                        //SiAuto.Main.AddCheckpoint(Level.Debug, id+" assign time");
                        getRow.CreateTime = DateTime.Now;
                        sqlCEdb.SubmitChanges();
                        #region send with prohibite data



                        foreach (DataRow row in dt.Rows)
                        {
                            //prohibitedEab2s.Add(new EAB2("p_prohibited", row[0].ToString(), row[1].ToString()));
                            //lock (mylock)
                            {
                                message += ";" + "p_prohibited" + "#" + row[0].ToString() + "#" + row[1].ToString();
                            }

                        }
                        #endregion send with prohibite data
                    }
                    else
                    {
                        //SiAuto.Main.AddCheckpoint(Level.Debug, id+" has time");
                        //table:p_config
                        //column:stay_time
                        //unit:min

                        //check if over time 
                        //over->send msg with prohibited data
                        //not over -> do nothing
                        string sqlCmd = @"select stay_time from p_config LIMIT 1";

                        //while (!sql_client.connect())
                        {
                            //Thread.Sleep(30);
                        }
                        DataTable dt2 = sql_client.get_DataTable(sqlCmd);
                        //sql_client.disconnect();
                        if (dt2 != null && dt2.Rows.Count != 0)
                        {
                            foreach (DataRow row in dt2.Rows)
                            {
                                stayTimeInMin = double .Parse(row[0].ToString());
                            }
                        }
                        dt2.Dispose();
                        dt2 = null;
                        //SiAuto.Main.WatchDouble(Level.Debug, "stayTimeInMin", stayTimeInMin);
                        DateTime getTime = new DateTime();
                        var dateTime = (from p in sqlCEdb.CheckIfOverTime where p.Uid == id select p.CreateTime).FirstOrDefault();
                        if (dateTime != default(DateTime))
                            getTime = dateTime.Value;
                        //SiAuto.Main.WatchDateTime(Level.Debug, "getTime", getTime);
                        int result;
                        result = DateTime.Compare(DateTime.Now, getTime.AddMinutes(stayTimeInMin));
                        //SiAuto.Main.LogText(Level.Debug, id + "-result-" + result,
                            //DateTime.Now + "--" + getTime.AddMinutes(stayTimeInMin));
                        if (isStayTimeEnable)
                        {
                            if (result > 0)
                            {
                                #region send with prohibite data



                                foreach (DataRow row in dt.Rows)
                                {
                                    //prohibitedEab2s.Add(new EAB2("p_prohibited", row[0].ToString(), row[1].ToString()));
                                    //lock (mylock)
                                    {
                                        message += ";" + "p_prohibited" + "#" + row[0].ToString() + "#" + row[1].ToString();
                                    }

                                }
                                #endregion send with prohibite data
                            }
                        }
                        else
                        {
                            #region send with prohibite data



                            foreach (DataRow row in dt.Rows)
                            {
                                //prohibitedEab2s.Add(new EAB2("p_prohibited", row[0].ToString(), row[1].ToString()));
                                //lock (mylock)
                                {
                                    message += ";" + "p_prohibited" + "#" + row[0].ToString() + "#" + row[1].ToString();
                                }

                            }
                            #endregion send with prohibite data
                        }
                        

                        
                    }
                   
                }
                catch (Exception ex)
                {

                    SiAuto.Main.LogText(Level.Debug, id+":sqlCEException", ex.ToString());
                }
                
                
              
            }
            else
            {
                try
                {
                    CheckIfOverTime getRow = sqlCEdb.CheckIfOverTime.FirstOrDefault(p => p.CreateTime != null && p.Uid == id);
                    if (getRow != null)
                    {
                        //SiAuto.Main.AddCheckpoint(Level.Debug,id+" remove time");
                        getRow.CreateTime = null;
                        sqlCEdb.SubmitChanges();

                        #region send with prohibite data



                       // foreach (DataRow row in dt.Rows)
                        {
                            //prohibitedEab2s.Add(new EAB2("p_prohibited", row[0].ToString(), row[1].ToString()));
                            //lock (mylock)
                            {
                                message += ";" + "p_prohibited_out";
                                //SiAuto.Main.LogMessage(message);
                            }

                        }
                        #endregion send with prohibite data
                    }
                }
                catch (Exception ex)
                {

                    SiAuto.Main.LogError(id + "sqlce excep 1", ex.ToString());
                }
                
            }

            //while (!sql_client.connect())
            {
                //Thread.Sleep(30);
            }
            dt = sql_client.get_DataTable(regSqlCmdForLocationTable);
            //sql_client.disconnect();
            //List<EAB2> locationEab2s = new List<EAB2>();
            if (dt != null && dt.Rows.Count != 0)
            {
                //SiAuto.Main.AddCheckpoint(Level.Debug, id + "-find data from sql", regSqlCmdForLocationTable);
                try
                {
                    CheckIfOverTime2 getRow = sqlCEdb.CheckIfOverTime2.FirstOrDefault(p => p.CreateTime == null && p.Uid == id);
                    if (getRow != null)
                    {
                        //SiAuto.Main.AddCheckpoint(Level.Debug, id + " assign time");
                        getRow.CreateTime = DateTime.Now;
                        sqlCEdb.SubmitChanges();
                        #region send with location data
                        foreach (DataRow row in dt.Rows)
                        {
                            //locationEab2s.Add(new EAB2("patrol_location", row[0].ToString(), row[1].ToString()));
                            //lock (mylock)
                            {
                                message += ";" + "patrol_location" + "#" + row[0].ToString() + "#" + row[1].ToString();
                            }

                        }
                        #endregion send with location data
                    }
                    else
                    {
                        //SiAuto.Main.AddCheckpoint(Level.Debug, id + " has time");
                        //table:p_config
                        //column:stay_time
                        //unit:min

                        //check if over time 
                        //over->send msg with prohibited data
                        //not over -> do nothing

                        string sqlCmd = @"select stay_time from p_config LIMIT 1";

                        //while (!sql_client.connect())
                        {
                            //Thread.Sleep(30);
                        }
                        DataTable dt2 = sql_client.get_DataTable(sqlCmd);
                        sql_client.disconnect();
                        if (dt2 != null && dt2.Rows.Count != 0)
                        {
                            foreach (DataRow row in dt2.Rows)
                            {
                                stayTimeInMin = double.Parse(row[0].ToString());
                            }
                        }
                        dt2.Dispose();
                        dt2 = null;
                        //SiAuto.Main.WatchDouble(Level.Debug, "stayTimeInMin", stayTimeInMin);
                        DateTime getTime = new DateTime();
                        var dateTime = (from p in sqlCEdb.CheckIfOverTime2 where p.Uid == id select p.CreateTime).FirstOrDefault();
                        if (dateTime != default(DateTime))
                            getTime = dateTime.Value;
                        //SiAuto.Main.WatchDateTime(Level.Debug, "getTime", getTime);
                        int result;
                        result = DateTime.Compare(DateTime.Now, getTime.AddMinutes(stayTimeInMin));
                        //SiAuto.Main.LogText(Level.Debug, id + "-result-" + result,
                            //DateTime.Now + "--" + getTime.AddMinutes(stayTimeInMin));
                        if (isStayTimeEnable)
                        {
                            if (result > 0)
                            {

                                #region send with location data
                                foreach (DataRow row in dt.Rows)
                                {
                                    //locationEab2s.Add(new EAB2("patrol_location", row[0].ToString(), row[1].ToString()));
                                    //lock (mylock)
                                    {
                                        message += ";" + "patrol_location" + "#" + row[0].ToString() + "#" + row[1].ToString();
                                    }

                                }
                                #endregion send with location data
                            }
                        }
                        else
                        {
                            #region send with location data
                            foreach (DataRow row in dt.Rows)
                            {
                                //locationEab2s.Add(new EAB2("patrol_location", row[0].ToString(), row[1].ToString()));
                                //lock (mylock)
                                {
                                    message += ";" + "patrol_location" + "#" + row[0].ToString() + "#" + row[1].ToString();
                                }

                            }
                            #endregion send with location data
                        }
                        

                    }
                }
                catch (Exception ex)
                {

                    SiAuto.Main.LogError(id + ":sqlce excep 3", ex.ToString() );
                }
                
            }
            else
            {
                try
                {
                    CheckIfOverTime2 getRow = sqlCEdb.CheckIfOverTime2.FirstOrDefault(p => p.CreateTime != null && p.Uid == id);
                    if (getRow != null)
                    {
                        //SiAuto.Main.AddCheckpoint(Level.Debug, id + " remove time");
                        getRow.CreateTime = null;
                        sqlCEdb.SubmitChanges();

                        #region send with location data
                        //foreach (DataRow row in dt.Rows)
                        {
                            //locationEab2s.Add(new EAB2("patrol_location", row[0].ToString(), row[1].ToString()));
                            //lock (mylock)
                            {
                                //message += ";" + "patrol_location" + "#" + "lout";
                            }

                        }
                        #endregion send with location data
                    }
                }
                catch (Exception ex)
                {

                    SiAuto.Main.LogError(id + "sqlce excep 2", ex.ToString() );
                }
                
            }
            dt.Dispose();
            dt = null;
            sql_client.Dispose();
			sql_client=null;
            //sql_client = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //if (message.Contains("p_prohibited"))
                //SiAuto.Main.LogMessage(message);
            return message;
        }

        /*
        private static void GetRidAndGeomFromSqlTable(string table, out List<EAB> myList)
        {
            string regSqlCmd = string.Empty;
            regSqlCmd = @"SELECT 
  "+table+@".rid,
  " + table + @".the_geom
FROM
  "+table;
            myList = new List<EAB>();
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            while (!sql_client.connect())
            {
                Thread.Sleep(30);
            }
            DataTable dt = sql_client.get_DataTable(regSqlCmd);
                            sql_client.disconnect();
            sql_client.Dispose();
			sql_client=null;
            if (dt != null && dt.Rows.Count != 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    myList.Add(new EAB(row[0].ToString(), row[1].ToString()));
                }
            }
            dt.Dispose();
            dt = null;
        }
        */
        private static void avls_WriteLine(NetworkStream netStream, byte[] writeData, string write)
        {
            if (netStream.CanWrite)
            {
                //byte[] writeData = Encoding.ASCII.GetBytes(write);
                try
                {
                    
                    //Console.WriteLine("S----------------------------------------------------------------------------");
                    //Console.WriteLine("Write:\r\n" + write);
                    //Console.WriteLine("E----------------------------------------------------------------------------");

                    //using (StreamWriter w = File.AppendText("log.txt"))
                    {
                        log.Info("Write:\r\n"+write);
                        // Close the writer and underlying file.
                        //w.Close();
                    }

                    //send method1
                    //netStream.Write(writeData, 0, writeData.Length);
                    // 需等待資料真的已寫入 NetworkStream
                    //Thread.Sleep(3000);
                    unsConnectDone.WaitOne();
                    //send method2
                    IAsyncResult result = netStream.BeginWrite(writeData, 0, writeData.Length, new AsyncCallback(avls_myWriteCallBack), netStream);
                    result.AsyncWaitHandle.WaitOne();
                    result.AsyncWaitHandle.Close();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("S----------------------------------------------------------------------------");
                    Console.WriteLine("avls write:\r\n" + write);
                    Console.WriteLine("E----------------------------------------------------------------------------");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("avls_WriteLineError:\r\n" + ex);
                    //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                    //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                    log.Error("avls_WriteLineError:\r\n" + ex);

                    if (avlsNetworkStream != null)
                        avlsNetworkStream.Close();
                    if (avlsTcpClient != null)
                        avlsTcpClient.Close();
                    avlsTcpClient = new TcpClient();
                    avlsConnectDone.Reset();
                    avlsTcpClient.BeginConnect(avls_ipaddress, avls_port, new AsyncCallback(AvlsConnectCallback), avlsTcpClient);
                    avlsConnectDone.WaitOne();
                    Keeplive.keep(avlsTcpClient.Client);
                    if (avlsTcpClient != null && avlsTcpClient.Client != null)
                    {
                        avlsNetworkStream = avlsTcpClient.GetStream();
                        avls_WriteLine(avlsNetworkStream, System.Text.Encoding.UTF8.GetBytes(avlsSendPackage), avlsSendPackage);
                        SiAuto.Main.LogMessage(avlsSendPackage);
                    }
                }


            }
        }
        public struct AUTO_SQL_DATA
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
        struct MANUAL_SQL_DATA
        {
            public string _id;
            public string event_id;
            public string application_id;
            public string request_id;
            public string result_code;
            public string result_msg;
            public string eqp_id;
            public string eqp_lat;
            public string eqp_lon;
            public string eqp_speed;
            public string eqp_course;
            public string eqp_distance;
            public string option1;
            public string option2;
            public string option3;
            public string option4;
            public string option5;
            public string option6;
            public string option7;
            public string option8;
            public string note;
            public string create_user;
        }
        enum device_status
        {
            MV,TK,EM,PE,UL
        }

        private static void access_sql_server(object o)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("+access_sql_server");
                Console.ResetColor();
            //Console.WriteLine("+access_sql_server");
                var oo = o as SqlClass;

                string xml_root_tag = oo.XmlRootTag;
                Hashtable htable = oo.Htable;
                List<string> sensor_name = oo.SensorName;
                List<string> sensor_type = oo.SensorType;
                List<string> sensor_value = oo.SensorValue;
                HashSet<XName> elements = oo.Elements;
                string log1 = oo.Log1;
                string getMessage = oo.GetMessage;
                oo = null;
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"],
                ConfigurationManager.AppSettings["SQL_SERVER_PORT"],
                ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"],
                ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"],
                ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"],
                ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"],
                ConfigurationManager.AppSettings["ConnectionLifetime"]);
            DateTime dtime = DateTime.Now;
            AUTO_SQL_DATA gps_log = new AUTO_SQL_DATA();
            MANUAL_SQL_DATA operation_log = new MANUAL_SQL_DATA();
            gps_log._or_lat = gps_log._or_lon = gps_log._satellites = gps_log._temperature = gps_log._voltage = "0";
            string now = string.Format("{0:yyyyMMdd}", dtime);
            gps_log._time = "\'" + string.Format("{0:yyyyMMdd HH:mm:ss.fff}", dtime) + "+08" + "\'";

            

            //Console.WriteLine("operationLogIdCount:" + operationLogIdCount);
            
            //Console.WriteLine("operationLogIdCount:" + operationLogIdCount);
            operation_log.request_id = "\'" + ConfigurationManager.AppSettings["request-id"].ToString() + "\'";

            if (htable.ContainsKey("protocol_version"))
            {
                operation_log.option3 = "\'" + htable["protocol_version"].ToString() + "\'";
            }
            if (htable.ContainsKey("app_id"))
            {
                operation_log.application_id = "\'" + htable["app_id"].ToString() + "\'";
            }
            else
                operation_log.application_id = "\'" + "null" + "\'";
            string deviceID = string.Empty;
                //double operationLogIdCount;
                /*
                //lock (gpsLogAccessSqlLock)
                {
                    #region sql get/set id
                    while (!sql_client.connect())
                    {
                        Thread.Sleep(30);
                    }
                    string _gps_logUidCount = string.Empty;
                    if (htable.ContainsKey("suaddr"))
                    {
                        _gps_logUidCount =
                    sql_client.get_DataTable("SELECT COUNT(_uid)   FROM public._gps_log WHERE _uid ='" + htable["suaddr"] as string + "\'" +
                    " AND public._gps_log._time::Date = current_date").Rows[0].ItemArray[0].ToString();
                        sql_client.disconnect();
                    }


                    while (!sql_client.connect())
                    {
                        Thread.Sleep(30);
                    }
                    operationLogIdCount =
                        Convert.ToDouble(
                            sql_client.get_DataTable("SELECT COUNT(_id)   FROM public.operation_log").Rows[0].ItemArray[0]
                                .ToString());
                    sql_client.disconnect();
                    operationLogIdCount.ToString("000000000000");
                    if (htable.ContainsKey("suaddr"))
                    {
                        deviceID = htable["suaddr"] as string;
                        gps_log._uid = operation_log.eqp_id = "\'" + deviceID + "\'";
                        gps_log._id = "\'" + deviceID + "_" + now + "_" + _gps_logUidCount + "\'";
                        operation_log._id = "\'" + "operation" + "_" + now + "_" + operationLogIdCount + "\'";
                    }
                    else
                    {
                        gps_log._uid = operation_log.eqp_id = "\'" + "null" + "\'";
                        gps_log._id = "\'" + Convert.ToBase64String(System.Guid.NewGuid().ToByteArray()) + "_" +
                                      _gps_logUidCount + "\'";
                        //operation_log._id = "\'" + Convert.ToBase64String(System.Guid.NewGuid().ToByteArray()) + "_" + manual_id_serial_command + "\'";
                        operation_log._id = "\'" + "operation" + "_" + now + "_" + operationLogIdCount + "\'";
                    }
                    #endregion sql get/set id
                }
            */
                if (htable.ContainsKey("suaddr"))
                {
                    deviceID = htable["suaddr"] as string;
                    gps_log._uid = operation_log.eqp_id = "\'" + deviceID + "\'";
                    //gps_log._id = "\'" + deviceID + "_" + now + "_" + _gps_logUidCount + "\'";
                    //operation_log._id = "\'" + "operation" + "_" + now + "_" + operationLogIdCount + "\'";
                    gps_log._id = "\'" + deviceID + "_" + now + "\'";
                    operation_log._id = "\'" + "op" + "_" + now + "\'";
                }
                else
                {
                    gps_log._uid = operation_log.eqp_id = "\'" + "null" + "\'";
                    gps_log._id = "\'" + "null" + "_" + now + "\'";
                    operation_log._id = "\'" + "op" + "_" + now + "\'";
                    //gps_log._id = "\'" + Convert.ToBase64String(System.Guid.NewGuid().ToByteArray()) + "_" +
                    //"\'";
                    //operation_log._id = "\'" + Convert.ToBase64String(System.Guid.NewGuid().ToByteArray()) + "_" + manual_id_serial_command + "\'";
                    //operation_log._id = "\'" + deviceID + "_" + now + "_"  + "\'";
                }
            if (htable.ContainsKey("result_code"))
            {
                gps_log._option2 = operation_log.result_code = "\'" + htable["result_code"].ToString() + "\'";
                gps_log._option3 =
                    operation_log.result_msg =
                        "\'" + ConfigurationManager.AppSettings["RESULT_CODE_" + htable["result_code"].ToString()] +
                        "\'";
            }
            else
                gps_log._option2 =
                    gps_log._option3 = operation_log.result_code = operation_log.result_msg = "\'" + "null" + "\'";
            //if (htable.ContainsKey("result_msg"))
            //{
            //    gps_log._option3 = "\'"+htable["result_msg"].ToString()+"\'";
            //}
            #region
            if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                {
                    string sqlCmd1 = string.Empty;
                    sqlCmd1 = @"SELECT 
  custom.uns_deivce_power_status.uid
FROM
   custom.uns_deivce_power_status
WHERE
  custom.uns_deivce_power_status.uid = " + @"'" + deviceID+@"'";

                    while (!sql_client.connect())
                    {
                        Thread.Sleep(30);
                    }
                    DataTable dt1 = sql_client.get_DataTable(sqlCmd1);
                    //sql_client.disconnect();

                    if (dt1 != null && dt1.Rows.Count != 0)
                    {
                        
                    }
                    else
                    {
                        

                            sqlCmd1 = "INSERT INTO custom.uns_deivce_power_status (uid) VALUES (" + "\'" + deviceID + "\'" +
                                      ")";
                            //while (!sql_client.connect())
                            {
                                //Thread.Sleep(30);
                            }
                            sql_client.modify(sqlCmd1);
                            //sql_client.disconnect();

                    }
                dt1.Dispose();
                    dt1 = null;
                }

                #endregion
            if (htable.ContainsKey("result_msg"))
            {
                //avls_package.Message = htable["result_msg"].ToString();
                //-999 to -500 :motorola error
                //-499 to -100 :our error
                string unsUpdateTimeStamp = string.Empty;
                string unsSqlCmd = string.Empty;
                switch (htable["result_msg"].ToString())
                {
                    case "ABSENT SUBSCRIBER":

                        #region access power status

                    {
                        if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                        {
                             unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                             unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = 'off',
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + deviceID + @"'" + @" AND 
  (custom.uns_deivce_power_status.power <> 'off' OR 
  custom.uns_deivce_power_status.power IS NULL)";
                            //while (!sql_client.connect())
                            {
                                //Thread.Sleep(30);
                            }
                            //lock (access_uns_deivce_power_status_Lock)
                            sql_client.modify(unsSqlCmd);
                            //sql_client.disconnect();
                        }
                    }

                        #endregion

                        //avls_package.Event = "182,";
                        //avls_package.Status = "00000000,";
                        //avls_package.Message = "power_off";
                        break;
                        /*
                case  "SYSTEM FAILURE":
                    avls_package.Event = "-500,";
                    break;
                case "UNSPECIFIED ERROR":
                    avls_package.Event = "-501,";
                    break;
                case "UNAUTHORIZED APPLICATION":
                    avls_package.Event = "-499,";
                    break;
                case "CONGESTION IN MOBILE NETWORK":
                    avls_package.Event = "-502,";
                    break;
                case "UNSUPPORTED VERSION":
                    avls_package.Event = "-498,";
                    break;
                case "SYNTAX ERROR":
                    avls_package.Event = "-497,";
                    break;
                case "SERVICE NOT SUPPORTED":
                    avls_package.Event = "-496,";
                    break;
                case "QUERY INFO NOT CURRENTLY ATTAINABLE":
                    avls_package.Event = "-503,";
                    break;
                case "REPORTING WILL STOP":
                    avls_package.Event = "-99,";
                    break;
                    */
                    case "INSUFFICIENT GPS SATELLITES":
                        //avls_package.Event = "1,";

                        #region access power status

                        if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                        {
                             unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                             unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = 'on',
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + deviceID + @"'" + @" AND 
  (custom.uns_deivce_power_status.power <> 'on' OR 
  custom.uns_deivce_power_status.power IS NULL) ";
                            //while (!sql_client.connect())
                            {
                                //Thread.Sleep(30);
                            }
                            //lock (access_uns_deivce_power_status_Lock)
                            sql_client.modify(unsSqlCmd);
                            //sql_client.disconnect();
                        }

                        #endregion

                        break;
                    case "BAD GPS GEOMETRY":
                        //avls_package.Event = "1,";

                        #region access power status

                        if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                        {
                             unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                             unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = 'on',
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + deviceID + @"'" + @" AND 
  (custom.uns_deivce_power_status.power <> 'on' OR 
  custom.uns_deivce_power_status.power IS NULL) ";
                            //while (!sql_client.connect())
                            {
                                //Thread.Sleep(30);
                            }
                            //lock (access_uns_deivce_power_status_Lock)
                            sql_client.modify(unsSqlCmd);
                            //sql_client.disconnect();
                        }

                        #endregion

                        break;
                    case "GPS INVALID":
                        // avls_package.Event = "1,";

                        #region access power status

                        if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                        {
                             unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                             unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = 'on',
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + deviceID + @"'" + @" AND 
  (custom.uns_deivce_power_status.power <> 'on' OR 
  custom.uns_deivce_power_status.power IS NULL) ";
                            //while (!sql_client.connect())
                            {
                                //Thread.Sleep(30);
                            }
                            //lock (access_uns_deivce_power_status_Lock)
                            sql_client.modify(unsSqlCmd);
                            //sql_client.disconnect();
                        }

                        #endregion

                        break;
                        /*
                case "API DISCONNECTED":
                    avls_package.Event = "-495,";
                    break;
                case "OPERA TION NOT PERMITTED":
                    avls_package.Event = "-494,";
                    break;
                case "API NOT LICENSED":
                    avls_package.Event = "-493,";
                    break;
                    */
                }
            }
            if (htable.ContainsKey("event_info"))
            {
                gps_log._option3 = "\'" + htable["event_info"].ToString() + "\'";
                string unsUpdateTimeStamp = string.Empty;
                string unsSqlCmd = string.Empty;
                switch (htable["event_info"].ToString())
                {
                    case "Emergency On":
                    case "Emergency Off":
                        gps_log.j_6 = "\'" + htable["event_info"].ToString() + "\'";
                        gps_log.j_7 = "\'" + "null" + "\'";
                        gps_log.j_8 = "\'" + "null" + "\'";
                        operation_log.event_id = "\'" + operation_log.eqp_id + now +
                                                 "\'";
                        break;
                    case "Unit Present":
                        if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                        {
                            /*
                            while (!sql_client.connect())
                            {
                                Thread.Sleep(30);
                            }
                            string reg_countUid = sql_client.get_DataTable("SELECT COUNT(uid)   FROM custom.regist_log").Rows[0].ItemArray[0].ToString();
                            sql_client.disconnect();
                            */
                            //string reg_sn = "\'" + deviceID + "_" + now + "_" + reg_countUid + "\'";
                            
                            {
                                /*
                                regSqlCmd = @"INSERT INTO
  custom.regist_log(
  serial_no,
  uid)
  VALUES (" + reg_sn + @"," + reg_uid + @")";
                                while (!sql_client.connect())
                                {
                                    Thread.Sleep(30);
                                }
                                sql_client.modify(regSqlCmd);
                                sql_client.disconnect();
                                */

                                #region access power status



                                 unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                                 unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = 'on',
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + deviceID + @"'" + @"AND 
  (custom.uns_deivce_power_status.power <> 'on' OR 
  custom.uns_deivce_power_status.power IS NULL) ";
                                //while (!sql_client.connect())
                                {
                                    //Thread.Sleep(30);
                                }
                                //lock (access_uns_deivce_power_status_Lock)
                                sql_client.modify(unsSqlCmd);
                                //sql_client.disconnect();


                                #endregion

                                #region insert into unsPowerStatusHistory
                                unsSqlCmd = @"INSERT INTO
  custom.""unsPowerStatusHistory""(
  uid,
  status)
VALUES(
  '" + deviceID + @"',
  'on')";
                                
                                //while (!sql_client.connect())
                                {
                                    //Thread.Sleep(30);
                                }
                                sql_client.modify(unsSqlCmd);
                                //sql_client.disconnect();
                                #endregion insert into unsPowerStatusHistory
                                /*
                                #region access custom.unsPowerStatusHistory

                                unsSqlCmd = @"SELECT 
  custom.""unsPowerStatusHistory"".sn
FROM
  custom.""unsPowerStatusHistory""
WHERE
  custom.""unsPowerStatusHistory"".status = 'on' AND 
  custom.""unsPowerStatusHistory"".uid = '" + deviceID + @"'
ORDER BY
  custom.""unsPowerStatusHistory"".""createTime"" DESC
LIMIT 1";
                                while (!sql_client.connect())
                                {
                                    Thread.Sleep(30);
                                }
                                dt = sql_client.get_DataTable(unsSqlCmd);
                                sql_client.disconnect();
                                if (dt != null && dt.Rows.Count != 0)
                                {
                                }
                                else
                                {
                                    unsSqlCmd = @"INSERT INTO
  custom.""unsPowerStatusHistory""(
  uid,
  status)
VALUES(
  '" + deviceID + @"',
  'on')";
                                }
                                while (!sql_client.connect())
                                {
                                    Thread.Sleep(30);
                                }
                                sql_client.modify(unsSqlCmd);
                                sql_client.disconnect();

                                #endregion
                                */
                            }


                        }
                        sql_client.disconnect();
                        sql_client.Dispose();
                        sql_client = null;
                        return;
                        break;
                    case "Unit Absent":
                        gps_log.j_7 = "\'" + htable["event_info"].ToString() + "\'";
                        gps_log.j_6 = "\'" + "null" + "\'";
                        gps_log.j_8 = "\'" + "null" + "\'";
                        operation_log.event_id = "\'" + operation_log.eqp_id + now +
                                                 "\'";

                        #region access power status

                    {
                        if (!string.IsNullOrEmpty(deviceID) && CheckIfUidExist(deviceID))
                        {
                            
                            {
                                 unsUpdateTimeStamp = DateTime.Now.ToString("yyyyMMdd HHmmss+8");
                                 unsSqlCmd = @"UPDATE 
  custom.uns_deivce_power_status
SET
  power = 'off',
""updateTime"" = '" + unsUpdateTimeStamp + @"'::timestamp
WHERE
  custom.uns_deivce_power_status.uid = '" + deviceID + @"'" + @" AND 
  (custom.uns_deivce_power_status.power <> 'off' OR 
  custom.uns_deivce_power_status.power IS NULL) ";

                                //while (!sql_client.connect())
                                {
                                    //Thread.Sleep(30);
                                }
                                //lock (access_uns_deivce_power_status_Lock)
                                sql_client.modify(unsSqlCmd);
                                //sql_client.disconnect();

                                #region insert into unsPowerStatusHistory
                                unsSqlCmd = @"INSERT INTO
  custom.""unsPowerStatusHistory""(
  uid,
  status)
VALUES(
  '" + deviceID + @"',
  'off')";

                                //while (!sql_client.connect())
                                {
                                    //Thread.Sleep(30);
                                }
                                sql_client.modify(unsSqlCmd);
                                //sql_client.disconnect();
                                #endregion insert into unsPowerStatusHistory
                                /*
                                #region access custom.unsPowerStatusHistory

                                unsSqlCmd = @"SELECT 
  custom.""unsPowerStatusHistory"".sn
FROM
  custom.""unsPowerStatusHistory""
WHERE
  custom.""unsPowerStatusHistory"".status = 'off' AND 
  custom.""unsPowerStatusHistory"".uid = '" + deviceID + @"'
ORDER BY
  custom.""unsPowerStatusHistory"".""createTime"" DESC
LIMIT 1";
                                while (!sql_client.connect())
                                {
                                    Thread.Sleep(30);
                                }
                                dt = sql_client.get_DataTable(unsSqlCmd);
                                sql_client.disconnect();
                                if (dt != null && dt.Rows.Count != 0)
                                {
                                }
                                else
                                {
                                    unsSqlCmd = @"INSERT INTO
  custom.""unsPowerStatusHistory""(
  uid,
  status)
VALUES(
  '" + deviceID + @"',
  'off')";
                                }
                                while (!sql_client.connect())
                                {
                                    Thread.Sleep(30);
                                }
                                sql_client.modify(unsSqlCmd);
                                sql_client.disconnect();

                                #endregion
                                */
                            }

                        }
                    }

                        #endregion
                    sql_client.disconnect();
                    sql_client.Dispose();
                    sql_client = null;
                        return;
                        break;
                    case "Ignition Off":
                    case "Ignition On":
                        gps_log.j_8 = "\'" + htable["event_info"].ToString() + "\'";
                        gps_log.j_6 = "\'" + "null" + "\'";
                        gps_log.j_7 = "\'" + "null" + "\'";
                        operation_log.event_id = "\'" + operation_log.eqp_id + now +
                                                  "\'";
                        break;
                    default:
                        gps_log.j_6 = gps_log.j_7 = gps_log.j_8 = operation_log.event_id = "\'" + "null" + "\'";
                        break;

                }
            }
            else
                gps_log.j_6 = gps_log.j_7 = gps_log.j_8 = operation_log.event_id = "\'" + "null" + "\'";
            ///TODO:implement set last lat lon value
            if (htable.ContainsKey("lat_value") && htable.ContainsKey("long_value"))
            {
                gps_log._lat = gps_log._or_lat = operation_log.eqp_lat = htable["lat_value"].ToString();
                gps_log._lon = gps_log._or_lon = operation_log.eqp_lon = htable["long_value"].ToString();
            }
            else
            {
                if (htable.ContainsKey("suaddr"))
                {

                    if (!string.IsNullOrEmpty(deviceID))
                    {
                    }
                    else
                    {
                        log.Error("access_sql_server:1:deviceID is null");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("-access_sql_server");
                        Console.ResetColor();
                        sql_client.disconnect();
                        sql_client.Dispose();
                        sql_client = null;
                        return;
                    }
                    string sqlCmd = @"SELECT 
  public._gps_log._lat,
  public._gps_log._lon
FROM
  public._gps_log
WHERE
  public._gps_log._time < now() AND 
  public._gps_log._uid = '" + deviceID + @"'
ORDER BY
  public._gps_log._time DESC
LIMIT 1";
                    //while (!sql_client.connect())
                    {
                        //Thread.Sleep(30);
                    }
                    DataTable sqlDatetable = sql_client.get_DataTable(sqlCmd);
                    //sql_client.disconnect();
                    if (sqlDatetable != null && sqlDatetable.Rows.Count != 0)
                    {
                        string avlsLat = string.Empty, avlsLon = string.Empty;
                        foreach (DataRow row in sqlDatetable.Rows)
                        {
                            gps_log._lat = gps_log._or_lat = operation_log.eqp_lat = row[0].ToString();
                            gps_log._lon = gps_log._or_lon = operation_log.eqp_lon = row[1].ToString();
                        }
                        string zero = "0";
                        if (gps_log._lat.Equals(zero) || gps_log._lon.Equals(zero))
                        {
                            string lat = string.Empty, lon = string.Empty;
                            GetInitialLocationFromSql(ref lat, ref lon, deviceID);
                            gps_log._lat = gps_log._or_lat = operation_log.eqp_lat = lat;
                            gps_log._lon = gps_log._or_lon = operation_log.eqp_lon = lon;
                        }
                    }
                    else
                    {
                        //string zero = "0";
                        //gps_log._lat = operation_log.eqp_lat = zero;
                        //gps_log._lon = operation_log.eqp_lon = zero;

                        string lat = string.Empty, lon = string.Empty;
                        GetInitialLocationFromSql(ref lat, ref lon, deviceID);
                        gps_log._lat = gps_log._or_lat = operation_log.eqp_lat = lat;
                        gps_log._lon = gps_log._or_lon = operation_log.eqp_lon = lon;
                    }
                    sqlDatetable.Dispose();
                    sqlDatetable = null;
                }
                else
                {
                    //string zero = "0";
                    //gps_log._lat = operation_log.eqp_lat = zero;
                    //gps_log._lon = operation_log.eqp_lon = zero;

                    if (!string.IsNullOrEmpty(deviceID))
                    {
                    }
                    else
                    {
                        log.Error("access_sql_server:2:deviceID is null");
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("-access_sql_server");
                        Console.ResetColor();
                        sql_client.disconnect();
                        sql_client.Dispose();
                        sql_client = null;
                        return;
                    }
                    string lat = string.Empty, lon = string.Empty;
                    GetInitialLocationFromSql(ref lat, ref lon, deviceID);
                    gps_log._lat = gps_log._or_lat = operation_log.eqp_lat = lat;
                    gps_log._lon = gps_log._or_lon = operation_log.eqp_lon = lon;
                }

            }


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
                //gps_log._speed = htable["speed-hor"].ToString();
                gps_log._speed =
                    operation_log.eqp_speed =
                        Convert.ToInt32((double.Parse(htable["speed-hor"].ToString())*3.6)).ToString();
            }
            else
                gps_log._speed = operation_log.eqp_speed = "0";
            if (htable.ContainsKey("direction-hor"))
            {
                gps_log._course = operation_log.eqp_course = htable["direction-hor"].ToString();
            }
            else
                gps_log._course = operation_log.eqp_course = "0";
            if (htable.ContainsKey("Odometer"))
            {
                gps_log._distance = operation_log.eqp_distance = htable["Odometer"].ToString().Replace(",", ".");
            }
            if (htable.ContainsKey("info_time"))
            {
                DateTime infoTime = DateTime.ParseExact(htable["info_time"].ToString(), "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture).ToLocalTime();
                gps_log._option0 = "\'" + infoTime.ToString("yyyyMMddHHmmss") + "\'";
            }
            else
                gps_log._option0 = "\'" + DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss") + "\'";
            if (htable.ContainsKey("server_time"))
            {
                DateTime serverTime = DateTime.ParseExact(htable["server_time"].ToString(), "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture).ToLocalTime();
                gps_log._option1 = "\'" + serverTime.ToString("yyyyMMddHHmmss") + "\'";
            }
            else
                gps_log._option1 = "\'" + DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss") + "\'";
            /*
            #region operation error to access custom.turn_onoff_log table
            if (elements.Contains(new XElement("operation-error").Name))
            {
                if (htable.ContainsKey("suaddr"))
                {
                    string sql_cmd = string.Empty;
                    DataTable dt = new DataTable();
                    sql_cmd = @"SELECT 
  custom.turn_onoff_log.serial_no
FROM
  custom.turn_onoff_log
WHERE
  custom.turn_onoff_log.uid = '" + deviceID + @"' AND 
custom.turn_onoff_log.on_time IS NOT NULL AND 
  custom.turn_onoff_log.off_time IS  NULL
ORDER BY
  custom.turn_onoff_log.create_time DESC
LIMIT 1";
                    sql_client.connect();
                    dt = sql_client.get_DataTable(sql_cmd);
                    sql_client.disconnect();
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        //do nothing
                    }
                    else
                    {
                        Device_power_status dev_power_status = new Device_power_status();
                        dev_power_status.ID = deviceID;
                        #region
                        {
                            string sn = string.Empty;
                            string power_on_today = DateTime.Now.ToString("yyyyMMdd");
                            sql_cmd = @"SELECT 
  custom.turn_onoff_log.serial_no
FROM
  custom.turn_onoff_log
WHERE
  custom.turn_onoff_log.uid = '" + dev_power_status.ID + @"'

ORDER BY
  custom.turn_onoff_log.create_time DESC
LIMIT 1";
                            sql_client.connect();
                            dt = sql_client.get_DataTable(sql_cmd);
                            sql_client.disconnect();
                            if (dt != null && dt.Rows.Count != 0)
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    sn = row[0].ToString();
                                }

                                Console.WriteLine("dev_power_status.ID.Length=" + dev_power_status.ID.Length);
                                Console.WriteLine("dev_power_status.ID=" + dev_power_status.ID);

                                string yyyyMMdd = sn.Substring(dev_power_status.ID.Length, 8);
                                string count = sn.Substring(dev_power_status.ID.Length + yyyyMMdd.Length, 3);

                                if (power_on_today.Equals(yyyyMMdd))
                                {
                                    uint addCount = (uint.Parse(count) + 1);
                                    dev_power_status.SN = dev_power_status.ID + power_on_today + addCount.ToString("D3");
                                }
                                else
                                {
                                    int iVal = 0;

                                    dev_power_status.SN = dev_power_status.ID + power_on_today + iVal.ToString("D3");
                                }

                                
                            }
                            else
                            {
                                int iVal = 0;

                                dev_power_status.SN = dev_power_status.ID + power_on_today + iVal.ToString("D3");
                                

                            }
                        }
                        #endregion
                        string sql_table_columns = "serial_no,uid,on_time,create_user,create_ip";
                        string sql_table_column_value = "\'" + dev_power_status.SN + "\'" + "," + "\'" + dev_power_status.ID + "\'" + "," + "\'" +
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\'" + "," + "0" + "," + "\'" + GetLocalIPAddress() + "\'";
                        sql_cmd = "INSERT INTO custom.turn_onoff_log (" + sql_table_columns + ") VALUES (" + sql_table_column_value + ")";
                        sql_client.connect();
                        sql_client.modify(sql_cmd);
                        sql_client.disconnect();
                    }
                }

            }
            #endregion
            */
            //while (!sql_client.connect())
            {
                //Thread.Sleep(30);
            }
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
                                table_columns =
                                    "_id,_uid,_option2,_option3,_or_lon,_or_lat,_satellites,_temperature,_voltage,_option0,_option1,_lat,_lon";
                                table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._option2 + "," +
                                                     gps_log._option3 + "," +
                                                     gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites +
                                                     "," +
                                                     gps_log._temperature + "," + gps_log._voltage + "," +
                                                     gps_log._option0 +
                                                     "," + gps_log._option1
                                                     +
                                                     "," + gps_log._lat
                                                     +
                                                     "," + gps_log._lon;
                                cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES (" +
                                      table_column_value + ")";
                            }
                            else
                            {
                                if (elements.Contains(new XElement("vehicle-info").Name))
                                {
                                    gps_log._status = ((int) device_status.MV).ToString();
                                    gps_log._validity = "\'Y\'";
                                    table_columns =
                                        "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,_distance,j_5,_option0,_option1," +
                                        "_or_lon,_or_lat,_satellites,_temperature,_voltage";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," +
                                                         gps_log._time +
                                                         "," + gps_log._validity + "," + gps_log._lat + "," +
                                                         gps_log._lon + "," + gps_log._speed +
                                                         "," + gps_log._course + "," + gps_log._distance + "," +
                                                         gps_log.j_5 + "," + gps_log._option0 +
                                                         "," + gps_log._option1 + "," +
                                                         gps_log._or_lon + "," + gps_log._or_lat + "," +
                                                         gps_log._satellites + "," +
                                                         gps_log._temperature + "," + gps_log._voltage;
                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" +
                                          table_column_value + ")";
                                }
                                else
                                {
                                    gps_log._status = ((int) device_status.MV).ToString();
                                    gps_log._validity = "\'Y\'";
                                    table_columns =
                                        "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,j_5,_option0,_option1," +
                                        "_or_lon,_or_lat,_satellites,_temperature,_voltage";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," +
                                                         gps_log._time +
                                                         "," + gps_log._validity + "," + gps_log._lat + "," +
                                                         gps_log._lon + "," + gps_log._speed +
                                                         "," + gps_log._course + "," + gps_log.j_5 + "," +
                                                         gps_log._option0 +
                                                         "," + gps_log._option1 + "," +
                                                         gps_log._or_lon + "," + gps_log._or_lat + "," +
                                                         gps_log._satellites + "," +
                                                         gps_log._temperature + "," + gps_log._voltage;
                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" +
                                          table_column_value + ")";
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
                            gps_log._status = ((int) device_status.UL).ToString();
                            if (xml_validation_with_dtd(log1, xml_root_tag))
                            {
                                gps_log._validity = "\'Y\'";
                                table_columns =
                                    "_id,_uid,_status,_validity,_or_lon,_or_lat,_satellites,_temperature,,_voltage,_option3,j_6,j_7,_lat,_lon";
                                table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status + "," +
                                                     gps_log._validity + "," +
                                                     gps_log._or_lon + "," + gps_log._or_lat + "," + gps_log._satellites +
                                                     "," +
                                                     gps_log._temperature + "," + gps_log._voltage + "," +
                                                     gps_log._option3 + "," + gps_log.j_6 + "," + gps_log.j_7 + "," +
                                                     gps_log._lat + "," + gps_log._lon;
                                //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" +
                                      table_column_value + ")";
                            }
                            else
                            {
                                if (elements.Contains(new XElement("operation-error").Name))
                                {
                                    table_columns =
                                        "_id,_uid,_option2,_option3,_or_lon,_or_lat,_satellites,_temperature,_voltage,_option0,_option1,_lat,_lon";
                                    table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._option2 + "," +
                                                         gps_log._option3 + "," +
                                                         gps_log._or_lon + "," + gps_log._or_lat + "," +
                                                         gps_log._satellites + "," +
                                                         gps_log._temperature + "," + gps_log._voltage + "," +
                                                         gps_log._option0 +
                                                         "," + gps_log._option1 + "," + gps_log._lat + "," +
                                                         gps_log._lon;
                                    cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES (" +
                                          table_column_value + ")";

                                }
                                else
                                {
                                    if (elements.Contains(new XElement("vehicle-info").Name))
                                    {
                                        gps_log._status = ((int) device_status.MV).ToString();
                                        gps_log._validity = "\'Y\'";
                                        table_columns =
                                            "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,_distance,j_5,_option0,_option1," +
                                            "_or_lon,_or_lat,_satellites,_temperature,_voltage,_option3,j_6,j_7";
                                        table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status +
                                                             "," + gps_log._time +
                                                             "," + gps_log._validity + "," + gps_log._lat + "," +
                                                             gps_log._lon + "," + gps_log._speed +
                                                             "," + gps_log._course + "," + gps_log._distance + "," +
                                                             gps_log.j_5 + "," + gps_log._option0 +
                                                             "," + gps_log._option1 + "," +
                                                             gps_log._or_lon + "," + gps_log._or_lat + "," +
                                                             gps_log._satellites + "," +
                                                             gps_log._temperature + "," + gps_log._voltage + "," +
                                                             gps_log._option3 + "," + gps_log.j_6 + "," + gps_log.j_7;
                                        //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                        cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" +
                                              table_column_value + ")";
                                    }
                                    else
                                    {
                                        gps_log._status = ((int) device_status.MV).ToString();
                                        gps_log._validity = "\'Y\'";
                                        table_columns =
                                            "_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,j_5,_option0,_option1," +
                                            "_or_lon,_or_lat,_satellites,_temperature,_voltage,_option3,j_6,j_7";
                                        table_column_value = gps_log._id + "," + gps_log._uid + "," + gps_log._status +
                                                             "," + gps_log._time +
                                                             "," + gps_log._validity + "," + gps_log._lat + "," +
                                                             gps_log._lon + "," + gps_log._speed +
                                                             "," + gps_log._course + "," + gps_log.j_5 + "," +
                                                             gps_log._option0 +
                                                             "," + gps_log._option1 + "," +
                                                             gps_log._or_lon + "," + gps_log._or_lat + "," +
                                                             gps_log._satellites + "," +
                                                             gps_log._temperature + "," + gps_log._voltage + "," +
                                                             gps_log._option3 + "," + gps_log.j_6 + "," + gps_log.j_7;
                                        //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                        cmd = "INSERT INTO public._gps_log (" + table_columns + ") VALUES  (" +
                                              table_column_value + ")";

                                    }
                                }
                            }

                        }
                            break;
                        case "Immediate-Location-Report":
                        {
                            operation_log.create_user = @"'System'";
                            if (elements.Contains(new XElement("operation-error").Name))
                            {
                                table_columns =
                                    "_id,event_id,request_id,result_code,result_msg,create_user,eqp_lat,eqp_lon";
                                table_column_value = operation_log._id + "," + operation_log.event_id + "," +
                                                     operation_log.request_id + "," + operation_log.result_code +
                                                     "," + operation_log.result_msg + "," + operation_log.create_user +
                                                     "," +
                                                     operation_log.eqp_lat + "," +
                                                     operation_log.eqp_lon;

                                cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES (" +
                                      table_column_value + ")";

                            }
                            else
                            {
                                if (elements.Contains(new XElement("vehicle-info").Name))
                                {
                                    //gps_log._status = ((int)device_status.EM).ToString();
                                    //gps_log._validity = "\'Y\'";
                                    table_columns =
                                        "_id,event_id,request_id,eqp_id,eqp_lat,eqp_lon,eqp_speed,eqp_course,eqp_distance,create_user";
                                    table_column_value = operation_log._id + "," +
                                                         operation_log.event_id + "," +
                                                         operation_log.request_id + "," +
                                                         operation_log.eqp_id + "," +
                                                         operation_log.eqp_lat + "," +
                                                         operation_log.eqp_lon + "," +
                                                         operation_log.eqp_speed + "," +
                                                         operation_log.eqp_course + "," +
                                                         operation_log.eqp_distance + "," +
                                                         operation_log.create_user;

                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES  (" +
                                          table_column_value + ")";
                                }
                                else //remove eqp_distance only
                                {
                                    //gps_log._status = ((int)device_status.EM).ToString();
                                    //gps_log._validity = "\'Y\'";
                                    table_columns =
                                        "_id,event_id,request_id,eqp_id,eqp_lat,eqp_lon,eqp_speed,eqp_course,create_user";
                                    table_column_value = operation_log._id + "," +
                                                         operation_log.event_id + "," +
                                                         operation_log.request_id + "," +
                                                         operation_log.eqp_id + "," +
                                                         operation_log.eqp_lat + "," +
                                                         operation_log.eqp_lon + "," +
                                                         operation_log.eqp_speed + "," +
                                                         operation_log.eqp_course + "," +
                                                         operation_log.create_user;

                                    //table_column_value = @"'1','1','1','20130808 13:13:13.133 PST','Y',0,0,0,0,0,'0','0','0',0,0,0,0,0";
                                    cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES  (" +
                                          table_column_value + ")";

                                }
                            }
                        }
                            break;
                        case "Location-Registration-Answer":
                        {
                            operation_log.create_user = @"'System'";
                            if (elements.Contains(new XElement("operation-error").Name))
                            {
                                table_columns =
                                    "_id,event_id,request_id,result_code,result_msg,create_user,eqp_lat,eqp_lon";
                                table_column_value = operation_log._id + "," + operation_log.event_id + "," +
                                                     operation_log.request_id + "," + operation_log.result_code +
                                                     "," + operation_log.result_msg + "," + operation_log.create_user +
                                                     "," +
                                                     operation_log.eqp_lat + "," +
                                                     operation_log.eqp_lon;

                                cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES (" +
                                      table_column_value + ")";

                            }
                            else
                            {
                                table_column_value = operation_log._id + "," +
                                                     operation_log.event_id + "," +
                                                     operation_log.application_id + "," +
                                                     operation_log.result_code + "," +
                                                     operation_log.result_msg + "," +
                                                     operation_log.create_user + "," +
                                                     operation_log.eqp_lat + "," +
                                                     operation_log.eqp_lon;
                                ;
                                table_columns =
                                    "_id,event_id,application_id,result_code,result_msg,create_user,eqp_lat,eqp_lon";
                                cmd = "INSERT INTO public.operation_log (" + table_columns + ") VALUES (" +
                                      table_column_value + ")";
                            }
                        }
                            break;
                    }
                    sql_client.modify(cmd);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("access_sql_serverError:" + Environment.NewLine + ex);
                    //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" +
                                      //ex.LineNumber());
                    //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                    log.Error("access_sql_serverError:" + Environment.NewLine + ex);
                }
                finally
                {
                    //sql_client.disconnect();
                }
            }
            
            

            //while (!sql_client.connect())
            {
                //Thread.Sleep(30);
            }
                string cgaEventLogIdCount = string.Empty;
            //sql_client.disconnect();
            string yyyymmddhhmmss = DateTime.Now.ToString("yyyyMMddHHmmss");

            #region access GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql
            string prohibitedTableName = string.Empty, locationTableName = string.Empty;

            prohibitedTableName = "public.p_prohibited";
            locationTableName = "public.patrol_location";
            //string getMessage = string.Empty,bundaryEventNumber = string.Empty;
                string bundaryEventNumber = string.Empty;
                //lock (cgaEventAccessSqlLock)
            {

                /* 
                 * only for access sql table:custom.cga_event_log
                 * event:2->msg:p_prohibited
                 * event:3->msg:patrol_location
                 * event:4->msg:p_prohibited,patrol_location
                 * event:x.5->x stay over specific 
                 * event:5->stay over specific time within 0.1 km
                 * event:-2->msg:p_prohibited_out
                 * event:-5->out of prohibited but in patrol_location
                 */
                //getMessage = GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql(prohibitedTableName,
                    //locationTableName,
                    //gps_log._lat, gps_log._lon, deviceID,false);
                if (!string.IsNullOrEmpty(getMessage))
                {
                    if (getMessage.Contains("p_prohibited") && getMessage.Contains("patrol_location") && !getMessage.Contains("p_prohibited_out"))
                    {
                        bundaryEventNumber = "4";
                    }
                    else
                    {
                        if (getMessage.Contains("p_prohibited") && !getMessage.Contains("p_prohibited_out"))
                        {
                            bundaryEventNumber = "2";
                        }
                        else
                        {
                            if (getMessage.Contains("patrol_location") && !getMessage.Contains("p_prohibited_out"))
                            {
                                bundaryEventNumber = "3";
                            }
                        }
                    }
                    if (getMessage.Contains("p_prohibited_out") && !getMessage.Contains("patrol_location"))
                    {
                        bundaryEventNumber = "-2";
                    }
                    if (getMessage.Contains("p_prohibited_out") && getMessage.Contains("patrol_location"))
                    {
                        bundaryEventNumber = "-5";
                    }

                    //insert into custom.cga_event_log
                    //while (!sql_client.connect())
                    {
                        //Thread.Sleep(30);
                    }
                    cgaEventLogIdCount = DateTime.Now.ToString("yyyyMMddHHmmssfffffff");
                    string sn = "\'" + deviceID + now + cgaEventLogIdCount + "\'";
                    string table_columns =
                        "serial_no ,uid ,type ,lat ,lon,altitude ,speed ,course ,radius ,info_time ,server_time ,create_user ,create_ip,start_time,create_time";
                    string table_column_value = sn + "," +
                                                gps_log._uid + "," + //gps_log._option3
                                                @"'"+bundaryEventNumber+@"'" + "," + gps_log._lat + "," + gps_log._lon + "," +
                                                gps_log._altitude + "," + gps_log._speed + "," +
                                                gps_log._course + "," +
                                                gps_log.j_5 + "," + "to_timestamp(" +
                                                gps_log._option0 + @",'YYYYMMDDHH24MISS')" +
                                                "," + "to_timestamp(" +
                                                gps_log._option1 + @",'YYYYMMDDHH24MISS')" +
                                                "," + @"1" + "," + @"'" + GetLocalIPAddress().ToString() +
                                                @"'" +
                                                "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                @"','YYYYMMDDHH24MISS')" +
                                                "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                @"','YYYYMMDDHH24MISS')";
                    string cmd = "INSERT INTO custom.cga_event_log (" + table_columns + ") VALUES  (" +
                                 table_column_value + ")";
                    sql_client.modify(cmd);
                    //sql_client.disconnect();
                }
            }
                /*
            //lock (getGidAndFullnameLock)
            {

                 
                 // only for access sql table:custom.cga_event_log
                 // event:2->msg:p_prohibited
                 // event:3->msg:patrol_location
                 // event:4->msg:p_prohibited,patrol_location
                 // event:x.5->x stay over specific time
                 // event:5->stay over specific time within 0.1 km
                 //
                getMessage = GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql(prohibitedTableName,
                    locationTableName,
                    gps_log._lat, gps_log._lon, deviceID, true);
                if (!string.IsNullOrEmpty(getMessage))
                {
                    if (getMessage.Contains("p_prohibited") && getMessage.Contains("patrol_location") && !getMessage.Contains("p_prohibited_out"))
                    {
                        bundaryEventNumber = "4.5";
                    }
                    else
                    {
                        if (getMessage.Contains("p_prohibited") && !getMessage.Contains("p_prohibited_out"))
                        {
                            bundaryEventNumber = "2.5";
                        }
                        else
                        {
                            if (getMessage.Contains("patrol_location") && !getMessage.Contains("p_prohibited_out"))
                            {
                                bundaryEventNumber = "3.5";
                            }
                        }
                    }
                    
                    while (!sql_client.connect())
                    {
                        Thread.Sleep(30);
                    }
                    cgaEventLogIdCount =
                       Convert.ToDouble(
                           sql_client.get_DataTable("SELECT COUNT(uid)   FROM custom.cga_event_log WHERE uid = '" + deviceID + "\'").Rows[0].ItemArray[0]);
                    sql_client.disconnect();
                    //insert into custom.cga_event_log
                    while (!sql_client.connect())
                    {
                        Thread.Sleep(30);
                    }
                    string sn = "\'" + deviceID + now + cgaEventLogIdCount.ToString("000000000000") + "\'";
                    string table_columns =
                        "serial_no ,uid ,type ,lat ,lon,altitude ,speed ,course ,radius ,info_time ,server_time ,create_user ,create_ip,start_time,create_time";
                    string table_column_value = sn + "," +
                                                gps_log._uid + "," + //gps_log._option3
                                                @"'" + bundaryEventNumber + @"'" + "," + gps_log._lat + "," + gps_log._lon + "," +
                                                gps_log._altitude + "," + gps_log._speed + "," +
                                                gps_log._course + "," +
                                                gps_log.j_5 + "," + "to_timestamp(" +
                                                gps_log._option0 + @",'YYYYMMDDHH24MISS')" +
                                                "," + "to_timestamp(" +
                                                gps_log._option1 + @",'YYYYMMDDHH24MISS')" +
                                                "," + @"1" + "," + @"'" + GetLocalIPAddress().ToString() +
                                                @"'" +
                                                "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                @"','YYYYMMDDHH24MISS')" +
                                                "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                @"','YYYYMMDDHH24MISS')";
                    string cmd = "INSERT INTO custom.cga_event_log (" + table_columns + ") VALUES  (" +
                                 table_column_value + ")";
                    sql_client.modify(cmd);
                    sql_client.disconnect();
                }
            }
                */
            #endregion access GetGidAndFullnameFromP_prohibitedAndPatrol_locationFromSql

                //lock (cgaEventAccessSqlLock)
                {
                    #region checkIfOverTime
                    //event:5->stay over specific time within 0.1 km
                    getMessage = CheckIfStayOverTime(gps_log._lat, gps_log._lon, deviceID);
                    if (!string.IsNullOrEmpty(getMessage))
                    {
                        switch (getMessage)
                        {
                            case "in"://stay over time
                                bundaryEventNumber = "5";
                                cgaEventLogIdCount = DateTime.Now.ToString("yyyyMMddHHmmssfffffff");
                                //insert into custom.cga_event_log
                                //while (!sql_client.connect())
                                {
                                    //Thread.Sleep(30);
                                }
                                string sn = "\'" + deviceID + now + cgaEventLogIdCount + "\'";
                                string table_columns =
                                    "serial_no ,uid ,type ,lat ,lon,altitude ,speed ,course ,radius ,info_time ,server_time ,create_user ,create_ip,start_time,create_time";
                                string table_column_value = sn + "," +
                                                            gps_log._uid + "," + //gps_log._option3
                                                            @"'" + bundaryEventNumber + @"'" + "," + gps_log._lat + "," + gps_log._lon + "," +
                                                            gps_log._altitude + "," + gps_log._speed + "," +
                                                            gps_log._course + "," +
                                                            gps_log.j_5 + "," + "to_timestamp(" +
                                                            gps_log._option0 + @",'YYYYMMDDHH24MISS')" +
                                                            "," + "to_timestamp(" +
                                                            gps_log._option1 + @",'YYYYMMDDHH24MISS')" +
                                                            "," + @"1" + "," + @"'" + GetLocalIPAddress().ToString() +
                                                            @"'" +
                                                            "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                            @"','YYYYMMDDHH24MISS')" +
                                                            "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                            @"','YYYYMMDDHH24MISS')";
                                string cmd = "INSERT INTO custom.cga_event_log (" + table_columns + ") VALUES  (" +
                                             table_column_value + ")";
                                sql_client.modify(cmd);
                                //sql_client.disconnect();
                                break;
                            case "out":
                                break;
                        }
                    }
                    #endregion checkIfOverTime
                }
                //lock (cgaEventAccessSqlLock)
                {
                    #region insert into custom.cga_event_log
                    cgaEventLogIdCount = DateTime.Now.ToString("yyyyMMddHHmmssfffffff");

                    {
                        try
                        {
                            if (xml_root_tag == "Unsolicited-Location-Report" && htable.ContainsKey("event_info"))
                            {
                                switch (htable["event_info"].ToString())
                                {
                                    case "Emergency On":
                                        string sn = "\'" + deviceID + now + cgaEventLogIdCount + "\'";
                                        string table_columns =
                                            "serial_no ,uid ,type ,lat ,lon,altitude ,speed ,course ,radius ,info_time ,server_time ,create_user ,create_ip,start_time,create_time";
                                        string table_column_value = sn + "," +
                                                                    gps_log._uid + "," + //gps_log._option3
                                                                    @"'150'" + "," + gps_log._lat + "," + gps_log._lon + "," +
                                                                    gps_log._altitude + "," + gps_log._speed + "," +
                                                                    gps_log._course + "," +
                                                                    gps_log.j_5 + "," + "to_timestamp(" +
                                                                    gps_log._option0 + @",'YYYYMMDDHH24MISS')" +
                                                                    "," + "to_timestamp(" +
                                                                    gps_log._option1 + @",'YYYYMMDDHH24MISS')" +
                                                                    "," + @"1" + "," + @"'" + GetLocalIPAddress().ToString() +
                                                                    @"'" +
                                                                    "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                                    @"','YYYYMMDDHH24MISS')" +
                                                                    "," + @"to_timestamp('" + yyyymmddhhmmss +
                                                                    @"','YYYYMMDDHH24MISS')";
                                        string cmd = "INSERT INTO custom.cga_event_log (" + table_columns + ") VALUES  (" +
                                                     table_column_value + ")";
                                        //while (!sql_client.connect())
                                        {
                                            //Thread.Sleep(30);
                                        }
                                        sql_client.modify(cmd);
                                        break;
                                    case "Emergency Off":

                                        break;
                                    case "Unit Present":
                                    case "Unit Absent":

                                        break;
                                    case "Ignition Off":
                                    case "Ignition On":

                                        break;

                                }

                            }


                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        finally
                        {
                            sql_client.disconnect();
                        }
                    }
                    #endregion #region insert into custom.cga_event_log
                }
            
            htable.Clear();
                htable = null;
            sql_client.Dispose();
                sql_client = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("-access_sql_server");
                Console.ResetColor();
            //Console.WriteLine("-access_sql_server");
            //sqlAccessEvent.Set();
                stopWatch.Stop();
                SiAuto.Main.LogMessage("access_sql_server spend time(ms):"+stopWatch.ElapsedMilliseconds);
        }
    }

        private static string CheckIfStayOverTime(string lat, string lon, string deviceID)
        {
            double distanceLimit = 0.1;//unit:km
            string DB = string.Empty, stayTimeInMin = string.Empty;
            List<string> resultList= new List<string>();
            DB = "lmap100";
            SqlClient sqlclient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], DB, ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);
            
            string sqlCmd = @"select stay_time from p_config LIMIT 1";

            while (!sqlclient.connect())
            {
                Thread.Sleep(30);
            }
            var dt2 = sqlclient.get_DataTable(sqlCmd);
            sqlclient.disconnect();
            if (dt2 != null && dt2.Rows.Count != 0)
            {
                foreach (DataRow row in dt2.Rows)
                {
                    stayTimeInMin = row[0].ToString();
                }
            }
            else
            {
                stayTimeInMin = "0";
            }
			dt2.Dispose();
			dt2=null;
			sqlclient.Dispose();
			sqlclient=null;
            sqlCmd = @"SELECT
public._gps_log._lat,
public._gps_log._lon
FROM
public._gps_log
WHERE
public._gps_log._time <= now() AND
public._gps_log._time >= now() - interval '"+stayTimeInMin+@" minute' AND
public._gps_log._uid = '"+deviceID+@"'
";
             SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"],
                ConfigurationManager.AppSettings["SQL_SERVER_PORT"],
                ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"],
                ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"],
                ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"],
                ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"],
                ConfigurationManager.AppSettings["ConnectionLifetime"]);
            while (!sql_client.connect())
            {
                Thread.Sleep(30);
            }
            var dt3 = sql_client.get_DataTable(sqlCmd);
            sql_client.disconnect();
			sql_client.Dispose();
                    sql_client = null;
            if (dt3 != null && dt3.Rows.Count != 0)
            {
                foreach (DataRow row in dt3.Rows)
                {
                    double d = GeoCodeCalc.CalcDistance(double.Parse(lat), double.Parse(lon), double.Parse(row[0].ToString()),
                        double.Parse(row[1].ToString()), GeoCodeCalcMeasurement.Kilometers);
                    if(d<=distanceLimit)
                        resultList.Add("in");
                    else
                    {
                        resultList.Add("out");
                    }
                    Thread.Sleep(30);
                }
                resultList.Sort();
                int index = resultList.BinarySearch("out");
                resultList.Clear();
                resultList = null;
                if (index<0)
                {
                    string result = string.Empty;
                    result = "in";
                    
                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
					dt3.Dispose();
					dt3=null;
                    return result;
                }
                else
                {
                    string result = string.Empty;
                    result = "out";
                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
					dt3.Dispose();
					dt3=null;
                    return result;
                }
            }
            else
            {

                //GC.Collect();
                //GC.WaitForPendingFinalizers();
				dt3.Dispose();
				dt3=null;
                return string.Empty;
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
            catch (Exception ex)
            {
                Console.WriteLine("XmlGetTagAttributeValue:"+tag_name + ":" + tag_attribute_name+":"+ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("XmlGetTagAttributeValue:" + tag_name + ":" + tag_attribute_name + ":" + ex);
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
            catch (Exception ex)
            {
                Console.WriteLine("XmlGetTagValue:"+tag_name+":"+ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("XmlGetTagValue:" + tag_name + ":" + ex);
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
            catch (Exception ex)
            {
                Console.WriteLine("XmlGetFirstChildTagName:"+parent_tag_name+":"+ex);
                //Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                //log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name + "_errorline:" + ex.LineNumber());
                log.Error("XmlGetFirstChildTagName:" + parent_tag_name + ":" + ex);
                result = "";
            }

                return result;

        }
        static HashSet<XName> XmlGetAllElementsXname(XDocument xml_data)
        {
            var a = (from e1 in xml_data.DescendantNodes().OfType<XElement>() select e1).Select(x => x.Name).Distinct();
            return new HashSet<XName>(a);
        }
        static int GetLittleEndianIntegerFromByteArray(byte[] data, int startIndex)
        {
            //Console.WriteLine("+GetLittleEndianIntegerFromByteArray");
            //Console.WriteLine("date="+data);
            //Console.WriteLine("date.length=" + data.Length);
            //Console.WriteLine("startIndex=" + startIndex);
            //Console.WriteLine("-GetLittleEndianIntegerFromByteArray");
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

        private static bool xml_validation_with_dtd(string xml, string xml_root_tag)
        {
            string doctype_append = "<?xml version='1.0' encoding='utf-16'?><!DOCTYPE " + xml_root_tag + " SYSTEM \"" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"\\"+xml_root_tag + ".dtd\">";
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
                //Console.WriteLine("Document is valid");
                return true;
            }
            else
            {
                //Console.WriteLine("Document is invalid");
                return false;
            }
        }
        // Display any validation errors. 
        private static void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            isValid = false;
            Console.WriteLine("Validation Error: {0}", e.ToString());
        }

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
    }
    /*
     * private static void access_sql_server(string xml_root_tag, Hashtable htable, List<string> sensor_name,
            List<string> sensor_type, List<string> sensor_value, IEnumerable<XName> elements, string log1, string getMessage)
     */
    public class SqlClass
    {
        public string XmlRootTag;
        public Hashtable Htable;
        public List<string> SensorName;
        public List<string> SensorType;
        public List<string> SensorValue;
        public HashSet<XName> Elements;
        public string Log1;
        public string GetMessage;

        public SqlClass(string xml_root_tag, Hashtable htable, List<string> sensor_name,
            List<string> sensor_type, List<string> sensor_value, HashSet<XName> elements, string log1,
            string getMessage)
        {
            XmlRootTag = xml_root_tag;
            Htable = new Hashtable(htable);
            SensorName = new List<string>(sensor_name);
            SensorType = new List<string>(sensor_type);
            SensorValue = new List<string>(sensor_value);
            Elements = elements;
            Log1 = log1;
            GetMessage = getMessage;
        }
        ~SqlClass()
        {
            XmlRootTag = Log1 = GetMessage = string.Empty;
            Htable.Clear();
            SensorName.Clear();
            SensorType.Clear();
            SensorValue.Clear();
            Htable = null;
            SensorName = null;
            SensorType = null;
            SensorValue = null;
            Elements = null;
        }
    }
    /*
     * private static void access_avls_server(string xml_root_tag, Hashtable htable, List<string> sensor_name,
            List<string> sensor_type, List<string> sensor_value, IEnumerable<XName> iEnumerable, string log, 
            TcpClient avlsTcpClient, string getMessage)
     */

    public class AvlsClass
    {
        public string XmlRootTag;
        public Hashtable Htable;
        public List<string> SensorName;
        public List<string> SensorType;
        public List<string> SensorValue;
        public HashSet<XName> Elements;
        public string Log;
        public string GetMessage;

        public AvlsClass(string xml_root_tag, Hashtable htable, List<string> sensor_name,
            List<string> sensor_type, List<string> sensor_value, HashSet<XName> iEnumerable, string log,
             string getMessage)
        {
            XmlRootTag = xml_root_tag;
            Htable = new Hashtable(htable);
            SensorName = new List<string>(sensor_name);
            SensorType = new List<string>(sensor_type);
            SensorValue = new List<string>(sensor_value);
            Elements = iEnumerable;
            Log = log;
            GetMessage = getMessage;
        }
        ~AvlsClass()
        {
            XmlRootTag = Log = GetMessage = string.Empty;
            Htable.Clear();
            SensorName.Clear();
            SensorType.Clear();
            SensorValue.Clear();
            Htable = null;
            SensorName = null;
            SensorType = null;
            SensorValue = null;
            Elements = null;
        }
    }
    
    public class GeoAngle
    {
        public bool IsNegative { get; set; }
        public int Degrees { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Milliseconds { get; set; }



        public static GeoAngle FromDouble(decimal angleInDegrees)
        {
            //ensure the value will fall within the primary range [-180.0..+180.0]
            while (angleInDegrees < -180.0m)
                angleInDegrees += 360.0m;

            while (angleInDegrees > 180.0m)
                angleInDegrees -= 360.0m;

            var result = new GeoAngle();

            //switch the value to positive
            result.IsNegative = angleInDegrees < 0;
            angleInDegrees = Math.Abs(angleInDegrees);

            //gets the degree
            result.Degrees = (int)Math.Floor(angleInDegrees);
            var delta = angleInDegrees - result.Degrees;

            //gets minutes and seconds
            var seconds = (int)Math.Floor(3600.0m * delta);
            result.Seconds = seconds % 60;
            result.Minutes = (int)Math.Floor(seconds / 60.0);
            delta = delta * 3600.0m - seconds;

            //gets fractions
            result.Milliseconds = (int)(1000.0m * delta);

            return result;
        }



        public override string ToString()
        {
            var degrees = this.IsNegative
                ? -this.Degrees
                : this.Degrees;

            return string.Format(
                "{0}° {1:00}' {2:00}\"",
                degrees,
                this.Minutes,
                this.Seconds);
        }



        public string ToString(string format)
        {
            switch (format)
            {
                case "NS":
                    return string.Format(
                        "{0}° {1:00}' {2:00}\".{3:000} {4}",
                        this.Degrees,
                        this.Minutes,
                        this.Seconds,
                        this.Milliseconds,
                        this.IsNegative ? 'S' : 'N');

                case "WE":
                    return string.Format(
                        "{0}° {1:00}' {2:00}\".{3:000} {4}",
                        this.Degrees,
                        this.Minutes,
                        this.Seconds,
                        this.Milliseconds,
                        this.IsNegative ? 'W' : 'E');

                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class EAB //exclusion_area_boundary
    {
        public string rid { get; set; }
        public string the_geom { get; set; }

        public EAB(string Rid, string Geom)
        {
            rid = Rid;
            the_geom = Geom;
        }
    }
    public class EAB2 //exclusion_area_boundary
    {
        public string table { get; set; }
        public string gid { get; set; }
        public string fullname { get; set; }

        public EAB2(string Table,string Gid, string Fullname)
        {
            table = Table;
            gid = Gid;
            fullname = Fullname;
        }
    }
}
