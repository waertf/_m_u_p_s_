using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Devart.Common;
using Gurock.SmartInspect;
using Devart.Data.PostgreSql;
using System.Collections.Concurrent;

namespace WhatsUpSqlClient
{
    class Program
    {
        static Mutex _mutex = new Mutex(false, "WhatsUpSqlClient.exe");
        static SqlClient pgsqSqlClient = new SqlClient(
                ConfigurationManager.AppSettings["SQL_SERVER_IP"],
                ConfigurationManager.AppSettings["SQL_SERVER_PORT"],
                ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"],
                ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"],
                ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]
                );
        static SqlClient smsSqlClient = new SqlClient(
            ConfigurationManager.AppSettings["SMS_SERVER_IP"],
                ConfigurationManager.AppSettings["SMS_SERVER_PORT"],
                ConfigurationManager.AppSettings["SMS_SERVER_USER_ID"],
                ConfigurationManager.AppSettings["SMS_SERVER_PASSWORD"],
                ConfigurationManager.AppSettings["SMS_SERVER_DATABASE"]);
        static object sqlLock = new object();
        static string snPointer = ConfigurationManager.AppSettings["AMSCL_pointer"];
        static decimal snPDecimal = decimal.Parse(snPointer);
        static ConcurrentQueue<string> smsQueue = new ConcurrentQueue<string>(); 
        static void Main(string[] args)
        {

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!_mutex.WaitOne(1000, false))
                return;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            SiAuto.Si.Enabled = true;
            SiAuto.Si.Level = Level.Debug;
            SiAuto.Si.Connections = @"file(filename=""" +
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                    "\\log.sil\",rotate=weekly,append=true,maxparts=5,maxsize=500MB)";
            string connectionString = ConfigurationManager.AppSettings["connectString"];
            SiAuto.Main.LogWarning("Start at " + DateTime.Now);

            while (true)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))// + ";Connection Timeout=1"))
                    {
                        SqlExtensions.QuickOpen(connection, 1000);
                    }
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("G") + ":"+e.Message+"WhatsUp ms sql server.");
                Thread.Sleep(10000);
                }
            }
            /*
             * string queryStringForDeviceStatus = @"SELECT 
	dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID,
	dbo.ActiveMonitorStateChangeLog.nMonitorStateID,
	dbo.MonitorState.sStateName
FROM
	dbo.ActiveMonitorStateChangeLog
INNER JOIN dbo.MonitorState ON dbo.ActiveMonitorStateChangeLog.nMonitorStateID = dbo.MonitorState.nMonitorStateID
WHERE
	(
		(
			(
				dbo.ActiveMonitorStateChangeLog.dEndTime
			) IS NULL
		)
		AND (
			(
				dbo.ActiveMonitorStateChangeLog.dStartTime
			) IS NOT NULL
		)
	);";
     */
            string queryStringForDeviceStatus = @"SELECT
	dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID AS id,
	dbo.Device.sDisplayName AS name,
	dbo.ActiveMonitorStateChangeLog.nMonitorStateID AS stateID,
	dbo.MonitorState.sStateName AS stateName,
	dbo.MonitorState.nStateFillColor AS stateColor
FROM
	dbo.ActiveMonitorStateChangeLog
