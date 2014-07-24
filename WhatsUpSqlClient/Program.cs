using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;

namespace WhatsUpSqlClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = ConfigurationManager.AppSettings["connectString"];
            string queryStringForDeviceStatus = @"SELECT DISTINCT
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
            /*
            string queryStringForDeviceGroup = @"SELECT dbo.PivotDeviceToGroup.nDeviceID, dbo.PivotDeviceToGroup.nDeviceGroupID, dbo.DeviceGroup.nParentGroupID, dbo.DeviceGroup.nMonitorStateID
FROM dbo.PivotDeviceToGroup INNER JOIN dbo.DeviceGroup ON dbo.PivotDeviceToGroup.nDeviceGroupID = dbo.DeviceGroup.nDeviceGroupID;
";
            */
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
            System.Threading.Thread t1 = new System.Threading.Thread
      (delegate()
      {
          SqlConnection connection = new SqlConnection(connectionString);
          connection.Open();
          while (true)
          {
              //using (SqlConnection connection = new SqlConnection(connectionString))
              {
                  //connection.Open();
                  //SqlCommand command = new SqlCommand(queryStringForDeviceStatus, connection);
                  using (SqlCommand command = new SqlCommand(queryStringForDeviceStatus, connection))
                  using (SqlDataReader reader = command.ExecuteReader())
                  {
                      while (reader.Read())
                      {
                          Console.WriteLine(String.Format("DeviceID={0}:StateID={1}:StateMsg={2}", reader[0], reader[1], reader[2]));
                      }
                  }
              }
              Thread.Sleep(1000);
          }
      });
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
            t1.Start();
            t2.Start();
            
            
        }
    }
}
