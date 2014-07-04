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
            string queryStringForDeviceStatus = @"SELECT DISTINCT dbo_ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID, dbo_ActiveMonitorStateChangeLog.nMonitorStateID, dbo_MonitorState.sStateName
FROM dbo_ActiveMonitorStateChangeLog INNER JOIN dbo_MonitorState ON dbo_ActiveMonitorStateChangeLog.nMonitorStateID = dbo_MonitorState.nMonitorStateID
WHERE (((dbo_ActiveMonitorStateChangeLog.dEndTime) Is Null) AND ((dbo_ActiveMonitorStateChangeLog.dStartTime) Is Not Null));";
            string queryStringForDeviceGroup = @"SELECT dbo_PivotDeviceToGroup.nDeviceID, dbo_PivotDeviceToGroup.nDeviceGroupID, dbo_DeviceGroup.nParentGroupID, dbo_DeviceGroup.nMonitorStateID
FROM dbo_PivotDeviceToGroup INNER JOIN dbo_DeviceGroup ON dbo_PivotDeviceToGroup.nDeviceGroupID = dbo_DeviceGroup.nDeviceGroupID;
";
            System.Threading.Thread t1 = new System.Threading.Thread
      (delegate()
      {
          while (true)
          {
              using (SqlConnection connection = new SqlConnection(connectionString))
              {
                  connection.Open();
                  SqlCommand command = new SqlCommand(queryStringForDeviceStatus, connection);
                  using (SqlDataReader reader = command.ExecuteReader())
                  {
                      while (reader.Read())
                      {
                          Console.WriteLine(String.Format("DeviceID={0}:StateID={1}:StateMsg={2}", reader[0], reader[1], reader[2]));
                      }
                  }
              }
              Thread.Sleep(3000);
          }
      });
            System.Threading.Thread t2 = new System.Threading.Thread
      (delegate()
      {
          while (true)
          {
              using (SqlConnection connection = new SqlConnection(connectionString))
              {
                  connection.Open();
                  SqlCommand command = new SqlCommand(queryStringForDeviceGroup, connection);
                  using (SqlDataReader reader = command.ExecuteReader())
                  {
                      while (reader.Read())
                      {
                          Console.WriteLine(String.Format("DeviceID={0}:DeviceGroupID={1}:ParentGroupID={2}:MonitorStateID={3}", reader[0], reader[1], reader[2], reader[3]));
                      }
                  }
              }
              Thread.Sleep(3000);
          }
      });
            t1.Start();
            t2.Start();
            
            
        }
    }
}