INNER JOIN dbo.MonitorState ON dbo.ActiveMonitorStateChangeLog.nMonitorStateID = dbo.MonitorState.nMonitorStateID
INNER JOIN dbo.Device ON dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID = dbo.Device.nDeviceID
INNER JOIN dbo.PivotDeviceToGroup ON dbo.PivotDeviceToGroup.nDeviceID = dbo.Device.nDeviceID
AND dbo.PivotDeviceToGroup.nDeviceGroupID = 38
INNER JOIN dbo.PivotActiveMonitorTypeToDevice ON dbo.PivotActiveMonitorTypeToDevice.nPivotActiveMonitorTypeToDeviceID = dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID
WHERE
	(
		(
			(
				dbo.ActiveMonitorStateChangeLog.dEndTime IS NULL
			)
		)
		AND (
			(
				dbo.ActiveMonitorStateChangeLog.dStartTime IS NOT NULL
			)
		)
	);";

            /*
            string queryStringForDeviceGroup = @"SELECT dbo.PivotDeviceToGroup.nDeviceID, dbo.PivotDeviceToGroup.nDeviceGroupID, dbo.DeviceGroup.nParentGroupID, dbo.DeviceGroup.nMonitorStateID
FROM dbo.PivotDeviceToGroup INNER JOIN dbo.DeviceGroup ON dbo.PivotDeviceToGroup.nDeviceGroupID = dbo.DeviceGroup.nDeviceGroupID;
";
            */
            /*
            string queryStringForDeviceGroup = @"SELECT
	PivotDeviceToGroup.nDeviceID AS MyID,
	Device.sDisplayName AS MyName,
	PivotDeviceToGroup.nDeviceGroupID AS MyGroupID,
	DeviceGroup_1.sGroupName AS MyGroup,
	DeviceGroup_1.nMonitorStateID AS MyGroupStateID,
	MonitorState.sStateName AS MyGroupState,
	DeviceGroup_1.nParentGroupID AS MyFatherID,
	DeviceGroup.sGroupName AS MyFather
FROM
	DeviceGroup
INNER JOIN DeviceGroup AS DeviceGroup_1 ON DeviceGroup.nDeviceGroupID = DeviceGroup_1.nParentGroupID
AND DeviceGroup.nDeviceGroupID = DeviceGroup_1.nParentGroupID
INNER JOIN PivotDeviceToGroup ON DeviceGroup_1.nDeviceGroupID = PivotDeviceToGroup.nDeviceGroupID
INNER JOIN Device ON PivotDeviceToGroup.nDeviceID = Device.nDeviceID
INNER JOIN MonitorState ON DeviceGroup_1.nMonitorStateID = MonitorState.nMonitorStateID;
";
            */
            /*
            string pgCreateDeviceStatusTable = @"CREATE TABLE
IF NOT EXISTS ""custom"".""WhatsUpDeviceStatus"" (
	""id"" TEXT COLLATE ""default"" NOT NULL,
	""name"" TEXT COLLATE ""default"",
	""stateID"" TEXT COLLATE ""default"",
	""stateName"" TEXT COLLATE ""default"",
	CONSTRAINT ""WhatsUpDeviceStatus_pkey"" PRIMARY KEY (""id"")
) WITH (OIDS = FALSE);

ALTER TABLE ""custom"".""WhatsUpDeviceStatus"" OWNER TO ""postgres"";";
            pgsqSqlClient.SqlScriptCmd(pgCreateDeviceStatusTable);
            */
            //insert into current site status

            string getDeviceWhichGroupIs38=@"SELECT
	dbo.PivotDeviceToGroup.nDeviceID,
	dbo.Device.sDisplayName
FROM
	dbo.PivotDeviceToGroup
