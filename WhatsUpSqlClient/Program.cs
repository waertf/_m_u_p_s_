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
            string queryString = @"SELECT DISTINCT dbo_ActiveMonitorStateChangeLog.nPivotActiveMonitorTypeToDeviceID, dbo_ActiveMonitorStateChangeLog.nMonitorStateID, dbo_MonitorState.sStateName
FROM dbo_ActiveMonitorStateChangeLog INNER JOIN dbo_MonitorState ON dbo_ActiveMonitorStateChangeLog.nMonitorStateID = dbo_MonitorState.nMonitorStateID
WHERE (((dbo_ActiveMonitorStateChangeLog.dEndTime) Is Null) AND ((dbo_ActiveMonitorStateChangeLog.dStartTime) Is Not Null));";
            while (true)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(queryString, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine(String.Format("DeviceID={0}:StateID={1}:StateMsg={2}", reader[0],reader[1],reader[2]));
                    }
                }
                Thread.Sleep(3000);
            }
            
        }
    }
}
