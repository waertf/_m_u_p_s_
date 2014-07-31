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
	dbo.ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID,
	dbo.Device.sDisplayName,
	dbo.ActiveMonitorStateChangeLog.nMonitorStateID,
	dbo.MonitorState.sStateName
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
            string createDeviceStatusTable = @"CREATE TABLE
IF NOT EXISTS ""public"".""WhatsUpDeviceStatus"" (
	""id"" TEXT COLLATE ""default"" NOT NULL,
	""name"" TEXT COLLATE ""default"",
	""stateID"" TEXT COLLATE ""default"",
	""stateName"" TEXT COLLATE ""default"",
	CONSTRAINT ""WhatsUpDeviceStatus_pkey"" PRIMARY KEY (""id"")
) WITH (OIDS = FALSE);

ALTER TABLE ""public"".""WhatsUpDeviceStatus"" OWNER TO ""postgres"";";
            System.Threading.Thread t1 = new System.Threading.Thread
      (delegate()
      {
          //SqlConnection connection = new SqlConnection(connectionString);
          //connection.Open();
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
                          while (reader.Read())
                          {
                              Console.WriteLine(String.Format("DeviceID={0}:DeviceName={1}:StateID={2}:StateMsg={3}", reader[0], reader[1], reader[2], reader[3]));
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