INNER JOIN dbo.Device ON dbo.PivotDeviceToGroup.nDeviceID = dbo.Device.nDeviceID
WHERE
	dbo.PivotDeviceToGroup.nDeviceGroupID = 38";

            using (SqlConnection connection = new SqlConnection(connectionString))
                  using (SqlCommand command = new SqlCommand(getDeviceWhichGroupIs38, connection))
                  {
                      StringBuilder sqlScriptStringBuilder = new StringBuilder();
                      connection.Open();
                      using (SqlDataReader reader = command.ExecuteReader())
                      {
                          string insertScript = null;
                          while (reader.Read())
                          {
                              reader[0].ToString();
                              reader[1].ToString();
                              insertScript = @"INSERT INTO PUBLIC .site_status_now_whatup (
	site_id,
	site_name,
	status_code,
	status_name
)
VALUES
	(" + reader[0].ToString() + @", '" + reader[1].ToString() + @"', " + "6" + @", '" + "Up at least 5 min" + @"');";
                              string updateLinkStatus=@"UPDATE link_status_now_whatup
SET status_code = "+"6"+@"
WHERE
	bsite_id = "+reader[0].ToString()+";";
                              sqlScriptStringBuilder.AppendLine(insertScript);
                              //sqlScriptStringBuilder.AppendLine(updateLinkStatus);
                          }
                      }
                      if (sqlScriptStringBuilder.Length > 0)
                      {
                          //exeute insert/update script
                          pgsqSqlClient.SqlScriptCmd(sqlScriptStringBuilder.ToString());
                          SiAuto.Main.LogStringBuilder("Update Modify site_status_now_whatup", sqlScriptStringBuilder);
                          sqlScriptStringBuilder.Clear();
                      }
                  }
            
            System.Threading.Thread getCurrentStatusThread = new System.Threading.Thread
      (delegate()
      {
          //SqlConnection connection = new SqlConnection(connectionString);
          //connection.Open();
          StringBuilder sqlScriptStringBuilder = new StringBuilder();
          while (true)
          {
              //using (SqlConnection connection = new SqlConnection(connectionString))
              {
                  //connection.Open();
                  //SqlCommand command = new SqlCommand(queryStringForDeviceStatus, connection);
                  //remove duplicate rows
                  /*
                  #region

                  string getDuplicateIdAndCount = @"SELECT
	dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID,
	COUNT (*)
FROM
	dbo.ActiveMonitorStateChangeLog
WHERE
	dbo.ActiveMonitorStateChangeLog.dStartTime IS NOT NULL
AND dbo.ActiveMonitorStateChangeLog.dEndTime IS NULL
GROUP BY
	dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID
HAVING
	COUNT (*) > 1";
                  
                  List<string> deviceID=new List<string>(), deviceCount=new List<string>();
                  using (SqlConnection connection = new SqlConnection(connectionString))
                  using (SqlCommand command = new SqlCommand(getDuplicateIdAndCount, connection))
                  {
                      connection.Open();
                      using (SqlDataReader reader = command.ExecuteReader())
                      {
                          while (reader.Read())
                          {
                              deviceID.Add(reader[0].ToString());
                              deviceCount.Add(reader[1].ToString());
                          }
                      }
                  }
                  

                  for (int i = 0; i < deviceID.Count; i++)
                  {
                      string removeDuplicateId =
                      @"DELETE FROM dbo.ActiveMonitorStateChangeLog WHERE ActiveMonitorStateChangeLog.nActiveMonitorStateChangeLogID
IN
(
SELECT TOP "+(int.Parse(deviceCount[i])-1)+@"
dbo.ActiveMonitorStateChangeLog.nActiveMonitorStateChangeLogID

FROM
dbo.ActiveMonitorStateChangeLog
WHERE
dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID = " + deviceID[i] + @"
AND dbo.ActiveMonitorStateChangeLog.dStartTime IS NOT NULL
		AND dbo.ActiveMonitorStateChangeLog.dEndTime IS NULL
)";
                      using (SqlConnection connection1 = new SqlConnection(connectionString))
                      using (SqlCommand command1 = new SqlCommand(removeDuplicateId, connection1))
                      {
                          connection1.Open();
                          int row = command1.ExecuteNonQuery();
                          Console.WriteLine("Affected rows:" + row);
                      }
                  }
                  #endregion
                  */
                  DataSet dsTOP = new DataSet();
                  DataTable dtetTOP=null;

                  //DataSet ds = new DataSet();
                  DataTable dtet=null;
                  string strQuery = "SELECT dbo.Device.nDeviceID,dbo.Device.sDisplayName " +
                              "FROM dbo.Device INNER JOIN dbo.PivotDeviceToGroup ON dbo.Device.nDeviceID = dbo.PivotDeviceToGroup.nDeviceID " +
                              "WHERE dbo.PivotDeviceToGroup.nDeviceGroupID = 38 order by dbo.Device.nDeviceID";
                  using (SqlConnection conn = new SqlConnection(connectionString))
                  {
                      conn.Open();
                      using (SqlDataAdapter daScore = new SqlDataAdapter(strQuery, conn))
                      {
                          daScore.Fill(dsTOP);//, "site"
                          dtetTOP = dsTOP.Tables[0];//["site"]
                      }

                  }
                  using (SqlConnection conn = new SqlConnection(connectionString))
                  //using (SqlCommand command = new SqlCommand(queryStringForDeviceStatus, conn))
                  {
                      //conn.Open();
                      //using (SqlDataReader reader = command.ExecuteReader())
                      {
                          /*
                          using (DataTable dt = new DataTable())
                          {
                              dt.Load(reader);
                              if (dt.Rows.Count > 0)
                              {
                                  pgsqSqlClient.SqlScriptCmd("DELETE FROM custom.\"WhatsUpDeviceStatus\"");
                                  pgsqSqlClient.LoadDatatable(dt);
                              }
                              
                          }
                          */

                          for (int i = 0; i < dtetTOP.Rows.Count; i++)
                          {
                              strQuery = "SELECT top 1 dbo.ActiveMonitorStateChangeLog.nActiveMonitorStateChangeLogID, " +
                                  "dbo.MonitorState.sStateName, " +
                                  "dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID, " +
                                  "dbo.ActiveMonitorStateChangeLog.dStartTime, " +
                                  "dbo.ActiveMonitorStateChangeLog.dEndTime, " +
                                  "dbo.PivotActiveMonitorTypeToDevice.nDeviceID, " +
                                  "dbo.ActiveMonitorStateChangeLog.nMonitorStateID " +
                                  "FROM " +
                                  "(dbo.ActiveMonitorStateChangeLog INNER JOIN " +
                                  "dbo.MonitorState ON dbo.ActiveMonitorStateChangeLog.nMonitorStateID = dbo.MonitorState.nMonitorStateID) " +
                                  "INNER JOIN dbo.PivotActiveMonitorTypeToDevice ON " +
                                  "dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID = dbo.PivotActiveMonitorTypeToDevice.nPivotActiveMonitorTypeToDeviceID " +
                                  "WHERE dbo.ActiveMonitorStateChangeLog.dEndTime Is Null and dbo.PivotActiveMonitorTypeToDevice.nDeviceID = '" + dtetTOP.Rows[i]["nDeviceID"].ToString() + "' " +
                                  "ORDER BY dbo.ActiveMonitorStateChangeLog.dStartTime DESC";

                              conn.Open();
                              using (DataSet ds = new DataSet())
                              using (SqlDataAdapter daScore = new SqlDataAdapter(strQuery, conn))
                              {
                                  daScore.Fill(ds);//, "site"
                                  dtet = ds.Tables[0];//["site"]
                              }
                              //daScore = new SqlDataAdapter(strQuery, conn);
                              //daScore.Fill(ds);//, "site"
                              //dtet = ds.Tables[0];//["site"]
                              conn.Close();
                              //Console.WriteLine(String.Format("DeviceID={0}:DeviceName={1}:StateID={2}:StateMsg={3}:StateColor={4}", reader[0], reader[1], reader[2], reader[3], reader[4]));
                              string DeviceID = dtetTOP.Rows[i]["nDeviceID"].ToString();
                              string DeviceName = dtetTOP.Rows[i]["sDisplayName"].ToString();
                              string StateID = dtet.Rows[0]["nMonitorStateID"].ToString();
                              string StateMsg = dtet.Rows[0]["sStateName"].ToString();
                              //string StateColor = "#"+int.Parse(reader[4].ToString()).ToString("X6");
                              string querySpecificDeviceID = @"SELECT
	PUBLIC .site_status_now_whatup.site_id
FROM
	PUBLIC .site_status_now_whatup
WHERE
	PUBLIC .site_status_now_whatup.site_id = "  + DeviceID;
                              string querySpecificStateID = @"SELECT
	PUBLIC .site_status_now_whatup.status_code
FROM
	PUBLIC .site_status_now_whatup
WHERE
	PUBLIC .site_status_now_whatup.site_id = " + DeviceID;
                              using (DataTable dt = pgsqSqlClient.get_DataTable(querySpecificDeviceID))
                              {
                                  if (dt != null && dt.Rows.Count != 0)
                                  {
                                      using (DataTable dt2 = pgsqSqlClient.get_DataTable(querySpecificStateID))
                                      {
                                          string stateResult = string.Empty;
                                          if (dt2 != null && dt2.Rows.Count != 0)
                                          {
                                              stateResult = dt2.Rows[0].ItemArray[0]
                                                  .ToString();
                                              
                                              if (stateResult.Equals(StateID))
                                              {
                                                  //do nothing
                                              }
                                              else
                                              {
                                                  //update
                                                  Console.WriteLine("stateResult:" + stateResult + Environment.NewLine + "StateID:" + StateID);
                                                  SiAuto.Main.LogMessage("stateResult:" + stateResult + Environment.NewLine + "StateID:" + StateID);
                                                  string updateScript = @"UPDATE PUBLIC .site_status_now_whatup
SET status_code = "+StateID+@",
 status_name = '"+StateMsg+@"'"+@"
WHERE
	site_id = " + DeviceID+";";
                                                  string updateLinkStatus=@"UPDATE link_status_now_whatup
SET status_code = "+StateID+@"
WHERE
	bsite_id = "+DeviceID+";";
                                                  sqlScriptStringBuilder.AppendLine(updateScript);
                                                  sqlScriptStringBuilder.AppendLine(updateLinkStatus);
                                                  //call send sms
                                                  smsQueue.Enqueue(DeviceName+"&"+StateID);
                                              }
                                          }
                                      }    
                                  }
                                  else
                                  {
                                      //insert
                                      string insertScript = @"INSERT INTO PUBLIC .site_status_now_whatup (
	site_id,
	site_name,
	status_code,
	status_name
)
VALUES
	("+DeviceID+@", '"+DeviceName+@"', "+StateID+@", '"+StateMsg+@"');";
                                      string updateLinkStatus=@"UPDATE link_status_now_whatup
SET status_code = "+StateID+@"
WHERE
	bsite_id = "+DeviceID+";";
                                      sqlScriptStringBuilder.AppendLine(insertScript);
                                      sqlScriptStringBuilder.AppendLine(updateLinkStatus);
                                      //call send sms
                                      smsQueue.Enqueue(DeviceName + "&" + StateID);
                                  }
                              }
                              
                          }
                          
                      }
                  }
                  
              }
              if (sqlScriptStringBuilder.Length > 0)
              {
                  //exeute insert/update script
                  pgsqSqlClient.SqlScriptCmd(sqlScriptStringBuilder.ToString());
                  SiAuto.Main.LogStringBuilder("Update Modify site_status_now_whatup", sqlScriptStringBuilder);
                  sqlScriptStringBuilder.Clear();
              }
              Thread.Sleep(1000);
          }
      });
            /*
            System.Threading.Thread t2 = new System.Threading.Thread
      (delegate()
      {
          SqlConnection connection = new SqlConnection(connectionString);
          connection.Open();
          while (true)
          {
              //using (SqlConnection connection = new SqlConnection(connectionString))
              {
                  //connection.Open();
                  //SqlCommand command = new SqlCommand(queryStringForDeviceGroup, connection);
                  using (SqlCommand command = new SqlCommand(queryStringForDeviceGroup, connection))
                  using (SqlDataReader reader = command.ExecuteReader())
                  {
                      while (reader.Read())
                      {
                          Console.WriteLine(String.Format("MyID={0}:MyName={1}:MyGroupID={2}:MyGroup={3}:MyStateID={4}:MyState={5}:MyFatherID={6}:MyFather={7}", reader[0], reader[1], reader[2], reader[3], reader[4], reader[5], reader[6], reader[7]));
                      }
                  }
              }
              Thread.Sleep(1000);
          }
      });
            */
            System.Threading.Thread synDeviceHistoryThread = new System.Threading.Thread
      (delegate()
      {
          //string snPointer = ConfigurationManager.AppSettings["AMSCL_pointer"];
          //decimal snPDecimal = decimal.Parse(snPointer);
          while (true)
          {
              string queryResult = @"SELECT
	dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID,
	dbo.Device.sDisplayName,
	dbo.ActiveMonitorStateChangeLog.nMonitorStateID,
	dbo.MonitorState.sStateName,
	dbo.ActiveMonitorStateChangeLog.dStartTime,
	dbo.ActiveMonitorStateChangeLog.dEndTime
FROM
	dbo.ActiveMonitorStateChangeLog
INNER JOIN dbo.MonitorState ON dbo.ActiveMonitorStateChangeLog.nMonitorStateID = dbo.MonitorState.nMonitorStateID
INNER JOIN dbo.Device ON dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID = dbo.Device.nDeviceID
INNER JOIN dbo.PivotDeviceToGroup ON dbo.PivotDeviceToGroup.nDeviceID = dbo.Device.nDeviceID
AND dbo.PivotDeviceToGroup.nDeviceGroupID = 38
INNER JOIN dbo.PivotActiveMonitorTypeToDevice ON dbo.PivotActiveMonitorTypeToDevice.nPivotActiveMonitorTypeToDeviceID = dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID
WHERE
	dbo.ActiveMonitorStateChangeLog.nActiveMonitorStateChangeLogID = " + snPDecimal + ";";
              
              using (SqlConnection connection = new SqlConnection(connectionString))
              using (SqlCommand command = new SqlCommand(queryResult, connection))
              {
                  connection.Open();
                  using (SqlDataReader reader = command.ExecuteReader())
                  {
                      if (reader.Read())
                      {
                          //if (reader[0].Equals(DBNull.Value))
                          {
                              /*
                              if (LessThanMaxID(snPDecimal))
                              {
                                  snPDecimal++;
                                  UpdateSetting("AMSCL_pointer", snPDecimal.ToString());
                              }
                              */
                          }
                          //else
                          {
                              snPDecimal++;
                              Console.WriteLine(snPDecimal);
                              //UpdateSetting("AMSCL_pointer", snPDecimal.ToString());
                              string DeviceID = reader[0].ToString();
                              string DeviceName = reader[1].ToString();
                              string StateID = reader[2].ToString();
                              string StateMsg = reader[3].ToString();
                              string startTime = reader[4].ToString();
                              string endTime = reader[5].ToString();
                              string insertScript = @"INSERT INTO PUBLIC .device_status_history_whatup (
	device_no,
	device_name,
	message_code,
	message_note,
	start_time,
    end_time,
alarm_status
)
VALUES
	(" + DeviceID + @", '" + DeviceName + @"', " + StateID + @", '" + StateMsg + @"', '" + startTime + @"', '" + endTime + @"',0);";
                              pgsqSqlClient.SqlScriptCmd(insertScript);
                          }
                      }
                      else
                      {
                          if (LessThanMaxID(snPDecimal))
                          {
                              snPDecimal++;
                              Console.WriteLine(snPDecimal);
                              //UpdateSetting("AMSCL_pointer", snPDecimal.ToString());
                          }
                      }
                      //Console.WriteLine(snPDecimal);
                  }
              }
              
              Thread.Sleep(1);
          }
      });
            getCurrentStatusThread.Start();
            //t2.Start();
            synDeviceHistoryThread.Start();
            GraceFullCtrlC();
            var smsSendTimer = new System.Timers.Timer(5);
            smsSendTimer.Elapsed += (sender, e) =>
            {
                string result = null;
                string deviceName = null;
                string stateId = null;
                //ThreadPool.QueueUserWorkItem(delegate
                //{
                if (smsQueue.TryDequeue(out result))
                {
                    string[] getStrings = result.Split(new char[] {'&'});
                    if (getStrings.Length.Equals(2))
                    {
                        deviceName = getStrings[0];
                        stateId = getStrings[1];
                        SendStatusSMS(deviceName,stateId);
                    }
                }
                //});
            };
            smsSendTimer.Enabled = true;
            
        }

        private static bool LessThanMaxID(decimal snPDecimal)
        {
            bool result = false;
            string connectionString = ConfigurationManager.AppSettings["connectString"];
            string queryMaxID = @"SELECT
	MAX (
		dbo.ActiveMonitorStateChangeLog.nActiveMonitorStateChangeLogID
	)
FROM
	dbo.ActiveMonitorStateChangeLog;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(queryMaxID, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string MaxID = reader[0].ToString();
                        decimal maxIdDecimal;
                        if (decimal.TryParse(MaxID, out maxIdDecimal))
                        {
                            if (maxIdDecimal > snPDecimal)
                                result= true;
                            else
                            {
                                result= false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("MaxID:" + MaxID + " Over Decimal scope" );
                            SiAuto.Main.LogError("MaxID:"+MaxID+" Over Decimal scope");
                            result= false;
                        }
                    }
                }
            }
            return result;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                SiAuto.Main.LogException(exception);
            }
            Environment.Exit(1);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            UpdateSetting("AMSCL_pointer", snPDecimal.ToString());
            string logMsg = string.Empty;
            logMsg = "Close time:" + DateTime.Now.ToString("G") + Environment.NewLine +
                  "Memory usage:" +
                  Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            SiAuto.Main.LogWarning(logMsg);
            if (_mutex!=null)
            _mutex.ReleaseMutex();
        }

        private static void UpdateSetting(string key, string value)
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        static void GraceFullCtrlC()
        {
            Console.CancelKeyPress += delegate(object sender,
                                    ConsoleCancelEventArgs e)
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlBreak)
                {
                    Console.WriteLine("Ctrl-Break catched and" +
                      " translated into an cooperative shutdown");
                    // Environment.Exit(1) would NOT do 
                    // a cooperative shutdown. No finalizers are called!
                    Thread t = new Thread(delegate()
                    {
                        Console.WriteLine("Asynchronous shutdown started");
                        Environment.Exit(1);
                    });

                    t.Start();
                    t.Join();
                }
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    e.Cancel = true; // tell the CLR to keep running
                    Console.WriteLine("Ctrl-C catched and " +
                      "translated into cooperative shutdown");
                    // If we want to call exit triggered from
                    // out event handler we have to spin
                    // up another thread. If somebody of the
                    // CLR team reads this. Please fix!
                    new Thread(delegate()
                    {
                        Console.WriteLine("Asynchronous shutdown started");
                        Environment.Exit(2);
                    }).Start();
                }
            };

            Console.WriteLine("Press now Ctrl-C or Ctrl-Break");
            Thread.Sleep(10000);
        }

        private static readonly string m_sender = "拓樸系統管理者";
        static void SendStatusSMS(string deviceName, string deviceStateId)
        {
            string queryPhoneNumber = @"SELECT
public.msg_whatup_send.phone_number
FROM
public.msg_whatup_send
WHERE
public.msg_whatup_send.message_no = " + deviceStateId;
            string queryStateChineseDescription = @"SELECT
public.alarm_set_whatup.cnote
FROM
public.alarm_set_whatup
WHERE
public.alarm_set_whatup.serial_no = " + deviceStateId;
            string stateChineseDescription = null;
            string phoneNumber = null;
            StringBuilder smsInsertSqlScriptBuilder = new StringBuilder();
            StringBuilder smsHistoryBuilder = new StringBuilder();
            try
            {
                using (DataTable dt = pgsqSqlClient.get_DataTable(queryStateChineseDescription))
                {
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        stateChineseDescription = dt.Rows[0].ItemArray[0].ToString();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            try
            {
                using (DataTable dt = pgsqSqlClient.get_DataTable(queryPhoneNumber))
                {
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            phoneNumber = row[0].ToString();
                            Console.WriteLine(phoneNumber + ":" + deviceName + ":" + stateChineseDescription);
                            //send sms 
                            SiAuto.Main.LogText("send sms", phoneNumber + ":" + deviceName + ":" + stateChineseDescription);
                            string insertSqlScript = @"INSERT INTO t_sendsms (
	m_sender,
	m_recver,
	m_recvtime,
	m_content,
	m_phoneno,
	m_status
)
VALUES
	(
		'"+m_sender+@"',
		'"+phoneNumber+@"',
		'"+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+@"',
		'" + m_sender+@"(WhatsUp)"+deviceName + ":" + stateChineseDescription + @"',
		1,
		1
	);";
                            string smsHistory = @"INSERT INTO ams_history (phone_number,message_note) VALUES ('" + phoneNumber + @"','" + deviceStateId + @"');";
                            smsInsertSqlScriptBuilder.AppendLine(insertSqlScript);
                            smsHistoryBuilder.AppendLine(smsHistory);
                        }
                    }
                }
            }
            catch (Exception)
            {
                
                throw;
            }
            if (!smsInsertSqlScriptBuilder.Length.Equals(0))
            {
                smsSqlClient.SqlScriptCmd(smsInsertSqlScriptBuilder.ToString());
            }
            if (!smsHistoryBuilder.Length.Equals(0))
            {
                pgsqSqlClient.SqlScriptCmd(smsHistoryBuilder.ToString());
            }
        }
        
    }
}
