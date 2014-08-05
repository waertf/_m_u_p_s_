using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Gurock.SmartInspect;
using Devart.Data.PostgreSql;

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
        static object sqlLock = new object();
        static void Main(string[] args)
        {
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
	)
;";

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
            System.Threading.Thread t1 = new System.Threading.Thread
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
                  using (SqlConnection connection = new SqlConnection(connectionString))
                  using (SqlCommand command = new SqlCommand(queryStringForDeviceStatus, connection))
                  {
                      connection.Open();
                      using (SqlDataReader reader = command.ExecuteReader())
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
                          
                          while (reader.Read())
                          {
                              //Console.WriteLine(String.Format("DeviceID={0}:DeviceName={1}:StateID={2}:StateMsg={3}:StateColor={4}", reader[0], reader[1], reader[2], reader[3], reader[4]));
                              string DeviceID = reader[0].ToString();
                              string DeviceName = reader[1].ToString();
                              string StateID = reader[2].ToString();
                              string StateMsg = reader[3].ToString();
                              string StateColor = reader[4].ToString();
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
                                          if (dt2 != null && dt2.Rows.Count != 0)
                                          {
                                              string stateResult = dt2.Rows[0].ItemArray[0]
                                                  .ToString();
                                              if (stateResult.Equals(StateID))
                                              {
                                                  //do nothing
                                              }
                                              else
                                              {
                                                  //update
                                                  string updateScript = @"UPDATE PUBLIC .site_status_now_whatup
SET status_code = "+StateID+@",
 status_name = '"+StateMsg+@"',
 status_color = '"+StateColor+@"'
WHERE
	site_id = " + DeviceID+";";
                                                  sqlScriptStringBuilder.AppendLine(updateScript);
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
	status_name,
	status_color
)
VALUES
	("+DeviceID+@", '"+DeviceName+@"', "+StateID+@", '"+StateMsg+@"', '"+StateColor+@"');";
                                      sqlScriptStringBuilder.AppendLine(insertScript);
                                  }
                              }
                              //exeute insert/update script
                              pgsqSqlClient.SqlScriptCmd(sqlScriptStringBuilder.ToString());
                              sqlScriptStringBuilder.Clear();
                          }
                          
                      }
                  }
                  
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
            t1.Start();
            //t2.Start();
            
            
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                SiAuto.Main.LogException("Restart", exception);
            }
            Environment.Exit(1);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            string logMsg = string.Empty;
            logMsg = "Close time:" + DateTime.Now.ToString("G") + Environment.NewLine +
                  "Memory usage:" +
                  Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            SiAuto.Main.LogWarning(logMsg);
            _mutex.ReleaseMutex();
        }
    }
}
